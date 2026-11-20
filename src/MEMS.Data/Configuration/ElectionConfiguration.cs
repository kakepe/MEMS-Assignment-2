using MEMS.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MEMS.Data.Configuration;

internal sealed class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> entity)
    {
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.Property(x => x.Slug).UseCollation("Latin1_General_100_BIN2");
        entity.HasIndex(x => x.Name).IsUnique();
        entity.Property(x => x.BrandColor).HasMaxLength(7);
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Municipalities_Slug", "LEN([Slug]) BETWEEN 1 AND 80 AND [Slug] NOT LIKE '%[^-a-z0-9]%' COLLATE Latin1_General_100_BIN2 AND LEFT([Slug],1) <> '-' AND RIGHT([Slug],1) <> '-' AND [Slug] NOT LIKE '%--%' AND [Slug] NOT IN ('admin','api','health','alive','mems-public')");
            t.HasCheckConstraint("CK_Municipalities_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Municipalities_Description", "LEN(LTRIM(RTRIM([Description]))) > 0");
            t.HasCheckConstraint("CK_Municipalities_Brand", "LEN([BrandColor]) = 7 AND LEFT([BrandColor],1) = '#' AND SUBSTRING([BrandColor],2,6) NOT LIKE '%[^0-9a-fA-F]%' COLLATE Latin1_General_100_BIN2");
            t.HasCheckConstraint("CK_Municipalities_Date", "[ElectionDate] >= '20000101' AND [ElectionDate] < '22000101'");
            t.HasCheckConstraint("CK_Municipalities_Logo", FileConstraint("LogoFileName"));
        });
    }

    internal static string FileConstraint(string column) => $"[{column}] IS NULL OR (LEN([{column}]) > 0 AND [{column}] NOT LIKE '%[^-a-zA-Z0-9._]%' COLLATE Latin1_General_100_BIN2 AND [{column}] NOT LIKE '%..%')";
}

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> entity)
    {
        entity.HasAlternateKey(x => new { x.MunicipalityId, x.PositionId });
        entity.Property(x => x.MunicipalityId).IsConcurrencyToken();
        entity.HasIndex(x => new { x.MunicipalityId, x.Name }).IsUnique();
        entity.HasOne(x => x.Municipality).WithMany(x => x.Positions).HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Positions_Seats", "[SeatsAvailable] BETWEEN 1 AND 100");
            t.HasCheckConstraint("CK_Positions_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
    }
}

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> entity)
    {
        entity.Property(x => x.MunicipalityId).IsConcurrencyToken();
        entity.HasOne(x => x.Position).WithMany(x => x.Candidates)
            .HasForeignKey(x => new { x.MunicipalityId, x.PositionId })
            .HasPrincipalKey(x => new { x.MunicipalityId, x.PositionId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.DemoSeedKey).IsUnique().HasFilter("[DemoSeedKey] IS NOT NULL");
        entity.HasIndex(x => new { x.MunicipalityId, x.Name });
        entity.Property(x => x.DateCreated).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Candidates_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Candidates_Profile", "LEN(LTRIM(RTRIM([Profile]))) > 0");
            t.HasCheckConstraint("CK_Candidates_Photo", MunicipalityConfiguration.FileConstraint("PhotoFileName"));
            t.HasCheckConstraint("CK_Candidates_Website", "[Website] IS NULL OR [Website] LIKE 'http://_%' OR [Website] LIKE 'https://_%'");
            t.HasCheckConstraint("CK_Candidates_Email", "[Email] IS NULL OR ([Email] LIKE '_%@_%._%' AND [Email] NOT LIKE '% %')");
        });
    }
}

