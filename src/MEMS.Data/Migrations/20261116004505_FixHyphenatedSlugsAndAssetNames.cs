using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixHyphenatedSlugsAndAssetNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Municipalities_Logo",
                table: "Municipalities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Municipalities_Slug",
                table: "Municipalities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Candidates_Photo",
                table: "Candidates");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Municipalities_Logo",
                table: "Municipalities",
                sql: "[LogoFileName] IS NULL OR (LEN([LogoFileName]) > 0 AND [LogoFileName] NOT LIKE '%[^-a-zA-Z0-9._]%' COLLATE Latin1_General_100_BIN2 AND [LogoFileName] NOT LIKE '%..%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Municipalities_Slug",
                table: "Municipalities",
                sql: "LEN([Slug]) BETWEEN 1 AND 80 AND [Slug] NOT LIKE '%[^-a-z0-9]%' COLLATE Latin1_General_100_BIN2 AND LEFT([Slug],1) <> '-' AND RIGHT([Slug],1) <> '-' AND [Slug] NOT LIKE '%--%' AND [Slug] NOT IN ('admin','api','health','alive','mems-public')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Candidates_Photo",
                table: "Candidates",
                sql: "[PhotoFileName] IS NULL OR (LEN([PhotoFileName]) > 0 AND [PhotoFileName] NOT LIKE '%[^-a-zA-Z0-9._]%' COLLATE Latin1_General_100_BIN2 AND [PhotoFileName] NOT LIKE '%..%')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Municipalities_Logo",
                table: "Municipalities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Municipalities_Slug",
                table: "Municipalities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Candidates_Photo",
                table: "Candidates");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Municipalities_Logo",
                table: "Municipalities",
                sql: "[LogoFileName] IS NULL OR (LEN([LogoFileName]) > 0 AND [LogoFileName] NOT LIKE '%[^a-zA-Z0-9._-]%' COLLATE Latin1_General_100_BIN2 AND [LogoFileName] NOT LIKE '%..%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Municipalities_Slug",
                table: "Municipalities",
                sql: "LEN([Slug]) BETWEEN 1 AND 80 AND [Slug] NOT LIKE '%[^a-z0-9-]%' COLLATE Latin1_General_100_BIN2 AND LEFT([Slug],1) <> '-' AND RIGHT([Slug],1) <> '-' AND [Slug] NOT LIKE '%--%' AND [Slug] NOT IN ('admin','api','health','alive','mems-public')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Candidates_Photo",
                table: "Candidates",
                sql: "[PhotoFileName] IS NULL OR (LEN([PhotoFileName]) > 0 AND [PhotoFileName] NOT LIKE '%[^a-zA-Z0-9._-]%' COLLATE Latin1_General_100_BIN2 AND [PhotoFileName] NOT LIKE '%..%')");
        }
    }
}
