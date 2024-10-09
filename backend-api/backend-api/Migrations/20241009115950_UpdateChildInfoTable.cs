using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateChildInfoTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialCondition",
                table: "ChildInformations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitialCondition",
                table: "ChildInformations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
