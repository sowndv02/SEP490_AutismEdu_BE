using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddUpdateTableUserTutorAddCurrirulumWorkexperience : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WorkExperiences",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "TutorRegistrationRequestId",
                table: "WorkExperiences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_TutorRegistrationRequestId",
                table: "WorkExperiences",
                column: "TutorRegistrationRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_TutorRegistrationRequests_TutorRegistrationRequestId",
                table: "WorkExperiences",
                column: "TutorRegistrationRequestId",
                principalTable: "TutorRegistrationRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_TutorRegistrationRequests_TutorRegistrationRequestId",
                table: "WorkExperiences");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences");

            migrationBuilder.DropIndex(
                name: "IX_WorkExperiences_TutorRegistrationRequestId",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "TutorRegistrationRequestId",
                table: "WorkExperiences");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WorkExperiences",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Users_UserId",
                table: "WorkExperiences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
