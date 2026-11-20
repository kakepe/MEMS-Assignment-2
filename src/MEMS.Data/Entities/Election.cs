using System.ComponentModel.DataAnnotations;

namespace MEMS.Data.Entities;

public sealed class Municipality
{
    public int MunicipalityId { get; set; }
    private string slug = "";
    [Required, StringLength(80), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get => slug; set => slug = (value ?? "").Trim().ToLowerInvariant(); }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(50)] public string Province { get; set; } = "British Columbia";
    public DateOnly ElectionDate { get; set; } = new(2026, 10, 17);
    [Required, StringLength(2000)] public string Description { get; set; } = "";
    [StringLength(160), RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9._-]*$")] public string? LogoFileName { get; set; }
    [Required, RegularExpression("^#[0-9a-fA-F]{6}$")] public string BrandColor { get; set; } = "#08695f";
    [Required, StringLength(120)] public string PublicTitle { get; set; } = "Municipal election guide";
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public List<Position> Positions { get; set; } = [];
}

public sealed class Position
{
    public int PositionId { get; set; }
    public int MunicipalityId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(1, 100)] public int SeatsAvailable { get; set; } = 1;
    public Municipality Municipality { get; set; } = null!;
    public List<Candidate> Candidates { get; set; } = [];
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public sealed class Candidate
{
    public int CandidateId { get; set; }
    public int MunicipalityId { get; set; }
    public int PositionId { get; set; }
    [StringLength(80)] public string? DemoSeedKey { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(4000)] public string Profile { get; set; } = "";
    [StringLength(160), RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9._-]*$")] public string? PhotoFileName { get; set; }
    [StringLength(300), Url] public string? Website { get; set; }
    [StringLength(254), EmailAddress] public string? Email { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public Municipality Municipality { get; set; } = null!;
    public Position Position { get; set; } = null!;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

// A completed seed version is never rerun, even after an administrator deletes a sample.
public sealed class SeedHistory
{
    [Key, StringLength(80)] public string SeedKey { get; set; } = "";
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}
