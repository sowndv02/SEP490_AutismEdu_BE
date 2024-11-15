using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutismEduConnectSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExerciseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseTypes_ExerciseTypes_OriginalId",
                table: "ExerciseTypes");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseTypes_OriginalId",
                table: "ExerciseTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ExerciseTypes");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "ExerciseTypes");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "ExerciseTypes");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "ExerciseTypes",
                newName: "IsHide");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsHide",
                table: "ExerciseTypes",
                newName: "IsDeleted");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ExerciseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "ExerciseTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "ExerciseTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTypes_OriginalId",
                table: "ExerciseTypes",
                column: "OriginalId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTypes_ExerciseTypes_OriginalId",
                table: "ExerciseTypes",
                column: "OriginalId",
                principalTable: "ExerciseTypes",
                principalColumn: "Id");
        }
    }
}
