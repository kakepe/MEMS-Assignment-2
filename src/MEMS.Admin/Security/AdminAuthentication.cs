using MEMS.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace MEMS.Admin.Security;

public sealed record AdminRequirement(bool SuperOnly) : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(IServiceScopeFactory scopes) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        await using var scope = scopes.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ElectionAccess>().RequireAdminAsync(context.User, requirement.SuperOnly);
            context.Succeed(requirement);
        }
        catch (ApiFault) { context.Fail(); }
    }
}

// Periodic display-state refresh plus an independent fresh authorization check on every operation.
public sealed class AdminAuthenticationStateProvider(ILoggerFactory logger, IServiceScopeFactory scopes)
    : RevalidatingServerAuthenticationStateProvider(logger)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ElectionAccess>().RequireAdminAsync(state.User);
            return true;
        }
        catch (ApiFault) { return false; }
    }
}

public static class AccountNavigation
{
    public static string LocalReturnUrl(string? value) => value is not null
        && (value == "/admin" || value.StartsWith("/admin/", StringComparison.Ordinal) || value.StartsWith("/admin?", StringComparison.Ordinal))
        && !value.Any(c => char.IsControl(c) || c == '\\') ? value : "/admin";
}
