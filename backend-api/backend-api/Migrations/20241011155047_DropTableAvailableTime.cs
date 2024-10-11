using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class DropTableAvailableTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailableTimeSlots");

            migrationBuilder.DropTable(
                name: "AvailableTimes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailableTimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Weekday = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailableTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailableTimes_Users_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvailableTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekdayId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    From = table.Column<TimeSpan>(type: "time", nullable: false),
                    To = table.Column<TimeSpan>(type: "time", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailableTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailableTimeSlots_AvailableTimes_WeekdayId",
                        column: x => x.WeekdayId,
                        principalTable: "AvailableTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailableTimes_TutorId",
                table: "AvailableTimes",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailableTimeSlots_WeekdayId",
                table: "AvailableTimeSlots",
                column: "WeekdayId");
        }
    }
}
