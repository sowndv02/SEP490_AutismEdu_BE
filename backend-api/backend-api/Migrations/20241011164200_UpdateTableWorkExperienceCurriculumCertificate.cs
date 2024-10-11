using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateTableWorkExperienceCurriculumCertificate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Curriculums_OriginalCurriculumId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Curriculums_OriginalCurriculumId",
                table: "WorkExperiences");

            migrationBuilder.RenameColumn(
                name: "OriginalCurriculumId",
                table: "WorkExperiences",
                newName: "OriginalId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_OriginalCurriculumId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_OriginalId");

            migrationBuilder.RenameColumn(
                name: "OriginalCurriculumId",
                table: "Certificates",
                newName: "OriginalId");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_OriginalCurriculumId",
                table: "Certificates",
                newName: "IX_Certificates_OriginalId");

            migrationBuilder.AddColumn<string>(
                name: "TutorUserId",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TutorUserId",
                table: "Certificates",
                column: "TutorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_OriginalId",
                table: "Certificates",
                column: "OriginalId",
                principalTable: "Certificates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Tutors_TutorUserId",
                table: "Certificates",
                column: "TutorUserId",
                principalTable: "Tutors",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_WorkExperiences_OriginalId",
                table: "WorkExperiences",
                column: "OriginalId",
                principalTable: "WorkExperiences",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_OriginalId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Tutors_TutorUserId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_WorkExperiences_OriginalId",
                table: "WorkExperiences");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_TutorUserId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "TutorUserId",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "OriginalId",
                table: "WorkExperiences",
                newName: "OriginalCurriculumId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_OriginalId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_OriginalCurriculumId");

            migrationBuilder.RenameColumn(
                name: "OriginalId",
                table: "Certificates",
                newName: "OriginalCurriculumId");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_OriginalId",
                table: "Certificates",
                newName: "IX_Certificates_OriginalCurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Curriculums_OriginalCurriculumId",
                table: "Certificates",
                column: "OriginalCurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Curriculums_OriginalCurriculumId",
                table: "WorkExperiences",
                column: "OriginalCurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id");
        }
    }
}
