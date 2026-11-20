using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MEMS.Data.Identity;

// Shared local session revocation protects already-connected circuits without changing user records.
// This is development single-machine storage, alongside (but separate from) the protected key ring.
public sealed partial class IdentitySessions(IConfiguration configuration)
{
    public const string ClaimType = "mems:session";
    public const string ExpiresClaimType = "mems:session-expires";
    private readonly string directory = CreateDirectory(configuration);

    private static string CreateDirectory(IConfiguration configuration)
    {
        var local = configuration["Mems:LocalStorage"]
            ?? throw new InvalidOperationException("Configure application-owned Identity session storage.");
        return Directory.CreateDirectory(Path.Combine(local, "identity-sessions")).FullName;
    }

    [GeneratedRegex("^[a-f0-9]{32}$")]
    private static partial Regex SessionId();

    public bool IsActive(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimType);
        return id is not null && SessionId().IsMatch(id)
            && long.TryParse(principal.FindFirstValue(ExpiresClaimType), out var expiry)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expiry && Directory.Exists(directory)
            && !File.Exists(Path.Combine(directory, id + ".revoked"));
    }

    public void Revoke(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimType);
        if (id is not null && SessionId().IsMatch(id))
            File.WriteAllText(Path.Combine(directory, id + ".revoked"), DateTime.UtcNow.ToString("O"));
    }
}

internal sealed class SessionCookieOptions(IdentitySessions sessions) : IPostConfigureOptions<CookieAuthenticationOptions>
{
    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (name != IdentityConstants.ApplicationScheme) return;
        var signingIn = options.Events.OnSigningIn;
        var signingOut = options.Events.OnSigningOut;
        var validate = options.Events.OnValidatePrincipal;
        options.Events.OnSigningIn = async context =>
        {
            await signingIn(context);
            if (!context.Principal!.HasClaim(x => x.Type == IdentitySessions.ClaimType))
            {
                // Switching accounts in the same browser also ends its previous circuit session.
                sessions.Revoke(context.HttpContext.User);
                ((ClaimsIdentity)context.Principal.Identity!).AddClaim(new(IdentitySessions.ClaimType, Guid.NewGuid().ToString("N")));
                var expiry = context.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(options.ExpireTimeSpan);
                ((ClaimsIdentity)context.Principal.Identity!).AddClaim(new(IdentitySessions.ExpiresClaimType,
                    expiry.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
        };
        options.Events.OnSigningOut = async context =>
        {
            sessions.Revoke(context.HttpContext.User);
            await signingOut(context);
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            await validate(context); // Retain Identity's real security-stamp validation.
            if (context.Principal is not null && !sessions.IsActive(context.Principal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        };
    }
}
