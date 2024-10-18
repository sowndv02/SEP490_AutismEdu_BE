using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateScheduleTimeSlotFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleTimeSlots_StudentProfiles_StudentProfileId1",
                table: "ScheduleTimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleTimeSlots_StudentProfileId1",
                table: "ScheduleTimeSlots");

            migrationBuilder.DropColumn(
                name: "StudentProfileId1",
                table: "ScheduleTimeSlots");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentProfileId1",
                table: "ScheduleTimeSlots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTimeSlots_StudentProfileId1",
                table: "ScheduleTimeSlots",
                column: "StudentProfileId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleTimeSlots_StudentProfiles_StudentProfileId1",
                table: "ScheduleTimeSlots",
                column: "StudentProfileId1",
                principalTable: "StudentProfiles",
                principalColumn: "Id");
        }
    }
}
