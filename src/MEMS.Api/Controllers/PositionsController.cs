using MEMS.Data.Services;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MEMS.Api.Controllers;

[ApiController,Route("api/{slug}/positions"),Produces("application/json"),RequestSizeLimit(1048576)]
[ProducesResponseType<ProblemDetails>(400),ProducesResponseType<ProblemDetails>(401),ProducesResponseType<ProblemDetails>(403),ProducesResponseType<ProblemDetails>(404),ProducesResponseType<ProblemDetails>(409),ProducesResponseType<ProblemDetails>(500)]
public sealed class PositionsController(ElectionApiService service) : ControllerBase
{
    [HttpGet,AllowAnonymous,ProducesResponseType<List<PositionDto>>(200)]
    public async Task<ActionResult<List<PositionDto>>> List(string slug)=>await service.PositionsAsync(User,slug);
    [HttpGet("{id:int}"),AllowAnonymous,ProducesResponseType<PositionDto>(200)]
    public async Task<ActionResult<PositionDto>> Get(string slug,int id)
    {var value=await service.PositionAsync(User,slug,id);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpPost,Authorize(Policy="Administrator"),ProducesResponseType<PositionDto>(201)]
    public async Task<ActionResult<PositionDto>> Create(string slug,PositionRequest request)
    {var value=await service.SavePositionAsync(User,slug,null,request,null);Response.Headers.ETag=$"\"{value.Version}\"";return Created($"/api/{slug}/positions/{value.PositionId}",value);}
    [HttpPut("{id:int}"),Authorize(Policy="Administrator"),ProducesResponseType<PositionDto>(200)]
    public async Task<ActionResult<PositionDto>> Update(string slug,int id,PositionRequest request,[FromHeader(Name="If-Match")]string? version)
    {var value=await service.SavePositionAsync(User,slug,id,request,version);Response.Headers.ETag=$"\"{value.Version}\"";return value;}
    [HttpDelete("{id:int}"),Authorize(Policy="Administrator"),ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string slug,int id,[FromHeader(Name="If-Match")]string? version)
    {await service.DeletePositionAsync(User,slug,id,version);return NoContent();}
}
