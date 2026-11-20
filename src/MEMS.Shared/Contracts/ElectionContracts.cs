using System.ComponentModel.DataAnnotations;

namespace MEMS.Shared.Contracts;

public sealed class MunicipalityRequest : IValidatableObject
{
    private string slug = "";
    [Required, StringLength(80), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get => slug; set => slug = (value ?? "").Trim().ToLowerInvariant(); }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(50)] public string Province { get; set; } = "British Columbia";
    public DateOnly ElectionDate { get; set; }
    [Required, StringLength(2000)] public string Description { get; set; } = "";
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string BrandColor { get; set; } = "#08695f";
    [Required, StringLength(120)] public string PublicTitle { get; set; } = "Municipal election guide";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ElectionDate.Year is < 2000 or >= 2200) yield return new("Election date must be between 2000 and 2199.", [nameof(ElectionDate)]);
        if (new[] { "admin", "api", "health", "alive", "mems-public" }.Contains(Slug)) yield return new("This slug is reserved.", [nameof(Slug)]);
    }
}

public sealed class PositionRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(1,100, ErrorMessage = "Available seats must be between 1 and 100.")] public int SeatsAvailable { get; set; } = 1;
}

public sealed class CandidateRequest
{
    [Range(1,int.MaxValue, ErrorMessage = "Choose a position in this municipality.")] public int PositionId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(4000)] public string Profile { get; set; } = "";
    [StringLength(254), EmailAddress] public string? Email { get; set; }
    [StringLength(300), HttpWebsite] public string? Website { get; set; }
}

public sealed class HttpWebsiteAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || value is string text &&
        (string.IsNullOrWhiteSpace(text) || Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" && !string.IsNullOrWhiteSpace(uri.Host));
    public HttpWebsiteAttribute() => ErrorMessage = "Use a complete HTTP or HTTPS website address.";
}

public sealed class CandidateQuery
{
    [StringLength(100)] public string? Search { get; set; }
    [Range(1,int.MaxValue)] public int? PositionId { get; set; }
    [Range(1,1000000)] public int Page { get; set; } = 1;
    [Range(1,100)] public int PageSize { get; set; } = 12;
    [RegularExpression("^(name|created)$")] public string Sort { get; set; } = "name";
}

public sealed record MunicipalityDirectoryItem(int MunicipalityId, string Slug, string Name, string BrandColor, string? LogoUrl);
public sealed record MunicipalityDto(int MunicipalityId, string Slug, string Name, string Province, DateOnly ElectionDate,
    string Description, string BrandColor, string PublicTitle, string? LogoUrl, string Version);
public sealed record PositionDto(int PositionId, int MunicipalityId, string Name, int SeatsAvailable, string Version);
public sealed record CandidateDto(int CandidateId, int MunicipalityId, int PositionId, string Name, string Profile,
    string? Email, string? Website, DateTime DateCreated, string? PhotoUrl, bool IsFictionalSample, string Version);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed class LoginRequest
{
    [Required, StringLength(256)] public string UserName { get; set; } = "";
    [Required, StringLength(1024)] public string Password { get; set; } = "";
}
public sealed record SessionDto(string UserName, bool IsSuperAdmin, int? MunicipalityId);
public sealed record CsrfDto(string RequestToken, string HeaderName);
public sealed record DashboardDto(int Municipalities, int Positions, int Candidates);
