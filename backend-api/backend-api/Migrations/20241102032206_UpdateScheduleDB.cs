using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateScheduleDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SyllabusId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SyllabusId",
                table: "Schedules",
                column: "SyllabusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Syllabuses_SyllabusId",
                table: "Schedules",
                column: "SyllabusId",
                principalTable: "Syllabuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Syllabuses_SyllabusId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SyllabusId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SyllabusId",
                table: "Schedules");
        }
    }
}
