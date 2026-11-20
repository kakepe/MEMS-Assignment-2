// Compiled into the existing API executable; no additional .NET project or HTTP test endpoint.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MEMS.Data;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using MEMS.Data.Seeding;
using MEMS.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Api.Verification;

internal static class Phase2DatabaseChecks
{
    private sealed record RestartState(string UserId, int CandidateId, int TenantId, string Password,
        string PasswordHash, string Profile, DateTime Created, string RowVersion, string BeforeFingerprint, string ProbeFingerprint);

    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration, bool development, string mode)
    {
        if (!development) throw new InvalidOperationException("Database verification is Development-only.");
        var local = configuration["Mems:LocalStorage"] ?? throw new InvalidOperationException("Local verification storage is required.");
        var statePath = Path.Combine(local, "phase2-persistence.json");
        var root = Directory.GetParent(local)!.FullName;
        var connection = configuration.GetConnectionString("mems")!;
        var checks = new List<string>();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MemsDbContext>();
        DatabaseInitialization.RequireAssignment2(db);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var reader = scope.ServiceProvider.GetRequiredService<TenantElectionReader>();

        if (mode == "--prepare-phase2-restart")
        {
            if (File.Exists(statePath)) throw new InvalidOperationException("A persistence probe already exists; complete it before preparing another.");
            var before = await Fingerprint(connection);
            var tenant = (await reader.ResolveAsync("burnaby"))!;
            var office = (await reader.PositionsAsync(tenant.MunicipalityId)).First();
            var first = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";
            var replacement = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "bB2!";
            var user = new ApplicationUser { UserName = "phase2.persistence." + Guid.NewGuid().ToString("N"),
                Scope = AdministratorScope.Municipality, MunicipalityId = tenant.MunicipalityId };
            Good(await users.CreateAsync(user, first));
            Good(await users.AddToRoleAsync(user, AdministratorRoles.MunicipalityAdmin));
            Good(await users.ChangePasswordAsync(user, first, replacement));
            db.UseTenant(tenant.MunicipalityId);
            var candidate = new Candidate { MunicipalityId = tenant.MunicipalityId, PositionId = office.PositionId,
                Name = "Phase 2 persistence probe", Profile = "Original verification-owned profile." };
            db.Candidates.Add(candidate);
            await db.SaveChangesAsync();
            candidate.Profile = "Edited verification-owned profile must survive restart.";
            await db.SaveChangesAsync();
            await DatabaseInitialization.SeedAsync(services, configuration, true);
            var state = new RestartState(user.Id, candidate.CandidateId, tenant.MunicipalityId, replacement,
                user.PasswordHash!, candidate.Profile, candidate.DateCreated, Convert.ToHexString(candidate.RowVersion),
                before, await Fingerprint(connection));
            await File.WriteAllTextAsync(statePath, JsonSerializer.Serialize(state));
            checks.Add("Created an owned candidate and administrator; changed its password and recorded a private restart fingerprint.");
        }
        else if (mode == "--complete-phase2-restart")
        {
            var state = JsonSerializer.Deserialize<RestartState>(await File.ReadAllTextAsync(statePath))!;
            var user = await users.FindByIdAsync(state.UserId);
            Require(user is not null && user.UserName!.StartsWith("phase2.persistence.", StringComparison.Ordinal), "Only the verification-owned account may be checked/cleaned.");
            Require(user!.PasswordHash == state.PasswordHash && await users.CheckPasswordAsync(user, state.Password), "Changed password/hash survived the Aspire restart.");
            Require(await Fingerprint(connection) == state.ProbeFingerprint, "All seeded and owned records survived restart without changes or duplicates.");
            db.UseTenant(state.TenantId);
            var candidate = await db.Candidates.SingleAsync(x => x.CandidateId == state.CandidateId);
            Require(candidate.Name == "Phase 2 persistence probe" && candidate.DemoSeedKey is null && candidate.Profile == state.Profile
                && candidate.DateCreated == state.Created && Convert.ToHexString(candidate.RowVersion) == state.RowVersion,
                "Edited candidate contents, creation date and row version survived restart.");
            // Remove only rows created by the prepare mode. Seeded/user-owned application records are not touched.
            db.Candidates.Remove(candidate);
            await db.SaveChangesAsync();
            Good(await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user)));
            Good(await users.DeleteAsync(user));
            Require(await Fingerprint(connection) == state.BeforeFingerprint, "Probe cleanup restored the complete pre-probe record fingerprint.");
            File.Move(statePath, Path.Combine(local, "phase2-persistence-completed-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json"));
            checks.AddRange(["Changed Identity password/hash survived actual Aspire restart.",
                "Candidate contents/creation date/row version and all seed records survived actual restart.",
                "Removed only verification-owned fixtures; complete pre-probe data fingerprint restored."]);
        }
        else
        {
            Require(!db.Database.HasPendingModelChanges(), "Migration snapshot matches the current EF model.");
            Require(!(await db.Database.GetPendingMigrationsAsync()).Any(), "All EF migrations are applied.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.Municipalities") == 5, "Five municipalities exist.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.Positions") == 15, "Fifteen positions exist.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.Candidates") == 30, "Thirty fictional candidates exist.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.AspNetUsers") == 5, "Five required accounts exist.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.AspNetRoles") == 7, "Two system/municipal roles and five legacy tenant groups exist.");
            Require(await Scalar(connection, "SELECT COUNT(*) FROM dbo.SeedHistory") == 2, "Both one-time seed versions completed.");
            checks.Add("Expected migration, seed history, 5 tenants, 15 positions, 30 candidates, 5 accounts and 7 roles verified.");

            Require(await db.Candidates.CountAsync() == 0 && await db.Positions.CountAsync() == 0 && await db.Municipalities.CountAsync() == 0,
                "Missing tenant context fails closed.");
            foreach (var slug in new[] { "burnaby", "surrey", "richmond", "pitt-meadows", "maple-ridge" })
            {
                var tenant = await reader.ResolveAsync(" " + slug.ToUpperInvariant() + " ");
                Require(tenant?.Slug == slug, "Slug resolution normalizes input.");
                var positions = await reader.PositionsAsync(tenant!.MunicipalityId);
                var candidates = await reader.CandidatesAsync(tenant.MunicipalityId);
                Require(positions.Count == 3 && candidates.Count == 6 && positions.All(x => x.MunicipalityId == tenant.MunicipalityId)
                    && candidates.All(x => x.MunicipalityId == tenant.MunicipalityId), "Tenant services return only their own rows.");
                foreach (var candidate in candidates)
                    Require(File.Exists(Path.Combine(root, "src/MEMS.Api/wwwroot/images/demo-candidates", candidate.PhotoFileName!)), "Each seeded candidate image exists.");
            }
            var burnaby = (await reader.ResolveAsync("burnaby"))!;
            var surrey = (await reader.ResolveAsync("surrey"))!;
            var foreign = (await reader.CandidatesAsync(surrey.MunicipalityId)).First();
            Require(await reader.CandidateAsync(burnaby.MunicipalityId, foreign.CandidateId) is null, "Foreign direct ID lookup returns no candidate.");
            await using (var tenantDb = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<MemsDbContext>>().CreateDbContextAsync())
            {
                tenantDb.UseTenant(burnaby.MunicipalityId);
                tenantDb.Candidates.Add(new Candidate { MunicipalityId = surrey.MunicipalityId, PositionId = foreign.PositionId, Name = "Rejected probe", Profile = "Must not be written." });
                var rejected = false;
                try { await tenantDb.SaveChangesAsync(); } catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Tenant write guard rejects foreign inserts before SQL.");
            }
            checks.Add("All five tenants isolated by EF filters/services; absent scope, direct-ID reads, foreign writes and all 30 image references verified.");

            var password = configuration["Seed:DevelopmentPassword"]!;
            foreach (var (name, slug) in new[] { ("super", ""), ("Burnabyadmin", "burnaby"), ("Surreyadmin", "surrey"), ("Richmondadmin", "richmond"), ("pm", "pitt-meadows") })
            {
                var user = await users.FindByNameAsync(name);
                Require(user is not null && user.PasswordHash != password && await users.CheckPasswordAsync(user, password), "Required account has a working salted Identity password hash.");
                if (slug == "") Require(user!.Scope == AdministratorScope.System && user.MunicipalityId is null && await users.IsInRoleAsync(user, AdministratorRoles.SuperAdmin), "Super Admin has system scope.");
                else Require(user!.Scope == AdministratorScope.Municipality && user.MunicipalityId == (await reader.ResolveAsync(slug))!.MunicipalityId
                    && await users.IsInRoleAsync(user, AdministratorRoles.MunicipalityAdmin), "Municipal account has exactly one expected tenant.");
            }
            var pm = (await users.FindByNameAsync("pm"))!;
            var roleRejected = false;
            try { await users.AddToRoleAsync(pm, "Burnaby Admin"); } catch (InvalidOperationException) { roleRejected = true; }
            Require(roleRejected, "Identity store rejects a foreign named tenant role.");
            checks.Add("Identity hashing, required role memberships, system scope, canonical municipal assignments and foreign tenant-role rejection verified.");

            var rejects = new (string Name, string Sql)[] {
                ("unique normalized slug", "UPDATE Municipalities SET Slug='surrey' WHERE Slug='burnaby'"),
                ("lowercase slug", "UPDATE Municipalities SET Slug='BURNABY' WHERE Slug='burnaby'"),
                ("slug characters", "UPDATE Municipalities SET Slug='bad/slug' WHERE Slug='burnaby'"),
                ("reserved slug", "UPDATE Municipalities SET Slug='admin' WHERE Slug='burnaby'"),
                ("duplicate office within tenant", "INSERT Positions(MunicipalityId,Name,SeatsAvailable) SELECT TOP(1) MunicipalityId,Name,SeatsAvailable FROM Positions"),
                ("positive seat count", "UPDATE Positions SET SeatsAvailable=0 WHERE PositionId=(SELECT MIN(PositionId) FROM Positions)"),
                ("candidate tenant/position composite FK", "UPDATE Candidates SET PositionId=(SELECT MIN(PositionId) FROM Positions WHERE MunicipalityId=(SELECT MunicipalityId FROM Municipalities WHERE Slug='surrey')) WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates WHERE MunicipalityId=(SELECT MunicipalityId FROM Municipalities WHERE Slug='burnaby'))"),
                ("municipality FK", "UPDATE Positions SET MunicipalityId=2147483647 WHERE PositionId=(SELECT MIN(PositionId) FROM Positions)"),
                ("required candidate name", "UPDATE Candidates SET Name=' ' WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates)"),
                ("maximum candidate name length", "UPDATE Candidates SET Name=REPLICATE('x',101) WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates)"),
                ("HTTP(S) website", "UPDATE Candidates SET Website='javascript:alert(1)' WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates)"),
                ("basic email integrity", "UPDATE Candidates SET Email='invalid' WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates)"),
                ("safe stored image filename", "UPDATE Candidates SET PhotoFileName='../other.png' WHERE CandidateId=(SELECT MIN(CandidateId) FROM Candidates)"),
                ("position delete restriction", "DELETE Positions WHERE PositionId=(SELECT MIN(PositionId) FROM Candidates)"),
                ("municipality delete restriction", "DELETE Municipalities WHERE Slug='burnaby'"),
                ("municipal admin requires tenant", "UPDATE AspNetUsers SET MunicipalityId=NULL WHERE NormalizedUserName='PM'"),
                ("system admin cannot have tenant", "UPDATE AspNetUsers SET MunicipalityId=(SELECT MunicipalityId FROM Municipalities WHERE Slug='burnaby') WHERE NormalizedUserName='SUPER'"),
                ("administrator tenant FK", "UPDATE AspNetUsers SET MunicipalityId=2147483647 WHERE NormalizedUserName='PM'"),
                ("membership cannot elevate municipal scope", "INSERT AspNetUserRoles(UserId,RoleId,Scope) SELECT u.Id,r.Id,1 FROM AspNetUsers u CROSS JOIN AspNetRoles r WHERE u.NormalizedUserName='PM' AND r.NormalizedName='SUPER ADMIN'"),
                ("membership cannot mislabel system role", "INSERT AspNetUserRoles(UserId,RoleId,Scope) SELECT u.Id,r.Id,2 FROM AspNetUsers u CROSS JOIN AspNetRoles r WHERE u.NormalizedUserName='PM' AND r.NormalizedName='SUPER ADMIN'")
            };
            var beforeRejects = await Fingerprint(connection);
            foreach (var (name, sql) in rejects) { await RejectSql(connection, sql); checks.Add("SQL Server rejected: " + name); }
            Require(await Fingerprint(connection) == beforeRejects, "All negative SQL tests left existing records unchanged.");

            // Repeat setup while edits/deletion and a password change are held in rollback-only transactions.
            await using (var sql = new SqlConnection(connection))
            {
                await sql.OpenAsync();
                await using var transaction = (SqlTransaction)await sql.BeginTransactionAsync();
                await Command(sql, transaction, "UPDATE Candidates SET Profile='Edited sample retained during reseed' WHERE DemoSeedKey='river-shah'; DELETE Candidates WHERE DemoSeedKey='leo-faris';");
                await DatabaseInitialization.SeedAsync(services, configuration, true);
                Require(await Scalar(sql, transaction, "SELECT COUNT(*) FROM Candidates WHERE DemoSeedKey='leo-faris'") == 0, "Deleted seed sample was not recreated.");
                Require(await Scalar(sql, transaction, "SELECT COUNT(*) FROM Candidates WHERE DemoSeedKey='river-shah' AND Profile='Edited sample retained during reseed'") == 1, "Edited seed profile was not reset.");
                await transaction.RollbackAsync();
            }
            await using (var identityScope = services.CreateAsyncScope())
            {
                var identityDb = identityScope.ServiceProvider.GetRequiredService<MemsDbContext>();
                var manager = identityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                await identityDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var transaction = await identityDb.Database.BeginTransactionAsync();
                    var user = (await manager.FindByNameAsync("Richmondadmin"))!;
                    var replacement = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "cC3!";
                    Good(await manager.ChangePasswordAsync(user, password, replacement));
                    var hash = user.PasswordHash;
                    await DatabaseInitialization.SeedAsync(services, configuration, true);
                    await identityDb.Entry(user).ReloadAsync();
                    Require(user.PasswordHash == hash && await manager.CheckPasswordAsync(user, replacement), "Reseeding does not overwrite a changed administrator password.");
                    // Exercise actual Identity lockout without leaving the seeded user locked.
                    for (var attempt = 0; attempt < 5; attempt++) Good(await manager.AccessFailedAsync(user));
                    Require(await manager.IsLockedOutAsync(user), "Identity locks out after five failures.");
                    await transaction.RollbackAsync();
                });
            }
            Require(await Fingerprint(connection) == beforeRejects, "Rollback-only sample/password tests restored the entire data fingerprint.");
            await DatabaseInitialization.InitializeAsync(services, configuration, true);
            Require(await Fingerprint(connection) == beforeRejects, "Repeated migration/seed startup is idempotent.");
            checks.Add("Deleted/edited seed samples and changed passwords survive repeat seeding; five-failure Identity lockout verified; all test mutations rolled back.");
            checks.Add("Repeated migration/startup left every seeded record and password hash unchanged.");
        }

        var artifact = Path.Combine(root, "artifacts", "phase2");
        Directory.CreateDirectory(artifact);
        var result = new { passed = true, mode, checks };
        await File.WriteAllTextAsync(Path.Combine(artifact, mode.TrimStart('-') + ".json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string description) { if (!condition) throw new InvalidOperationException("Database verification failed: " + description); }
    private static void Good(IdentityResult result) => Require(result.Succeeded, "Identity operation: " + string.Join(",", result.Errors.Select(x => x.Code)));
    private static async Task<int> Scalar(string connection, string sql)
    { await using var db = new SqlConnection(connection); await db.OpenAsync(); return await Scalar(db, null, sql); }
    private static async Task<int> Scalar(SqlConnection db, SqlTransaction? transaction, string sql)
    { await using var command = new SqlCommand(sql, db, transaction); return Convert.ToInt32(await command.ExecuteScalarAsync()); }
    private static async Task Command(SqlConnection db, SqlTransaction transaction, string sql)
    { await using var command = new SqlCommand(sql, db, transaction); await command.ExecuteNonQueryAsync(); }
    private static async Task RejectSql(string connection, string sql)
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await using var transaction = (SqlTransaction)await db.BeginTransactionAsync();
        var rejected = false;
        try { await Command(db, transaction, sql); }
        catch (SqlException ex) when (ex.Number is 547 or 2601 or 2627 or 2628 or 8152) { rejected = true; }
        finally { await transaction.RollbackAsync(); }
        Require(rejected, "Expected a constraint/length rejection, not a successful write.");
    }
    internal static async Task<string> Fingerprint(string connection)
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT (SELECT * FROM Municipalities ORDER BY MunicipalityId FOR JSON PATH) Municipalities,
              (SELECT * FROM Positions ORDER BY PositionId FOR JSON PATH) Positions,
              (SELECT * FROM Candidates ORDER BY CandidateId FOR JSON PATH) Candidates,
              (SELECT * FROM AspNetUsers ORDER BY Id FOR JSON PATH) Users,
              (SELECT * FROM AspNetRoles ORDER BY Id FOR JSON PATH) Roles,
              (SELECT * FROM AspNetUserRoles ORDER BY UserId,RoleId FOR JSON PATH) Memberships,
              (SELECT * FROM SeedHistory ORDER BY SeedKey FOR JSON PATH) Seeds
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            """, db);
        var builder = new StringBuilder();
        await using var rows = await command.ExecuteReaderAsync();
        while (await rows.ReadAsync()) builder.Append(rows.GetString(0));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
