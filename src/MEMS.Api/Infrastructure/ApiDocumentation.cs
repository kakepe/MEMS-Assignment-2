using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace MEMS.Api.Infrastructure;

public static class ApiDocumentation
{
    public static IServiceCollection AddMemsOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options=>
        {
            options.AddDocumentTransformer((document,context,cancellationToken)=>
            {
                document.Info.Title="MEMS tenant-aware election API";document.Info.Version="v1";
                document.Info.Description="Anonymous election reads are scoped by municipality slug. Municipal administrators are restricted to their stored assignment, including public routes. Municipality management is Super Admin only. Sign in using /api/auth/login and Identity cookies; fetch /api/auth/csrf before login and again afterward. Every modifying request requires X-CSRF-TOKEN. PUT, DELETE and media changes also require the current quoted ETag in If-Match. No ballot selections are accepted or stored.";
                document.Components??=new();document.Components.SecuritySchemes??=new Dictionary<string,IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["IdentityCookie"]=new OpenApiSecurityScheme {Type=SecuritySchemeType.ApiKey,In=ParameterLocation.Cookie,Name=".MEMS.Assignment2.Identity",Description="Secure HttpOnly Identity session, issued only by legitimate sign-in."};
                document.Components.SecuritySchemes["CsrfHeader"]=new OpenApiSecurityScheme {Type=SecuritySchemeType.ApiKey,In=ParameterLocation.Header,Name="X-CSRF-TOKEN",Description="Request token from /api/auth/csrf; the matching secure cookie is also required. Refresh after login."};
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation,context,cancellationToken)=>
            {
                var administrator=context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
                var modifying=context.Description.HttpMethod is "POST" or "PUT" or "DELETE" or "PATCH";
                if(administrator||modifying)
                {
                    var requirement=new OpenApiSecurityRequirement();
                    if(administrator)requirement[new OpenApiSecuritySchemeReference("IdentityCookie",context.Document)]=[];
                    if(modifying)requirement[new OpenApiSecuritySchemeReference("CsrfHeader",context.Document)]=[];
                    operation.Security=[requirement];
                }
                if(context.Description.RelativePath?.Contains("{slug}",StringComparison.Ordinal)==true)
                    operation.Description=(operation.Description??"")+" The URL slug defines the tenant; foreign record IDs never escape this scope. Unknown slugs return 404; authenticated municipal administrators requesting another tenant receive 403.";
                return Task.CompletedTask;
            });
        });
        return services;
    }
    public const string Html="""
        <!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <title>MEMS API documentation</title><body><main><h1>MEMS API v1</h1>
        <p><a href="/openapi/v1.json">OpenAPI 3 specification: endpoints, models, security and response codes</a></p>
        <h2>Authentication</h2><ol><li>GET /api/auth/csrf and retain the matching cookies.</li>
        <li>POST /api/auth/login with username/password JSON and X-CSRF-TOKEN.</li>
        <li>GET /api/auth/csrf again after login. Send its token with every modifying request.</li></ol>
        <p>Cookies are Secure and HttpOnly. No registration, password-reset backdoor, JWT or test authentication endpoint is provided.</p>
        <h2>Routes</h2><ul><li>GET /api/municipalities: safe tenant directory</li>
        <li>GET /api/{slug}/municipalities and /api/{slug}/municipalities/{id}: tenant details</li>
        <li>GET, POST /api/admin/municipalities; GET, PUT, DELETE /api/admin/municipalities/{id}: Super Admin only</li>
        <li>GET, POST /api/{slug}/positions and /candidates; GET, PUT, DELETE their /{id} routes</li>
        <li>GET, POST, DELETE /api/{slug}/candidates/{id}/photo and /api/{slug}/municipalities/{id}/logo</li></ul>
        <p>GET election routes permit anonymous browsing. Signed-in municipal administrators remain restricted to their assigned tenant. Logos can only be changed by Super Admin.</p>
        <h2>Concurrency and images</h2><p>GET/create/update returns a version and ETag. PUT, DELETE and media changes require the current quoted ETag as If-Match; stale versions return 409.</p>
        <p>Upload multipart field <code>file</code>: JPEG/PNG, maximum 2 MB, decoded content matching the extension, at most 6000 pixels per dimension and 16 megapixels. Images are re-encoded to PNG under generated tenant-owned names. Seeded assets are never deleted by upload cleanup.</p>
        <h2>Candidate query</h2><p>/api/burnaby/candidates?search=Vivian&amp;positionId=1&amp;page=1&amp;pageSize=12&amp;sort=name. Sort supports name or created; ties use candidate ID. Page size is limited to 100.</p>
        <h2>Errors</h2><p>JSON Problem Details: 400 invalid input/CSRF, 401 authentication required/failed, 403 permission denied, 404 missing tenant/record, 409 duplicate/stale/dependent data, 413 oversized upload, 429 login rate limit, 500 safe generic internal error.</p>
        <p>No election selections or vote totals are sent to or stored by this API. Final Blazor screens belong to later phases.</p></main></body></html>
        """;
}
