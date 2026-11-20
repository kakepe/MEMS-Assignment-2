using System.Data;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MEMS.Data.Seeding;

public static class DatabaseInitialization
{
    public const string DomainVersion = "election-samples-v1";
    public const string IdentityVersion = "administrator-samples-v1";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, bool development)
    {
        if (!development) throw new InvalidOperationException("Automatic database setup is restricted to local Development.");
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MemsDbContext>();
            RequireAssignment2(db);
            await db.Database.MigrateAsync();
        }
        await SeedAsync(services, configuration, development);
    }

    public static void RequireAssignment2(MemsDbContext db)
    {
        if (!string.Equals(db.Database.GetDbConnection().Database, "MEMSAssignment2", StringComparison.Ordinal))
            throw new InvalidOperationException("Database setup/checks require the dedicated MEMSAssignment2 database.");
    }

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, bool development)
    {
        if (!development) throw new InvalidOperationException("Demo seeds are restricted to local Development.");
        await using var strategyScope = services.CreateAsyncScope();
        var strategyDb = strategyScope.ServiceProvider.GetRequiredService<MemsDbContext>();
        RequireAssignment2(strategyDb);
        await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // A fresh scoped context and Identity managers share each retriable transaction.
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var db = provider.GetRequiredService<MemsDbContext>();
            db.UseSystemScope();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'MEMSAssignment2:Seed:v1',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
                IF @result < 0 THROW 51000, 'Could not acquire the seed lock.', 1;
                """);
            if (!await db.SeedHistory.AnyAsync(x => x.SeedKey == DomainVersion))
            {
                foreach (var (name, slug, color) in new[] {
                    ("Burnaby", "burnaby", "#08695f"), ("Surrey", "surrey", "#274b8b"),
                    ("Richmond", "richmond", "#8b3b54"), ("Pitt Meadows", "pitt-meadows", "#426c3b"),
                    ("Maple Ridge", "maple-ridge", "#87512d") })
                {
                    var municipality = await db.Municipalities.SingleOrDefaultAsync(x => x.Slug == slug);
                    if (municipality is null)
                    {
                        municipality = new Municipality { Name = name, Slug = slug, BrandColor = color,
                            PublicTitle = $"{name} election guide", LogoFileName = $"demo-{slug}.svg",
                            Description = $"Fictional student demonstration election information for {name}." };
                        db.Municipalities.Add(municipality);
                        await db.SaveChangesAsync();
                    }
                    foreach (var (office, seats) in new[] { ("Mayor", 1), ("Councilor", 6), ("School Trustee", 2) })
                        if (!await db.Positions.AnyAsync(x => x.MunicipalityId == municipality.MunicipalityId && x.Name == office))
                            db.Positions.Add(new Position { MunicipalityId = municipality.MunicipalityId, Name = office, SeatsAvailable = seats });
                    await db.SaveChangesAsync();
                }
                var positions = await db.Positions.Include(x => x.Municipality).ToListAsync();
                foreach (var sample in DemoCandidates.All.Concat(RichmondCandidates.All))
                {
                    if (await db.Candidates.AnyAsync(x => x.DemoSeedKey == sample.Key)) continue;
                    var position = positions.Single(x => x.Name == sample.Position && x.Municipality.Name == sample.Municipality);
                    db.Candidates.Add(new Candidate { Name = sample.Name, Profile = sample.Biography,
                        DemoSeedKey = sample.Key, MunicipalityId = position.MunicipalityId, PositionId = position.PositionId,
                        PhotoFileName = sample.PhotoFileName, Email = "candidate@example.org", Website = "https://example.org" });
                }
                db.SeedHistory.Add(new SeedHistory { SeedKey = DomainVersion });
                await db.SaveChangesAsync();
            }
            if (!await db.SeedHistory.AnyAsync(x => x.SeedKey == IdentityVersion))
            {
                var password = configuration["Seed:DevelopmentPassword"];
                if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Configure the local rubric development seed credential before initial account setup.");
                var roles = provider.GetRequiredService<RoleManager<ApplicationRole>>();
                var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
                await EnsureRole(AdministratorRoles.SuperAdmin, AdministratorScope.System, null);
                await EnsureRole(AdministratorRoles.MunicipalityAdmin, AdministratorScope.Municipality, null);
                var municipalities = await db.Municipalities.ToListAsync();
                foreach (var municipality in municipalities)
                    await EnsureRole(municipality.Name + " Admin", AdministratorScope.Municipality, municipality.MunicipalityId);
                foreach (var (username, slug) in new[] { ("super", ""), ("Burnabyadmin", "burnaby"),
                    ("Surreyadmin", "surrey"), ("Richmondadmin", "richmond"), ("pm", "pitt-meadows") })
                {
                    var municipality = slug.Length == 0 ? null : municipalities.Single(x => x.Slug == slug);
                    var scopeKind = municipality is null ? AdministratorScope.System : AdministratorScope.Municipality;
                    var user = await users.FindByNameAsync(username);
                    if (user is null)
                    {
                        user = new ApplicationUser { UserName = username, Scope = scopeKind, MunicipalityId = municipality?.MunicipalityId };
                        Check(await users.CreateAsync(user, password));
                    }
                    else if (user.Scope != scopeKind || user.MunicipalityId != municipality?.MunicipalityId)
                        throw new InvalidOperationException("An existing administrator conflicts with the expected seed assignment; no account was overwritten.");
                    var expectedRoles = municipality is null ? new[] { AdministratorRoles.SuperAdmin }
                        : new[] { AdministratorRoles.MunicipalityAdmin, municipality.Name + " Admin" };
                    foreach (var role in expectedRoles)
                        if (!await users.IsInRoleAsync(user, role)) Check(await users.AddToRoleAsync(user, role));
                }
                db.SeedHistory.Add(new SeedHistory { SeedKey = IdentityVersion });
                await db.SaveChangesAsync();

                async Task EnsureRole(string name, AdministratorScope scopeKind, int? municipalityId)
                {
                    var role = await roles.FindByNameAsync(name);
                    if (role is null) Check(await roles.CreateAsync(new ApplicationRole { Name = name, Scope = scopeKind, MunicipalityId = municipalityId }));
                    else if (role.Scope != scopeKind || role.MunicipalityId != municipalityId)
                        throw new InvalidOperationException("An existing role conflicts with the expected seed scope.");
                }
            }
            await transaction.CommitAsync();
            provider.GetRequiredService<ILoggerFactory>().CreateLogger("MEMS.DatabaseSetup")
                .LogInformation("Assignment 2 migration and seed checkpoint completed; existing samples and credentials retained.");
        });
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Identity seed validation failed: " + string.Join("; ", result.Errors.Select(x => x.Code)));
    }
}
