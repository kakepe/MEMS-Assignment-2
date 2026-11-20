using MEMS.Data;
var verificationMode = args.FirstOrDefault(x => x is "--verify-phase2" or "--prepare-phase2-restart" or "--complete-phase2-restart" or "--prepare-phase3" or "--complete-phase3");
var builder = WebApplication.CreateBuilder(args.Where(x => x != verificationMode).ToArray());
if (verificationMode is not null)
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.AddServiceDefaults();
builder.Services.AddMemsData(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddControllers(options=>options.Filters.Add<MEMS.Api.Infrastructure.ApiCsrfFilter>())
    .AddJsonOptions(options=>options.JsonSerializerOptions.UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
MEMS.Api.Infrastructure.ApiDocumentation.AddMemsOpenApi(builder.Services);
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,MEMS.Api.Infrastructure.AdministratorHandler>();
builder.Services.AddAuthorization(options=>
{
    options.AddPolicy("Administrator",policy=>policy.RequireAuthenticatedUser().AddRequirements(new MEMS.Api.Infrastructure.AdministratorRequirement(false)));
    options.AddPolicy("SuperAdmin",policy=>policy.RequireAuthenticatedUser().AddRequirements(new MEMS.Api.Infrastructure.AdministratorRequirement(true)));
});
builder.Services.AddAntiforgery(options=>
{
    options.HeaderName="X-CSRF-TOKEN";options.Cookie.Name=".MEMS.Assignment2.Csrf";
    options.Cookie.HttpOnly=true;options.Cookie.SecurePolicy=CookieSecurePolicy.Always;options.Cookie.SameSite=SameSiteMode.Strict;
});
builder.Services.AddRateLimiter(options=>
{
    options.RejectionStatusCode=429;
    options.AddPolicy("login",context=>System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new System.Threading.RateLimiting.FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
    options.OnRejected=async(context,cancellationToken)=>await MEMS.Api.Infrastructure.ApiProblems.WriteAsync(context.HttpContext,429,"Too many sign-in attempts. Try again later.");
});
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command",LogLevel.Critical);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update",LogLevel.Critical);
var app = builder.Build();
if (verificationMode is not null)
{
    if(verificationMode is "--prepare-phase3" or "--complete-phase3")
        await MEMS.Api.Verification.Phase3FixtureChecks.RunAsync(app.Services,app.Configuration,app.Environment.IsDevelopment(),verificationMode=="--prepare-phase3");
    else await MEMS.Api.Verification.Phase2DatabaseChecks.RunAsync(app.Services, app.Configuration, app.Environment.IsDevelopment(), verificationMode);
    return;
}
if (builder.Configuration.GetValue<bool>("Database:Initialize"))
    await MEMS.Data.Seeding.DatabaseInitialization.InitializeAsync(app.Services, app.Configuration, app.Environment.IsDevelopment());
app.UseMiddleware<MEMS.Api.Infrastructure.ApiProblems>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapGet("/api/docs",()=>Results.Content(MEMS.Api.Infrastructure.ApiDocumentation.Html,"text/html")).ExcludeFromDescription();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();

