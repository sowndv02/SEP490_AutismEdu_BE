using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutismEduConnectSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReportTutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportTutorType",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportTutorType",
                table: "Reports");
        }
    }
}
