using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewAi.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResumeText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelfDescription = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    MatchScore = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewReports", x => x.Id);
                    table.CheckConstraint("CK_InterviewReports_MatchScore", "[MatchScore] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_InterviewReports_ResumeOrSelfDescription", "[ResumeText] IS NOT NULL OR [SelfDescription] IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Intention = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SuggestedAnswer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                    table.CheckConstraint("CK_InterviewQuestions_Type", "[Type] IN ('Technical', 'Behavioral')");
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_InterviewReports_InterviewReportId",
                        column: x => x.InterviewReportId,
                        principalTable: "InterviewReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreparationDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Focus = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Tasks = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationDays", x => x.Id);
                    table.CheckConstraint("CK_PreparationDays_DayNumber", "[DayNumber] >= 1");
                    table.ForeignKey(
                        name: "FK_PreparationDays_InterviewReports_InterviewReportId",
                        column: x => x.InterviewReportId,
                        principalTable: "InterviewReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillGaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillGaps", x => x.Id);
                    table.CheckConstraint("CK_SkillGaps_Severity", "[Severity] IN ('Low', 'Medium', 'High')");
                    table.ForeignKey(
                        name: "FK_SkillGaps_InterviewReports_InterviewReportId",
                        column: x => x.InterviewReportId,
                        principalTable: "InterviewReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewReportId",
                table: "InterviewQuestions",
                column: "InterviewReportId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewReports_OwnerId_CreatedAt",
                table: "InterviewReports",
                columns: new[] { "OwnerId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PreparationDays_InterviewReportId_DayNumber",
                table: "PreparationDays",
                columns: new[] { "InterviewReportId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillGaps_InterviewReportId",
                table: "SkillGaps",
                column: "InterviewReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewQuestions");

            migrationBuilder.DropTable(
                name: "PreparationDays");

            migrationBuilder.DropTable(
                name: "SkillGaps");

            migrationBuilder.DropTable(
                name: "InterviewReports");
        }
    }
}
