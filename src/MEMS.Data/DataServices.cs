using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MEMS.Data.Identity;
using MEMS.Data.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace MEMS.Data;

public static class DataServices
{
    public static IServiceCollection AddMemsData(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("mems")
            ?? throw new InvalidOperationException("Start through MEMS.AppHost to supply the mems SQL Server connection.");
        services.AddDbContextFactory<MemsDbContext>(options => options.UseSqlServer(connection,
            sql => sql.EnableRetryOnFailure()));
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<MemsDbContext>().AddUserStore<MemsUserStore>().AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ".MEMS.Assignment2.Identity";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        });
        var localStorage = configuration["Mems:LocalStorage"];
        if (!string.IsNullOrWhiteSpace(localStorage))
        {
            var protection = services.AddDataProtection().SetApplicationName("MEMS.Assignment2")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(localStorage, "identity-keys")));
            if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        }
        services.AddScoped<TenantElectionReader>();
        services.AddScoped<ElectionAccess>();
        services.AddScoped<ElectionApiService>();
        services.AddSingleton<MEMS.Data.Storage.MediaStore>();
        services.AddSingleton<IdentitySessions>();
        services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>, SessionCookieOptions>();
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
            options.OnRefreshingPrincipal = context =>
            {
                var session = context.CurrentPrincipal?.FindFirst(IdentitySessions.ClaimType);
                if (session is not null && context.NewPrincipal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                {
                    identity.AddClaim(session);
                    var expiry = context.CurrentPrincipal?.FindFirst(IdentitySessions.ExpiresClaimType);
                    if (expiry is not null) identity.AddClaim(expiry);
                }
                return Task.CompletedTask;
            };
        });
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Path = "/";
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        services.AddHealthChecks().AddCheck<SqlReadinessCheck>("sql-server", tags: ["ready"]);
        return services;
    }
}

internal sealed class SqlReadinessCheck(IDbContextFactory<MemsDbContext> factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server connection succeeded.")
                : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("SQL Server is unavailable.");
        }
    }
}
