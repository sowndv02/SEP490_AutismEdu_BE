using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateDatabaseChildMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildInformationMedias");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrlPath",
                table: "ChildInformations",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrlPath",
                table: "ChildInformations");

            migrationBuilder.CreateTable(
                name: "ChildInformationMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildInformationId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UrlPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildInformationMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildInformationMedias_ChildInformations_ChildInformationId",
                        column: x => x.ChildInformationId,
                        principalTable: "ChildInformations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildInformationMedias_ChildInformationId",
                table: "ChildInformationMedias",
                column: "ChildInformationId");
        }
    }
}
