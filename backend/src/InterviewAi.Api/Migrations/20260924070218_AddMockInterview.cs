using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewAi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMockInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "InterviewReports",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InterviewDate",
                table: "InterviewReports",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "InterviewReports",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Planned");

            migrationBuilder.CreateTable(
                name: "MockAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuestionOrder = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    StarAssessment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Improvements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingKeywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockAnswers", x => x.Id);
                    table.CheckConstraint("CK_MockAnswers_QuestionType", "[QuestionType] IN ('Technical', 'Behavioral')");
                    table.CheckConstraint("CK_MockAnswers_Score", "[Score] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_MockAnswers_InterviewReports_InterviewReportId",
                        column: x => x.InterviewReportId,
                        principalTable: "InterviewReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewReports_OwnerId_InterviewDate",
                table: "InterviewReports",
                columns: new[] { "OwnerId", "InterviewDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewReports_Status",
                table: "InterviewReports",
                sql: "[Status] IN ('Planned', 'Applied', 'Interviewing', 'Offer', 'Rejected')");

            migrationBuilder.CreateIndex(
                name: "IX_MockAnswers_InterviewReportId_CreatedAt",
                table: "MockAnswers",
                columns: new[] { "InterviewReportId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockAnswers");

            migrationBuilder.DropIndex(
                name: "IX_InterviewReports_OwnerId_InterviewDate",
                table: "InterviewReports");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewReports_Status",
                table: "InterviewReports");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "InterviewReports");

            migrationBuilder.DropColumn(
                name: "InterviewDate",
                table: "InterviewReports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InterviewReports");
        }
    }
}
