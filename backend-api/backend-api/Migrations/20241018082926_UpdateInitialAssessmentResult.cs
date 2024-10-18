using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateInitialAssessmentResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "InitialAssessmentResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InitialAssessmentResults_QuestionId",
                table: "InitialAssessmentResults",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentQuestions_QuestionId",
                table: "InitialAssessmentResults",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentQuestions_QuestionId",
                table: "InitialAssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_InitialAssessmentResults_QuestionId",
                table: "InitialAssessmentResults");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "InitialAssessmentResults");
        }
    }
}
