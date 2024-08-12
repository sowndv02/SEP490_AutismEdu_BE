using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddColumnImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageLocalPathUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2537));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2546));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2548));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2549));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2549));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2550));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2551));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2552));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2553));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2553));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2554));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2554));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageLocalPathUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4385));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4395));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4396));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4397));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4398));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4398));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4399));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4400));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4401));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4401));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4402));

            migrationBuilder.UpdateData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2024, 8, 11, 21, 20, 52, 99, DateTimeKind.Local).AddTicks(4403));
        }
    }
}
