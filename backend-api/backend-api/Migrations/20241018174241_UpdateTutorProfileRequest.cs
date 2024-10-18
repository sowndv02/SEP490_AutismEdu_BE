using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateTutorProfileRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TutorId",
                table: "TutorProfileUpdateRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "TutorProfileUpdateRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EndAge",
                table: "TutorProfileUpdateRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "TutorProfileUpdateRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "TutorProfileUpdateRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StartAge",
                table: "TutorProfileUpdateRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TutorProfileUpdateRequests_TutorId",
                table: "TutorProfileUpdateRequests",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TutorProfileUpdateRequests_Tutors_TutorId",
                table: "TutorProfileUpdateRequests",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TutorProfileUpdateRequests_Tutors_TutorId",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropIndex(
                name: "IX_TutorProfileUpdateRequests_TutorId",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "EndAge",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "StartAge",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.AlterColumn<string>(
                name: "TutorId",
                table: "TutorProfileUpdateRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
