using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateCertificate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Certificates_OriginalId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_OriginalId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Certificates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Certificates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Certificates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_OriginalId",
                table: "Certificates",
                column: "OriginalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Certificates_OriginalId",
                table: "Certificates",
                column: "OriginalId",
                principalTable: "Certificates",
                principalColumn: "Id");
        }
    }
}
