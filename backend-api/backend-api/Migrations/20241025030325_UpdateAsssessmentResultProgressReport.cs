using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateAsssessmentResultProgressReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "AssessmentResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_QuestionId",
                table: "AssessmentResults",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_AssessmentQuestions_QuestionId",
                table: "AssessmentResults",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_AssessmentQuestions_QuestionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_QuestionId",
                table: "AssessmentResults");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "AssessmentResults");
        }
    }
}
