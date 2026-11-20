using MEMS.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MEMS.Data.Identity;

public enum AdministratorScope { System = 1, Municipality = 2 }

public static class AdministratorRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string MunicipalityAdmin = "Municipality Admin";
}

public sealed class ApplicationUser : IdentityUser
{
    public AdministratorScope Scope { get; set; } = AdministratorScope.Municipality;
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
}

public sealed class ApplicationRole : IdentityRole
{
    public AdministratorScope Scope { get; set; } = AdministratorScope.Municipality;
    // Legacy named tenant groups remain compatible; canonical ownership is on ApplicationUser.
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }
}

public sealed class ApplicationUserRole : IdentityUserRole<string>
{
    public AdministratorScope Scope { get; set; }
}

public sealed class MemsUserStore(MemsDbContext context, IdentityErrorDescriber errors)
    : UserStore<ApplicationUser, ApplicationRole, MemsDbContext, string,
        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
        IdentityUserToken<string>, IdentityRoleClaim<string>>(context, errors)
{
    protected override ApplicationUserRole CreateUserRole(ApplicationUser user, ApplicationRole role)
    {
        if (user.Scope != role.Scope || (role.MunicipalityId.HasValue && role.MunicipalityId != user.MunicipalityId))
            throw new InvalidOperationException("The administrator assignment does not match this role.");
        return new() { UserId = user.Id, RoleId = role.Id, Scope = user.Scope };
    }
}
