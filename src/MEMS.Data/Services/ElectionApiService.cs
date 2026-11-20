using System.Security.Claims;
using MEMS.Data.Entities;
using MEMS.Data.Identity;
using MEMS.Data.Storage;
using MEMS.Shared.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MEMS.Data.Services;

public sealed class ElectionApiService(ElectionAccess access, IDbContextFactory<MemsDbContext> factory, ILookupNormalizer normalizer, MediaStore media)
{
    public async Task<DashboardDto> DashboardAsync(ClaimsPrincipal principal)
    {
        var actor = await access.RequireAdminAsync(principal);
        await using var db = actor.IsSuperAdmin
            ? await access.SystemDbAsync(principal)
            : await access.TenantDbAsync(actor.MunicipalityId!.Value);
        return new(await db.Municipalities.CountAsync(), await db.Positions.CountAsync(), await db.Candidates.CountAsync());
    }
    private static MunicipalityDto Dto(Municipality x) => new(x.MunicipalityId,x.Slug,x.Name,x.Province,x.ElectionDate,x.Description,x.BrandColor,x.PublicTitle,
        x.LogoFileName is null ? null : $"/api/{x.Slug}/municipalities/{x.MunicipalityId}/logo",Convert.ToBase64String(x.RowVersion));
    private static PositionDto Dto(Position x) => new(x.PositionId,x.MunicipalityId,x.Name,x.SeatsAvailable,Convert.ToBase64String(x.RowVersion));
    private static CandidateDto Dto(Candidate x,string slug) => new(x.CandidateId,x.MunicipalityId,x.PositionId,x.Name,x.Profile,x.Email,x.Website,x.DateCreated,
        x.PhotoFileName is null ? null : $"/api/{slug}/candidates/{x.CandidateId}/photo",x.DemoSeedKey is not null,Convert.ToBase64String(x.RowVersion));

    public async Task<List<MunicipalityDirectoryItem>> DirectoryAsync(ClaimsPrincipal principal)
    {
        var actor = await access.ActorAsync(principal);
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Municipalities.IgnoreQueryFilters().AsNoTracking();
        if (actor.Authenticated && !actor.IsSuperAdmin) query = query.Where(x => x.MunicipalityId == actor.MunicipalityId);
        return await query.OrderBy(x => x.Name).Select(x => new MunicipalityDirectoryItem(x.MunicipalityId,x.Slug,x.Name,x.BrandColor,
            x.LogoFileName == null ? null : "/api/" + x.Slug + "/municipalities/" + x.MunicipalityId + "/logo")).ToListAsync();
    }

    public async Task<List<MunicipalityDto>> MunicipalitiesAsync(ClaimsPrincipal principal, string? slug)
    {
        if (slug is null)
        {
            await using var system = await access.SystemDbAsync(principal);
            return (await system.Municipalities.AsNoTracking().OrderBy(x => x.Name).ToListAsync()).Select(Dto).ToList();
        }
        var tenant = await access.TenantAsync(principal,slug);
        await using var db = await access.TenantDbAsync(tenant.MunicipalityId);
        return (await db.Municipalities.AsNoTracking().ToListAsync()).Select(Dto).ToList();
    }

    public async Task<MunicipalityDto> MunicipalityAsync(ClaimsPrincipal principal, string? slug, int id)
    {
        if (slug is null)
        {
            await using var system = await access.SystemDbAsync(principal);
            return Dto(await system.Municipalities.AsNoTracking().SingleOrDefaultAsync(x => x.MunicipalityId == id) ?? throw new ApiFault(404,"Municipality not found."));
        }
        var tenant = await access.TenantAsync(principal,slug);
        await using var db = await access.TenantDbAsync(tenant.MunicipalityId);
        return Dto(await db.Municipalities.AsNoTracking().SingleOrDefaultAsync(x => x.MunicipalityId == id) ?? throw new ApiFault(404,"Municipality not found."));
    }

