using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    /// <inheritdoc />
    public partial class InitDB_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Tutors_SubmiterId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Tutors_SubmiterId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Tutors_SubmiterId",
                table: "WorkExperiences");

            migrationBuilder.RenameColumn(
                name: "SubmiterId",
                table: "WorkExperiences",
                newName: "SubmitterId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_SubmiterId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_SubmitterId");

            migrationBuilder.RenameColumn(
                name: "SubmiterId",
                table: "Curriculums",
                newName: "SubmitterId");

            migrationBuilder.RenameIndex(
                name: "IX_Curriculums_SubmiterId",
                table: "Curriculums",
                newName: "IX_Curriculums_SubmitterId");

            migrationBuilder.RenameColumn(
                name: "SubmiterId",
                table: "Certificates",
                newName: "SubmitterId");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_SubmiterId",
                table: "Certificates",
                newName: "IX_Certificates_SubmitterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Tutors_SubmitterId",
                table: "Certificates",
                column: "SubmitterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Tutors_SubmitterId",
                table: "Curriculums",
                column: "SubmitterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Tutors_SubmitterId",
                table: "WorkExperiences",
                column: "SubmitterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Tutors_SubmitterId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Tutors_SubmitterId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Tutors_SubmitterId",
                table: "WorkExperiences");

            migrationBuilder.RenameColumn(
                name: "SubmitterId",
                table: "WorkExperiences",
                newName: "SubmiterId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_SubmitterId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_SubmiterId");

            migrationBuilder.RenameColumn(
                name: "SubmitterId",
                table: "Curriculums",
                newName: "SubmiterId");

            migrationBuilder.RenameIndex(
                name: "IX_Curriculums_SubmitterId",
                table: "Curriculums",
                newName: "IX_Curriculums_SubmiterId");

            migrationBuilder.RenameColumn(
                name: "SubmitterId",
                table: "Certificates",
                newName: "SubmiterId");

            migrationBuilder.RenameIndex(
                name: "IX_Certificates_SubmitterId",
                table: "Certificates",
                newName: "IX_Certificates_SubmiterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Tutors_SubmiterId",
                table: "Certificates",
                column: "SubmiterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Tutors_SubmiterId",
                table: "Curriculums",
                column: "SubmiterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Tutors_SubmiterId",
                table: "WorkExperiences",
                column: "SubmiterId",
                principalTable: "Tutors",
                principalColumn: "TutorId");
        }
    }
}
