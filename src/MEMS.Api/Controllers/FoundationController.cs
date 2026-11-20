using MEMS.Data;
using MEMS.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Api.Controllers;

[ApiController]
[Route("api/foundation")]
public sealed class FoundationController(IDbContextFactory<MemsDbContext> factory, ILogger<FoundationController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FoundationStatus>> Get(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        logger.LogInformation("Phase {Phase} SQL connectivity probe completed: {Connected}", 1, connected);
        if (!connected) return Problem(statusCode: 503, title: "SQL Server is unavailable.");
        return new FoundationStatus("MEMS.Api", "Phase 1 infrastructure demonstration", true, DateTimeOffset.UtcNow);
    }
}
