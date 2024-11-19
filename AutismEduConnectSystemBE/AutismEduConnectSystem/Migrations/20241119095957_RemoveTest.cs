using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutismEduConnectSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentOptions_AssessmentQuestions_TestId",
                table: "AssessmentOptions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentOptions_TestId",
                table: "AssessmentOptions");

            migrationBuilder.DropColumn(
                name: "TestId",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "TestId",
                table: "AssessmentOptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestId",
                table: "AssessmentQuestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestId",
                table: "AssessmentOptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentOptions_TestId",
                table: "AssessmentOptions",
                column: "TestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentOptions_AssessmentQuestions_TestId",
                table: "AssessmentOptions",
                column: "TestId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id");
        }
    }
}
