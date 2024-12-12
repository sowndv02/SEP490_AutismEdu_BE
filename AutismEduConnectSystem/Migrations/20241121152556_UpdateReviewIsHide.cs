using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutismEduConnectSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReviewIsHide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHide",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHide",
                table: "Reviews");
        }
    }
}
