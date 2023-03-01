using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiteralCollector.Migrations; 
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Literals",
            columns: table => new
            {
                LiteralId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Value = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Literals", x => x.LiteralId);
                table.UniqueConstraint("AK_Literal_Value", x => x.Value);
            });

        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Url = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                BaseFolder = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                HostName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", x => x.ProjectId);
            });

        migrationBuilder.CreateTable(
            name: "Scans",
            columns: table => new
            {
                ScanId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Url = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                LiteralCount = table.Column<int>(type: "INTEGER", nullable: false),
                PreviousLiteralCount = table.Column<int>(type: "INTEGER", nullable: false),
                DurationSecs = table.Column<int>(type: "INTEGER", nullable: false),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Scans", x => x.ScanId);
                table.ForeignKey(
                    name: "FK_Scans_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "ProjectId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SourceFiles",
            columns: table => new
            {
                SourceFileId = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Path = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                ScanId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SourceFiles", x => x.SourceFileId);
                table.ForeignKey(
                    name: "FK_SourceFiles_Scans_ScanId",
                    column: x => x.ScanId,
                    principalTable: "Scans",
                    principalColumn: "ScanId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LiteralLocations",
            columns: table => new
            {
                LiteralId = table.Column<int>(type: "INTEGER", nullable: false),
                SourceFileId = table.Column<int>(type: "INTEGER", nullable: false),
                LineStart = table.Column<int>(type: "INTEGER", nullable: false, defaultValueSql: "0"),
                ColumnStart = table.Column<int>(type: "INTEGER", nullable: false, defaultValueSql: "0"),
                LineEnd = table.Column<int>(type: "INTEGER", nullable: false, defaultValueSql: "0"),
                ColumnEnd = table.Column<int>(type: "INTEGER", nullable: false, defaultValueSql: "0")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LiteralLocations", x => new { x.SourceFileId, x.LiteralId, x.LineStart, x.ColumnStart });
                table.ForeignKey(
                    name: "FK_LiteralLocations_Literals_LiteralId",
                    column: x => x.LiteralId,
                    principalTable: "Literals",
                    principalColumn: "LiteralId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LiteralLocations_SourceFiles_SourceFileId",
                    column: x => x.SourceFileId,
                    principalTable: "SourceFiles",
                    principalColumn: "SourceFileId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LiteralLocations_LiteralId",
            table: "LiteralLocations",
            column: "LiteralId");

        migrationBuilder.CreateIndex(
            name: "IX_Scans_ProjectId",
            table: "Scans",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_SourceFiles_ScanId",
            table: "SourceFiles",
            column: "ScanId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LiteralLocations");

        migrationBuilder.DropTable(
            name: "Literals");

        migrationBuilder.DropTable(
            name: "SourceFiles");

        migrationBuilder.DropTable(
            name: "Scans");

        migrationBuilder.DropTable(
            name: "Projects");
    }
}
