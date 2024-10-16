using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateTutorRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TutorRequests_Tutors_TutorUserId",
                table: "TutorRequests");

            migrationBuilder.DropIndex(
                name: "IX_TutorRequests_TutorUserId",
                table: "TutorRequests");

            migrationBuilder.DropColumn(
                name: "TutorUserId",
                table: "TutorRequests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TutorUserId",
                table: "TutorRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TutorRequests_TutorUserId",
                table: "TutorRequests",
                column: "TutorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TutorRequests_Tutors_TutorUserId",
                table: "TutorRequests",
                column: "TutorUserId",
                principalTable: "Tutors",
                principalColumn: "UserId");
        }
    }
}
