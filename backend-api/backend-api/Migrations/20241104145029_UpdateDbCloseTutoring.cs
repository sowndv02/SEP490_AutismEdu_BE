using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbCloseTutoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentOptions_OptionId",
                table: "InitialAssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentQuestions_QuestionId",
                table: "InitialAssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_InitialAssessmentResults_StudentProfiles_StudentProfileId",
                table: "InitialAssessmentResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InitialAssessmentResults",
                table: "InitialAssessmentResults");

            migrationBuilder.RenameTable(
                name: "InitialAssessmentResults",
                newName: "InitialAndFinalAssessmentResult");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAssessmentResults_StudentProfileId",
                table: "InitialAndFinalAssessmentResult",
                newName: "IX_InitialAndFinalAssessmentResult_StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAssessmentResults_QuestionId",
                table: "InitialAndFinalAssessmentResult",
                newName: "IX_InitialAndFinalAssessmentResult_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAssessmentResults_OptionId",
                table: "InitialAndFinalAssessmentResult",
                newName: "IX_InitialAndFinalAssessmentResult_OptionId");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Users",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FinalCondition",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isInitialAssessment",
                table: "InitialAndFinalAssessmentResult",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InitialAndFinalAssessmentResult",
                table: "InitialAndFinalAssessmentResult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_AssessmentOptions_OptionId",
                table: "InitialAndFinalAssessmentResult",
                column: "OptionId",
                principalTable: "AssessmentOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_AssessmentQuestions_QuestionId",
                table: "InitialAndFinalAssessmentResult",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_StudentProfiles_StudentProfileId",
                table: "InitialAndFinalAssessmentResult",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_AssessmentOptions_OptionId",
                table: "InitialAndFinalAssessmentResult");

            migrationBuilder.DropForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_AssessmentQuestions_QuestionId",
                table: "InitialAndFinalAssessmentResult");

            migrationBuilder.DropForeignKey(
                name: "FK_InitialAndFinalAssessmentResult_StudentProfiles_StudentProfileId",
                table: "InitialAndFinalAssessmentResult");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InitialAndFinalAssessmentResult",
                table: "InitialAndFinalAssessmentResult");

            migrationBuilder.DropColumn(
                name: "FinalCondition",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "isInitialAssessment",
                table: "InitialAndFinalAssessmentResult");

            migrationBuilder.RenameTable(
                name: "InitialAndFinalAssessmentResult",
                newName: "InitialAssessmentResults");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAndFinalAssessmentResult_StudentProfileId",
                table: "InitialAssessmentResults",
                newName: "IX_InitialAssessmentResults_StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAndFinalAssessmentResult_QuestionId",
                table: "InitialAssessmentResults",
                newName: "IX_InitialAssessmentResults_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_InitialAndFinalAssessmentResult_OptionId",
                table: "InitialAssessmentResults",
                newName: "IX_InitialAssessmentResults_OptionId");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InitialAssessmentResults",
                table: "InitialAssessmentResults",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentOptions_OptionId",
                table: "InitialAssessmentResults",
                column: "OptionId",
                principalTable: "AssessmentOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAssessmentResults_AssessmentQuestions_QuestionId",
                table: "InitialAssessmentResults",
                column: "QuestionId",
                principalTable: "AssessmentQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InitialAssessmentResults_StudentProfiles_StudentProfileId",
                table: "InitialAssessmentResults",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
