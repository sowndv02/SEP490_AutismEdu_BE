using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddUpdateTableUserTutorAddCurrirulumWorkexperience_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Tutors_TutorInfoUserId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "IsApprove",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "PriceFrom",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IsApprove",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "TutorId",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "IsApprove",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "WorkExperiences",
                newName: "SubmiterId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_UserId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_SubmiterId");

            migrationBuilder.RenameColumn(
                name: "PriceTo",
                table: "Tutors",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Desc",
                table: "Reviews",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "TutorInfoUserId",
                table: "Curriculums",
                newName: "SubmiterId");

            migrationBuilder.RenameIndex(
                name: "IX_Curriculums_TutorInfoUserId",
                table: "Curriculums",
                newName: "IX_Curriculums_SubmiterId");

            migrationBuilder.RenameColumn(
                name: "Feedback",
                table: "Certificates",
                newName: "RejectionReason");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedId",
                table: "WorkExperiences",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "WorkExperiences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestStatus",
                table: "WorkExperiences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedId",
                table: "Curriculums",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Curriculums",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestStatus",
                table: "Curriculums",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedId",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestStatus",
                table: "Certificates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_ApprovedId",
                table: "WorkExperiences",
                column: "ApprovedId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_ApprovedId",
                table: "Curriculums",
                column: "ApprovedId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ApprovedId",
                table: "Certificates",
                column: "ApprovedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_ApprovedId",
                table: "Certificates",
                column: "ApprovedId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Tutors_SubmiterId",
                table: "Curriculums",
                column: "SubmiterId",
                principalTable: "Tutors",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Users_ApprovedId",
                table: "Curriculums",
                column: "ApprovedId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Users_ApprovedId",
                table: "WorkExperiences",
                column: "ApprovedId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Users_SubmiterId",
                table: "WorkExperiences",
                column: "SubmiterId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_ApprovedId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Tutors_SubmiterId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Users_ApprovedId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Users_ApprovedId",
                table: "WorkExperiences");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Users_SubmiterId",
                table: "WorkExperiences");

            migrationBuilder.DropIndex(
                name: "IX_WorkExperiences_ApprovedId",
                table: "WorkExperiences");

            migrationBuilder.DropIndex(
                name: "IX_Curriculums_ApprovedId",
                table: "Curriculums");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ApprovedId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ApprovedId",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "ApprovedId",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "ApprovedId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "SubmiterId",
                table: "WorkExperiences",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkExperiences_SubmiterId",
                table: "WorkExperiences",
                newName: "IX_WorkExperiences_UserId");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Tutors",
                newName: "PriceTo");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Reviews",
                newName: "Desc");

            migrationBuilder.RenameColumn(
                name: "SubmiterId",
                table: "Curriculums",
                newName: "TutorInfoUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Curriculums_SubmiterId",
                table: "Curriculums",
                newName: "IX_Curriculums_TutorInfoUserId");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "Certificates",
                newName: "Feedback");

            migrationBuilder.AddColumn<bool>(
                name: "IsApprove",
                table: "Tutors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFrom",
                table: "Tutors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprove",
                table: "Curriculums",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TutorId",
                table: "Curriculums",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprove",
                table: "Certificates",
                type: "bit",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Tutors_TutorInfoUserId",
                table: "Curriculums",
                column: "TutorInfoUserId",
                principalTable: "Tutors",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
