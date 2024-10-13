using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDatabase_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Tutors_RevieweeId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_ReviewerId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "ReviewerId",
                table: "Reviews",
                newName: "TutorId");

            migrationBuilder.RenameColumn(
                name: "RevieweeId",
                table: "Reviews",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_ReviewerId",
                table: "Reviews",
                newName: "IX_Reviews_TutorId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_RevieweeId",
                table: "Reviews",
                newName: "IX_Reviews_ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Tutors_TutorId",
                table: "Reviews",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_ParentId",
                table: "Reviews",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Tutors_TutorId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_ParentId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "TutorId",
                table: "Reviews",
                newName: "ReviewerId");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "Reviews",
                newName: "RevieweeId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_TutorId",
                table: "Reviews",
                newName: "IX_Reviews_ReviewerId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_ParentId",
                table: "Reviews",
                newName: "IX_Reviews_RevieweeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Tutors_RevieweeId",
                table: "Reviews",
                column: "RevieweeId",
                principalTable: "Tutors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_ReviewerId",
                table: "Reviews",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