    public async Task<MunicipalityDto> CreateMunicipalityAsync(ClaimsPrincipal principal, MunicipalityRequest request)
    {
        await access.RequireAdminAsync(principal,true);
        ContractValidation.Check(request);
        await using var strategyDb = await factory.CreateDbContextAsync();
        return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await access.SystemDbAsync(principal);
            await using var transaction = await db.Database.BeginTransactionAsync();
            var item = new Municipality(); Apply(item,request);
            db.Municipalities.Add(item);
            await db.SaveChangesAsync();
            var name = item.Name + " Admin";
            db.Roles.Add(new ApplicationRole { Name = name, NormalizedName = normalizer.NormalizeName(name), Scope = AdministratorScope.Municipality, MunicipalityId = item.MunicipalityId });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Dto(item);
        });
    }

    public async Task<MunicipalityDto> UpdateMunicipalityAsync(ClaimsPrincipal principal,int id,MunicipalityRequest request,string? version)
    {
        await using var db = await access.SystemDbAsync(principal);
        ContractValidation.Check(request);
        var item = await db.Municipalities.SingleOrDefaultAsync(x => x.MunicipalityId == id) ?? throw new ApiFault(404,"Municipality not found.");
        ContractValidation.Version(item.RowVersion,version);
        Apply(item,request); await db.SaveChangesAsync(); return Dto(item);
    }

    private static void Apply(Municipality item,MunicipalityRequest request)
    { item.Name=request.Name.Trim();item.Slug=request.Slug;item.Province=request.Province.Trim();item.ElectionDate=request.ElectionDate;item.Description=request.Description.Trim();item.BrandColor=request.BrandColor;item.PublicTitle=request.PublicTitle.Trim(); }

    public async Task DeleteMunicipalityAsync(ClaimsPrincipal principal,int id,string? version)
    {
        await using var db = await access.SystemDbAsync(principal);
        var item = await db.Municipalities.SingleOrDefaultAsync(x => x.MunicipalityId == id) ?? throw new ApiFault(404,"Municipality not found.");
        ContractValidation.Version(item.RowVersion,version);
        var roles = await db.Roles.Where(x => x.MunicipalityId == id).ToListAsync();
        var roleIds = roles.Select(x => x.Id).ToList();
        if (await db.Positions.AnyAsync(x => x.MunicipalityId == id) || await db.Candidates.AnyAsync(x => x.MunicipalityId == id)
            || await db.Users.AnyAsync(x => x.MunicipalityId == id) || await db.UserRoles.AnyAsync(x => roleIds.Contains(x.RoleId)))
            throw new ApiFault(409,"Remove dependent election records and administrator assignments first.");
        db.Roles.RemoveRange(roles);db.Municipalities.Remove(item);await db.SaveChangesAsync();media.DeleteUpload(id,item.LogoFileName);
    }

    public async Task<List<PositionDto>> PositionsAsync(ClaimsPrincipal principal,string slug)
    {
        var tenant=await access.TenantAsync(principal,slug);
        await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        return (await db.Positions.AsNoTracking().OrderBy(x=>x.Name).ThenBy(x=>x.PositionId).ToListAsync()).Select(Dto).ToList();
    }
    public async Task<PositionDto> PositionAsync(ClaimsPrincipal principal,string slug,int id)
    {
        var tenant=await access.TenantAsync(principal,slug);await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        return Dto(await db.Positions.AsNoTracking().SingleOrDefaultAsync(x=>x.PositionId==id) ?? throw new ApiFault(404,"Position not found."));
    }
    public async Task<PositionDto> SavePositionAsync(ClaimsPrincipal principal,string slug,int? id,PositionRequest request,string? version)
    {
        var tenant=await access.TenantAsync(principal,slug,true);ContractValidation.Check(request);
        await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        var item=id.HasValue ? await db.Positions.SingleOrDefaultAsync(x=>x.PositionId==id) ?? throw new ApiFault(404,"Position not found.") : new Position { MunicipalityId=tenant.MunicipalityId };
        if(id.HasValue) ContractValidation.Version(item.RowVersion,version);else db.Positions.Add(item);
        item.Name=request.Name.Trim();item.SeatsAvailable=request.SeatsAvailable;await db.SaveChangesAsync();return Dto(item);
    }
    public async Task DeletePositionAsync(ClaimsPrincipal principal,string slug,int id,string? version)
    {
        var tenant=await access.TenantAsync(principal,slug,true);await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        var item=await db.Positions.SingleOrDefaultAsync(x=>x.PositionId==id) ?? throw new ApiFault(404,"Position not found.");
        ContractValidation.Version(item.RowVersion,version);
        if(await db.Candidates.AnyAsync(x=>x.PositionId==id))throw new ApiFault(409,"Remove or reassign candidates before deleting this position.");
        db.Positions.Remove(item);await db.SaveChangesAsync();
    }

    public async Task<PageResult<CandidateDto>> CandidatesAsync(ClaimsPrincipal principal,string slug,CandidateQuery request)
    {
        var tenant=await access.TenantAsync(principal,slug);ContractValidation.Check(request);
        await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        if(request.PositionId.HasValue && !await db.Positions.AnyAsync(x=>x.PositionId==request.PositionId))throw new ApiFault(400,"Choose a position in this municipality.");
        var query=db.Candidates.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(request.Search))query=query.Where(x=>x.Name.Contains(request.Search.Trim()));
        if(request.PositionId.HasValue)query=query.Where(x=>x.PositionId==request.PositionId);
        var count=await query.CountAsync();
        var sorted=request.Sort=="created" ? query.OrderByDescending(x=>x.DateCreated).ThenBy(x=>x.CandidateId) : query.OrderBy(x=>x.Name).ThenBy(x=>x.CandidateId);
        var items=await sorted.Skip((request.Page-1)*request.PageSize).Take(request.PageSize).ToListAsync();
        return new(items.Select(x=>Dto(x,slug)).ToList(),request.Page,request.PageSize,count,(count+request.PageSize-1)/request.PageSize);
    }
    public async Task<CandidateDto> CandidateAsync(ClaimsPrincipal principal,string slug,int id)
    {
        var tenant=await access.TenantAsync(principal,slug);await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        return Dto(await db.Candidates.AsNoTracking().SingleOrDefaultAsync(x=>x.CandidateId==id) ?? throw new ApiFault(404,"Candidate not found."),slug);
    }
    public async Task<CandidateDto> SaveCandidateAsync(ClaimsPrincipal principal,string slug,int? id,CandidateRequest request,string? version)
    {
        var tenant=await access.TenantAsync(principal,slug,true);ContractValidation.Check(request);
        await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        // Check ownership of the existing record before accepting a submitted parent.
        var item=id.HasValue ? await db.Candidates.SingleOrDefaultAsync(x=>x.CandidateId==id) ?? throw new ApiFault(404,"Candidate not found.") : new Candidate { MunicipalityId=tenant.MunicipalityId };
        if(id.HasValue)ContractValidation.Version(item.RowVersion,version);
        if(!await db.Positions.AnyAsync(x=>x.PositionId==request.PositionId))throw new ApiFault(400,"Choose a position in this municipality.");
        if(!id.HasValue)db.Candidates.Add(item);
        item.Name=request.Name.Trim();item.Profile=request.Profile.Trim();item.PositionId=request.PositionId;
        item.Email=string.IsNullOrWhiteSpace(request.Email)?null:request.Email.Trim();item.Website=string.IsNullOrWhiteSpace(request.Website)?null:request.Website.Trim();
        await db.SaveChangesAsync();return Dto(item,slug);
    }
    public async Task DeleteCandidateAsync(ClaimsPrincipal principal,string slug,int id,string? version)
    {
        var tenant=await access.TenantAsync(principal,slug,true);await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        var item=await db.Candidates.SingleOrDefaultAsync(x=>x.CandidateId==id) ?? throw new ApiFault(404,"Candidate not found.");
        ContractValidation.Version(item.RowVersion,version);db.Candidates.Remove(item);await db.SaveChangesAsync();media.DeleteUpload(tenant.MunicipalityId,item.PhotoFileName);
    }

    public async Task<StoredMedia> ReadMediaAsync(ClaimsPrincipal principal,string slug,int id,bool logo)
    {
        var tenant=await access.TenantAsync(principal,slug);await using var db=await access.TenantDbAsync(tenant.MunicipalityId);
        string? name;
        if(logo)name=(await db.Municipalities.SingleOrDefaultAsync(x=>x.MunicipalityId==id) ?? throw new ApiFault(404,"Municipality not found.")).LogoFileName;
        else name=(await db.Candidates.SingleOrDefaultAsync(x=>x.CandidateId==id) ?? throw new ApiFault(404,"Candidate not found.")).PhotoFileName;
        return media.Read(tenant.MunicipalityId,name,logo);
    }
    public async Task<object> ChangeMediaAsync(ClaimsPrincipal principal,string slug,int id,bool logo,IFormFile? upload,string? version,CancellationToken cancellationToken)
    {
        var tenant=await access.TenantAsync(principal,slug,true);
        if(logo)await access.RequireAdminAsync(principal,true);
        await using var db=logo ? await access.SystemDbAsync(principal) : await access.TenantDbAsync(tenant.MunicipalityId);
        Municipality? municipality=null;Candidate? candidate=null;string? old;
        if(logo)
        {
            municipality=await db.Municipalities.SingleOrDefaultAsync(x=>x.MunicipalityId==id && x.MunicipalityId==tenant.MunicipalityId) ?? throw new ApiFault(404,"Municipality not found.");
            ContractValidation.Version(municipality.RowVersion,version);old=municipality.LogoFileName;
        }
        else
        {
            candidate=await db.Candidates.SingleOrDefaultAsync(x=>x.CandidateId==id) ?? throw new ApiFault(404,"Candidate not found.");
            ContractValidation.Version(candidate.RowVersion,version);old=candidate.PhotoFileName;
        }
        // Only authorized, existing, current-version records can create a file.
        var name=upload is null ? null : await media.SaveAsync(tenant.MunicipalityId,upload,cancellationToken);
        if(logo)municipality!.LogoFileName=name;else candidate!.PhotoFileName=name;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { media.DeleteUpload(tenant.MunicipalityId,name);throw; }
        if(old!=name)media.DeleteUpload(tenant.MunicipalityId,old);
        return logo ? Dto(municipality!) : Dto(candidate!,slug);
    }
}
