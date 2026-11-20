using MEMS.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MEMS.Data.Configuration;

internal sealed class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.HasAlternateKey(x => new { x.Id, x.Scope });
        entity.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("AspNetUsers", t => t.HasCheckConstraint("CK_Users_TenantAssignment",
            "([Scope] = 1 AND [MunicipalityId] IS NULL) OR ([Scope] = 2 AND [MunicipalityId] IS NOT NULL)"));
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> entity)
    {
        entity.HasAlternateKey(x => new { x.Id, x.Scope });
        entity.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("AspNetRoles", t => t.HasCheckConstraint("CK_Roles_Scope", "[Scope] IN (1,2) AND ([Scope] = 2 OR [MunicipalityId] IS NULL)"));
    }
}

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> entity)
    {
        // Replace Identity's single-column links with scope-consistent composite links.
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => new { x.UserId, x.Scope })
            .HasPrincipalKey(x => new { x.Id, x.Scope }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => new { x.RoleId, x.Scope })
            .HasPrincipalKey(x => new { x.Id, x.Scope }).OnDelete(DeleteBehavior.Restrict);
    }
}
