using MEMS.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data.Services;

public sealed record TenantSummary(int MunicipalityId, string Slug, string Name, string BrandColor);

public sealed class TenantElectionReader(IDbContextFactory<MemsDbContext> factory)
{
    // Narrow, deliberate bypass for public slug resolution; never returns global candidate data.
    public async Task<TenantSummary?> ResolveAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Municipalities.IgnoreQueryFilters().Where(x => x.Slug == normalized)
            .Select(x => new TenantSummary(x.MunicipalityId, x.Slug, x.Name, x.BrandColor)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Position>> PositionsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.UseTenant(tenantId);
        return await db.Positions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<List<Candidate>> CandidatesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.UseTenant(tenantId);
        return await db.Candidates.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.CandidateId).ToListAsync(cancellationToken);
    }

    public async Task<Candidate?> CandidateAsync(int tenantId, int candidateId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.UseTenant(tenantId);
        return await db.Candidates.AsNoTracking().SingleOrDefaultAsync(x => x.CandidateId == candidateId, cancellationToken);
    }
}
