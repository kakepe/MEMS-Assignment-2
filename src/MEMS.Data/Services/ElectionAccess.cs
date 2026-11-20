using System.Security.Claims;
using MEMS.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MEMS.Data.Services;

public sealed record ElectionActor(string UserName, bool IsSuperAdmin, int? MunicipalityId, bool Authenticated);

public sealed class ElectionAccess(UserManager<ApplicationUser> users, IOptions<IdentityOptions> options,
    IDbContextFactory<MemsDbContext> factory, TenantElectionReader reader, IdentitySessions sessions)
{
    public async Task<ElectionActor> ActorAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return new("", false, null, false);
        // A principal created internally before SignInAsync has no session yet; network cookies
        // acquire and validate their session in SessionCookieOptions. Circuits retain that ID.
        if (principal.HasClaim(x => x.Type == IdentitySessions.ClaimType) && !sessions.IsActive(principal))
            throw new ApiFault(401, "Your session ended. Sign in again to continue.");
        var user = await users.GetUserAsync(principal);
        if (user is null || principal.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType) != user.SecurityStamp)
            throw new ApiFault(401, "Sign in again to continue.");
        if (user.Scope == AdministratorScope.System && user.MunicipalityId is null && await users.IsInRoleAsync(user, AdministratorRoles.SuperAdmin))
            return new(user.UserName!, true, null, true);
        if (user.Scope == AdministratorScope.Municipality && user.MunicipalityId.HasValue && await users.IsInRoleAsync(user, AdministratorRoles.MunicipalityAdmin))
            return new(user.UserName!, false, user.MunicipalityId, true);
        throw new ApiFault(403, "This account has no valid administrator assignment.");
    }

    public async Task<ElectionActor> RequireAdminAsync(ClaimsPrincipal principal, bool super = false)
    {
        var actor = await ActorAsync(principal);
        if (!actor.Authenticated) throw new ApiFault(401, "Authentication is required.");
        if (super && !actor.IsSuperAdmin) throw new ApiFault(403, "Super Admin permission is required.");
        return actor;
    }

    public async Task<TenantSummary> TenantAsync(ClaimsPrincipal principal, string slug, bool write = false)
    {
        var actor = write ? await RequireAdminAsync(principal) : await ActorAsync(principal);
        if (slug.Length > 80 || !System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ApiFault(404, "Municipality not found.");
        var tenant = await reader.ResolveAsync(slug) ?? throw new ApiFault(404, "Municipality not found.");
        if (actor.Authenticated && !actor.IsSuperAdmin && actor.MunicipalityId != tenant.MunicipalityId)
            throw new ApiFault(403, "This municipality is outside your assignment.");
        return tenant;
    }

    internal async Task<MemsDbContext> SystemDbAsync(ClaimsPrincipal principal)
    {
        await RequireAdminAsync(principal, super: true);
        var db = await factory.CreateDbContextAsync();
        db.UseSystemScope();
        return db;
    }

    internal async Task<MemsDbContext> TenantDbAsync(int municipalityId)
    {
        var db = await factory.CreateDbContextAsync();
        db.UseTenant(municipalityId);
        return db;
    }
}
