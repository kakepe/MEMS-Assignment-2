using System.ComponentModel.DataAnnotations;
using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

public sealed class UploadRequest { [Required] public IFormFile File { get; set; } = null!; }

[ApiController,RequestSizeLimit(3*1024*1024),RequestFormLimits(MultipartBodyLengthLimit=3*1024*1024)]
[ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403),ProducesResponseType<ProblemDetails>(404),ProducesResponseType<ProblemDetails>(409),ProducesResponseType<ProblemDetails>(413),ProducesResponseType<ProblemDetails>(500)]
public sealed class MediaController(ElectionApiService service) : ControllerBase
{
    [HttpGet("api/{slug}/candidates/{id:int}/photo"),AllowAnonymous,ProducesResponseType(200)]
    public async Task<IActionResult> Photo(string slug,int id)=>await Read(slug,id,false);
    [HttpGet("api/{slug}/municipalities/{id:int}/logo"),AllowAnonymous,ProducesResponseType(200)]
    public async Task<IActionResult> Logo(string slug,int id)=>await Read(slug,id,true);
    private async Task<IActionResult> Read(string slug,int id,bool logo)
    {var value=await service.ReadMediaAsync(User,slug,id,logo);Response.Headers.ContentSecurityPolicy="default-src 'none'; sandbox";return PhysicalFile(value.Path,value.ContentType);}

    [HttpPost("api/{slug}/candidates/{id:int}/photo"),Authorize(Policy="Administrator"),Consumes("multipart/form-data"),ProducesResponseType<CandidateDto>(200)]
    public async Task<ActionResult<CandidateDto>> UploadPhoto(string slug,int id,[FromForm]UploadRequest request,[FromHeader(Name="If-Match")]string? version,CancellationToken cancellationToken)
    {var value=(CandidateDto)await service.ChangeMediaAsync(User,slug,id,false,request.File,version,cancellationToken);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpDelete("api/{slug}/candidates/{id:int}/photo"),Authorize(Policy="Administrator"),ProducesResponseType<CandidateDto>(200)]
    public async Task<ActionResult<CandidateDto>> DeletePhoto(string slug,int id,[FromHeader(Name="If-Match")]string? version,CancellationToken cancellationToken)
    {var value=(CandidateDto)await service.ChangeMediaAsync(User,slug,id,false,null,version,cancellationToken);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpPost("api/{slug}/municipalities/{id:int}/logo"),Authorize(Policy="SuperAdmin"),Consumes("multipart/form-data"),ProducesResponseType<MunicipalityDto>(200)]
    public async Task<ActionResult<MunicipalityDto>> UploadLogo(string slug,int id,[FromForm]UploadRequest request,[FromHeader(Name="If-Match")]string? version,CancellationToken cancellationToken)
    {var value=(MunicipalityDto)await service.ChangeMediaAsync(User,slug,id,true,request.File,version,cancellationToken);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpDelete("api/{slug}/municipalities/{id:int}/logo"),Authorize(Policy="SuperAdmin"),ProducesResponseType<MunicipalityDto>(200)]
    public async Task<ActionResult<MunicipalityDto>> DeleteLogo(string slug,int id,[FromHeader(Name="If-Match")]string? version,CancellationToken cancellationToken)
    {var value=(MunicipalityDto)await service.ChangeMediaAsync(User,slug,id,true,null,version,cancellationToken);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
}
