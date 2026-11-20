using System.ComponentModel.DataAnnotations;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data;

public sealed class MemsDbContext(DbContextOptions<MemsDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>,
        ApplicationUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>(options)
{
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<SeedHistory> SeedHistory => Set<SeedHistory>();

    public int? CurrentTenantId { get; private set; }
    public bool SystemAccess { get; private set; }

    // Each context gets one immutable scope. Phase 3 must authorize the requested tenant first.
    public void UseTenant(int municipalityId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(municipalityId);
        if (SystemAccess || CurrentTenantId.HasValue) throw new InvalidOperationException("Context scope is already assigned.");
        CurrentTenantId = municipalityId;
    }

    internal void UseSystemScope()
    {
        if (CurrentTenantId.HasValue) throw new InvalidOperationException("Cannot elevate a tenant context.");
        SystemAccess = true;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Bound composite Identity keys for SQL Server's clustered-index size limit.
        builder.Entity<ApplicationUser>().Property(x => x.Id).HasMaxLength(128);
        builder.Entity<ApplicationRole>().Property(x => x.Id).HasMaxLength(128);
        builder.Entity<ApplicationUserRole>().Property(x => x.UserId).HasMaxLength(128);
        builder.Entity<ApplicationUserRole>().Property(x => x.RoleId).HasMaxLength(128);
        builder.Entity<IdentityUserClaim<string>>().Property(x => x.UserId).HasMaxLength(128);
        builder.Entity<IdentityRoleClaim<string>>().Property(x => x.RoleId).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(x => x.UserId).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        builder.Entity<IdentityUserToken<string>>().Property(x => x.UserId).HasMaxLength(128);
        builder.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        builder.ApplyConfigurationsFromAssembly(typeof(MemsDbContext).Assembly);
        builder.Entity<Municipality>().HasQueryFilter(x => SystemAccess || x.MunicipalityId == CurrentTenantId);
        builder.Entity<Position>().HasQueryFilter(x => SystemAccess || x.MunicipalityId == CurrentTenantId);
        builder.Entity<Candidate>().HasQueryFilter(x => SystemAccess || x.MunicipalityId == CurrentTenantId);
    }

    private void ValidateWrites()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var tenant = entry.Entity switch
            {
                Municipality x => (int?)x.MunicipalityId,
                Position x => x.MunicipalityId,
                Candidate x => x.MunicipalityId,
                _ => null
            };
            if (entry.Entity is Municipality && !SystemAccess)
                throw new InvalidOperationException("Municipality changes require authorized system scope.");
            if (entry.Entity is Position or Candidate && !SystemAccess)
            {
                var original = entry.State == EntityState.Added ? tenant : entry.Property("MunicipalityId").OriginalValue as int?;
                if (!CurrentTenantId.HasValue || tenant != CurrentTenantId || original != CurrentTenantId)
                    throw new InvalidOperationException("The record is outside the active tenant.");
            }
            if (entry.State == EntityState.Deleted) continue;
            switch (entry.Entity)
            {
                case Municipality x:
                    x.Name = x.Name.Trim(); x.Province = x.Province.Trim(); x.Description = x.Description.Trim(); x.PublicTitle = x.PublicTitle.Trim();
                    break;
                case Position x: x.Name = x.Name.Trim(); break;
                case Candidate x:
                    x.Name = x.Name.Trim(); x.Profile = x.Profile.Trim();
                    x.Email = string.IsNullOrWhiteSpace(x.Email) ? null : x.Email.Trim();
                    x.Website = string.IsNullOrWhiteSpace(x.Website) ? null : x.Website.Trim();
                    if (x.Website is not null && (!Uri.TryCreate(x.Website, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
                        throw new ValidationException("Use an HTTP or HTTPS website address.");
                    break;
            }
            Validator.ValidateObject(entry.Entity, new ValidationContext(entry.Entity), validateAllProperties: true);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) { ValidateWrites(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { ValidateWrites(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
}
