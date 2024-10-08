using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateTableReview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_RevieweeId",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Tutors_RevieweeId",
                table: "Reviews",
                column: "RevieweeId",
                principalTable: "Tutors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Tutors_RevieweeId",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_RevieweeId",
                table: "Reviews",
                column: "RevieweeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
