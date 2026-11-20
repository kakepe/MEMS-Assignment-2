using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

[ApiController,Produces("application/json"),RequestSizeLimit(1048576)]
[ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403),ProducesResponseType<ProblemDetails>(404),ProducesResponseType<ProblemDetails>(409),ProducesResponseType<ProblemDetails>(500)]
public sealed class MunicipalitiesController(ElectionApiService service) : ControllerBase
{
    [HttpGet("api/municipalities"),AllowAnonymous,EndpointSummary("Safe tenant-discovery directory; authenticated municipal admins see only their assignment.")]
    [ProducesResponseType<List<MunicipalityDirectoryItem>>(200)]
    public async Task<ActionResult<List<MunicipalityDirectoryItem>>> Directory()=>await service.DirectoryAsync(User);

    [HttpGet("api/{slug}/municipalities"),AllowAnonymous,EndpointSummary("Return only the active municipality.")]
    [ProducesResponseType<List<MunicipalityDto>>(200)]
    public async Task<ActionResult<List<MunicipalityDto>>> TenantList(string slug)=>await service.MunicipalitiesAsync(User,slug);

    [HttpGet("api/{slug}/municipalities/{id:int}"),AllowAnonymous,ProducesResponseType<MunicipalityDto>(200)]
    public async Task<ActionResult<MunicipalityDto>> TenantGet(string slug,int id)
    {var value=await service.MunicipalityAsync(User,slug,id);Response.Headers.ETag=$"\"{value.Version}\"";return value;}

    [HttpGet("api/admin/municipalities"),Authorize(Policy="SuperAdmin"),EndpointSummary("System-wide municipality administration list; Super Admin only.")]
    [ProducesResponseType<List<MunicipalityDto>>(200)]
    public async Task<ActionResult<List<MunicipalityDto>>> List()=>await service.MunicipalitiesAsync(User,null);

    [HttpGet("api/admin/municipalities/{id:int}"),Authorize(Policy="SuperAdmin"),ProducesResponseType<MunicipalityDto>(200)]
    public async Task<ActionResult<MunicipalityDto>> Get(int id)
    {var value=await service.MunicipalityAsync(User,null,id);Response.Headers.ETag=$"\"{value.Version}\"";return value;}

    [HttpPost("api/admin/municipalities"),Authorize(Policy="SuperAdmin"),ProducesResponseType<MunicipalityDto>(201)]
    public async Task<ActionResult<MunicipalityDto>> Create(MunicipalityRequest request)
    {var value=await service.CreateMunicipalityAsync(User,request);Response.Headers.ETag=$"\"{value.Version}\"";return Created($"/api/admin/municipalities/{value.MunicipalityId}",value);}

    [HttpPut("api/admin/municipalities/{id:int}"),Authorize(Policy="SuperAdmin"),EndpointDescription("CSRF and the current quoted ETag in If-Match are required."),ProducesResponseType<MunicipalityDto>(200)]
    public async Task<ActionResult<MunicipalityDto>> Update(int id,MunicipalityRequest request,[FromHeader(Name="If-Match")]string? version)
    {var value=await service.UpdateMunicipalityAsync(User,id,request,version);Response.Headers.ETag=$"\"{value.Version}\"";return value;}

    [HttpDelete("api/admin/municipalities/{id:int}"),Authorize(Policy="SuperAdmin"),ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id,[FromHeader(Name="If-Match")]string? version)
    {await service.DeleteMunicipalityAsync(User,id,version);return NoContent();}
}
