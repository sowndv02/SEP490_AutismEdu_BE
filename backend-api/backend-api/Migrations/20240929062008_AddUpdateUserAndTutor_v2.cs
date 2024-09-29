using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddUpdateUserAndTutor_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tutors_TutorProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TutorProfileId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TutorProfileId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Tutors",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutors_UserId",
                table: "Tutors",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors");

            migrationBuilder.DropIndex(
                name: "IX_Tutors_UserId",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tutors");

            migrationBuilder.AddColumn<string>(
                name: "TutorProfileId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tutors",
                table: "Tutors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TutorProfileId",
                table: "Users",
                column: "TutorProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tutors_TutorProfileId",
                table: "Users",
                column: "TutorProfileId",
                principalTable: "Tutors",
                principalColumn: "UserId");
        }
    }
}
