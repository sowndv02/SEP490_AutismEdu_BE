using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutismEduConnectSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercisese_Exercisese_OriginalId",
                table: "Exercisese");

            migrationBuilder.DropIndex(
                name: "IX_Exercisese_OriginalId",
                table: "Exercisese");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Exercisese");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Exercisese");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Exercisese");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Exercisese",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "Exercisese",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Exercisese",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Exercisese_OriginalId",
                table: "Exercisese",
                column: "OriginalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercisese_Exercisese_OriginalId",
                table: "Exercisese",
                column: "OriginalId",
                principalTable: "Exercisese",
                principalColumn: "Id");
        }
    }
}
