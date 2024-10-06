using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateTableTutor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "TutorRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "TutorRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestStatus",
                table: "TutorRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "TutorRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorUserId",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TutorUserId",
                table: "Reviews",
                column: "TutorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Tutors_TutorUserId",
                table: "Reviews",
                column: "TutorUserId",
                principalTable: "Tutors",
                principalColumn: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Tutors_TutorUserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TutorUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "TutorRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "TutorRequests");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "TutorRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "TutorRequests");

            migrationBuilder.DropColumn(
                name: "TutorUserId",
                table: "Reviews");
        }
    }
}
