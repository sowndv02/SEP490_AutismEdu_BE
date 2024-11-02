using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateAssessmentQuestion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmitterId",
                table: "AssessmentQuestions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_SubmitterId",
                table: "AssessmentQuestions",
                column: "SubmitterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentQuestions_Users_SubmitterId",
                table: "AssessmentQuestions",
                column: "SubmitterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentQuestions_Users_SubmitterId",
                table: "AssessmentQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentQuestions_SubmitterId",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "AssessmentQuestions");
        }
    }
}
