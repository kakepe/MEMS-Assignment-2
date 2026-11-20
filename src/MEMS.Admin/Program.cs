using MEMS.Admin.Components;
using MEMS.Data;
using MEMS.Admin.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddMemsData(builder.Configuration);
builder.Services.AddHttpClient("mems-api", client => client.BaseAddress = new Uri("https+http://mems-api"));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, AdminAuthenticationStateProvider>();
builder.Services.AddScoped<MEMS.Admin.Services.AdminOperations>();
builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", p => p.RequireAuthenticatedUser().AddRequirements(new AdminRequirement(false)));
    options.AddPolicy("SuperAdmin", p => p.RequireAuthenticatedUser().AddRequirements(new AdminRequirement(true)));
});
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".MEMS.Assignment2.AdminCsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/account/login";
    options.AccessDeniedPath = "/admin/access-denied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/admin/media") || context.Request.Path.StartsWithSegments("/_blazor"))
            context.Response.StatusCode = 401;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/admin/media")) context.Response.StatusCode = 403;
        else context.Response.Redirect("/admin/access-denied");
        return Task.CompletedTask;
    };
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        context.Request.Path == "/admin/account/login" && HttpMethods.IsPost(context.Request.Method)
        ? RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
        : RateLimitPartition.GetNoLimiter("other"));
});
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Critical);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Critical);
var app = builder.Build();
app.UseExceptionHandler("/admin/error", createScopeForErrors: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
    if (context.Request.Path.StartsWithSegments("/admin")) context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

