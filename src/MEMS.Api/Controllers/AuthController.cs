using MEMS.Api.Infrastructure;
using MEMS.Data.Identity;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MEMS.Api.Controllers;

[ApiController,Route("api/auth"),Produces("application/json"),RequestSizeLimit(16384)]
public sealed class AuthController(IAntiforgery antiforgery,SignInManager<ApplicationUser> signIn,UserManager<ApplicationUser> users,ElectionAccess access) : ControllerBase
{
    [HttpGet("csrf"),AllowAnonymous,EndpointSummary("Issue an anti-CSRF token; fetch again after login.")]
    [ProducesResponseType<CsrfDto>(200)]
    public ActionResult<CsrfDto> Csrf()
    { var tokens=antiforgery.GetAndStoreTokens(HttpContext);return new CsrfDto(tokens.RequestToken!,"X-CSRF-TOKEN"); }

    [HttpPost("login"),AllowAnonymous,EnableRateLimiting("login"),EndpointSummary("Sign in with Identity credentials and a valid CSRF token.")]
    [ProducesResponseType<SessionDto>(200),ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403),ProducesResponseType<ProblemDetails>(429)]
    public async Task<ActionResult<SessionDto>> Login(LoginRequest request)
    {
        ContractValidation.Check(request);
        var user=await users.FindByNameAsync(request.UserName);
        if(user is null)throw new ApiFault(401,"Invalid credentials or sign-in is unavailable.");
        var result=await signIn.CheckPasswordSignInAsync(user,request.Password,lockoutOnFailure:true);
        if(!result.Succeeded)throw new ApiFault(401,"Invalid credentials or sign-in is unavailable.");
        var principal=await signIn.CreateUserPrincipalAsync(user);
        var actor=await access.RequireAdminAsync(principal);
        await signIn.SignInAsync(user,isPersistent:false);
        return new SessionDto(actor.UserName,actor.IsSuperAdmin,actor.MunicipalityId);
    }

    [HttpGet("me"),Authorize(Policy="Administrator"),EndpointSummary("Return only the current administrator's safe session information.")]
    [ProducesResponseType<SessionDto>(200),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<SessionDto>> Me()
    { var actor=await access.RequireAdminAsync(User);return new SessionDto(actor.UserName,actor.IsSuperAdmin,actor.MunicipalityId); }

    [HttpPost("logout"),Authorize(Policy="Administrator"),EndpointSummary("End the authenticated cookie session; CSRF required.")]
    [ProducesResponseType(204),ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401)]
    public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return NoContent();}
}
