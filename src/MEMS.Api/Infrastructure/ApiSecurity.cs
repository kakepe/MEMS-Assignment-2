using MEMS.Data.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MEMS.Api.Infrastructure;

public sealed record AdministratorRequirement(bool SuperOnly) : IAuthorizationRequirement;
public sealed class AdministratorHandler(ElectionAccess access) : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,AdministratorRequirement requirement)
    {
        try { await access.RequireAdminAsync(context.User,requirement.SuperOnly);context.Succeed(requirement); }
        catch(ApiFault) { context.Fail(); }
    }
}

public sealed class ApiCsrfFilter(IAntiforgery antiforgery,IHostEnvironment environment) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request=context.HttpContext.Request;
        if(HttpMethods.IsGet(request.Method)||HttpMethods.IsHead(request.Method)||HttpMethods.IsOptions(request.Method))return;
        var origin=request.Headers.Origin.ToString();
        var self=$"{request.Scheme}://{request.Host}";
        var localOrigins=new[]{"https://localhost:7192","https://localhost:7252","https://localhost:7290"};
        if(origin.Length>0 && !string.Equals(origin,self,StringComparison.OrdinalIgnoreCase) && !(environment.IsDevelopment()&&localOrigins.Contains(origin,StringComparer.OrdinalIgnoreCase)))
        { context.Result=new ObjectResult(new ProblemDetails{Status=403,Title="Forbidden",Detail="This request origin is not permitted."}){StatusCode=403};return; }
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch(AntiforgeryValidationException)
        { context.Result=new ObjectResult(new ProblemDetails{Status=400,Title="Bad Request",Detail="A valid X-CSRF-TOKEN header and matching cookie are required. Fetch a fresh token after sign-in."}){StatusCode=400}; }
    }
}
