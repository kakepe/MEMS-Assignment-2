using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

[ApiController,Route("api/{slug}/candidates"),Produces("application/json"),RequestSizeLimit(1048576)]
[ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403),ProducesResponseType<ProblemDetails>(404),ProducesResponseType<ProblemDetails>(409),ProducesResponseType<ProblemDetails>(500)]
public sealed class CandidatesController(ElectionApiService service) : ControllerBase
{
    [HttpGet,AllowAnonymous,EndpointSummary("Tenant-scoped name search, position filter and stable pagination."),ProducesResponseType<PageResult<CandidateDto>>(200)]
    public async Task<ActionResult<PageResult<CandidateDto>>> List(string slug,[FromQuery]CandidateQuery query)=>await service.CandidatesAsync(User,slug,query);
    [HttpGet("{id:int}"),AllowAnonymous,ProducesResponseType<CandidateDto>(200)]
    public async Task<ActionResult<CandidateDto>> Get(string slug,int id)
    {var value=await service.CandidateAsync(User,slug,id);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpPost,Authorize(Policy="Administrator"),ProducesResponseType<CandidateDto>(201)]
    public async Task<ActionResult<CandidateDto>> Create(string slug,CandidateRequest request)
    {var value=await service.SaveCandidateAsync(User,slug,null,request,null);Response.Headers.ETag=$"\"{value.Version}\"";return Created($"/api/{slug}/candidates/{value.CandidateId}",value);}
    [HttpPut("{id:int}"),Authorize(Policy="Administrator"),ProducesResponseType<CandidateDto>(200)]
    public async Task<ActionResult<CandidateDto>> Update(string slug,int id,CandidateRequest request,[FromHeader(Name="If-Match")]string? version)
    {var value=await service.SaveCandidateAsync(User,slug,id,request,version);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpDelete("{id:int}"),Authorize(Policy="Administrator"),ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string slug,int id,[FromHeader(Name="If-Match")]string? version)
    {await service.DeleteCandidateAsync(User,slug,id,version);return NoContent();}
}
