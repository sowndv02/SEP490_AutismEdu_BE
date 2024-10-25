using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDb_v6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercisese_ExerciseTypes_ExerciseTypeId",
                table: "Exercisese");

            migrationBuilder.DropIndex(
                name: "IX_Exercisese_ExerciseTypeId",
                table: "Exercisese");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Exercisese_ExerciseTypeId",
                table: "Exercisese",
                column: "ExerciseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercisese_ExerciseTypes_ExerciseTypeId",
                table: "Exercisese",
                column: "ExerciseTypeId",
                principalTable: "ExerciseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
