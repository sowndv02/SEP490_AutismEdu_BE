using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddtableLicense : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Licences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuingDate",
                table: "Licences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuingInstitution",
                table: "Licences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tutors",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FormalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceFrom = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceTo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartAge = table.Column<int>(type: "int", nullable: false),
                    EndAge = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Tutors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tutors");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Licences");

            migrationBuilder.DropColumn(
                name: "IssuingDate",
                table: "Licences");

            migrationBuilder.DropColumn(
                name: "IssuingInstitution",
                table: "Licences");
        }
    }
}
