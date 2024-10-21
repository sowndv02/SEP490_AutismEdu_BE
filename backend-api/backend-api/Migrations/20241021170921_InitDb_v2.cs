using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDb_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_ChildInformations_ChildId",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "ChildId",
                table: "Schedules",
                newName: "StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_ChildId",
                table: "Schedules",
                newName: "IX_Schedules_StudentProfileId");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "End",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Start",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_StudentProfiles_StudentProfileId",
                table: "Schedules",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_StudentProfiles_StudentProfileId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "End",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Start",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "StudentProfileId",
                table: "Schedules",
                newName: "ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_StudentProfileId",
                table: "Schedules",
                newName: "IX_Schedules_ChildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_ChildInformations_ChildId",
                table: "Schedules",
                column: "ChildId",
                principalTable: "ChildInformations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
