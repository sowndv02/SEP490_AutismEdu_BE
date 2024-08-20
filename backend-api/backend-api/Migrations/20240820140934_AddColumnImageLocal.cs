using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddColumnImageLocal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ApplicationClaims",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.AddColumn<string>(
                name: "ImageLocalUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageLocalUrl",
                table: "Users");

            migrationBuilder.InsertData(
                table: "ApplicationClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "CreatedDate", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Create", "True", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2537), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Delete", "True", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2546), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "Update", "True", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2548), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "Create", "Claim", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2549), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "Delete", "Claim", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2549), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, "Update", "Claim", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2550), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, "Create", "Role", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2551), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, "Delete", "Role", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2552), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, "Update", "Role", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2553), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, "Create", "User", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2553), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 11, "Delete", "User", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2554), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, "Update", "User", new DateTime(2024, 8, 12, 22, 11, 45, 11, DateTimeKind.Local).AddTicks(2554), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }
    }
}
