using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDb_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseTypes_Syllabuses_SyllabusId",
                table: "ExerciseTypes");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseTypes_SyllabusId",
                table: "ExerciseTypes");

            migrationBuilder.DropColumn(
                name: "SyllabusId",
                table: "ExerciseTypes");

            migrationBuilder.CreateTable(
                name: "ExerciseTypeExercises",
                columns: table => new
                {
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTypeExercises", x => new { x.ExerciseTypeId, x.ExerciseId });
                    table.ForeignKey(
                        name: "FK_ExerciseTypeExercises_Exercisese_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercisese",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseTypeExercises_ExerciseTypes_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusExerciseTypes",
                columns: table => new
                {
                    SyllabusId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusExerciseTypes", x => new { x.SyllabusId, x.ExerciseTypeId });
                    table.ForeignKey(
                        name: "FK_SyllabusExerciseTypes_ExerciseTypes_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusExerciseTypes_Syllabuses_SyllabusId",
                        column: x => x.SyllabusId,
                        principalTable: "Syllabuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTypeExercises_ExerciseId",
                table: "ExerciseTypeExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusExerciseTypes_ExerciseTypeId",
                table: "SyllabusExerciseTypes",
                column: "ExerciseTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseTypeExercises");

            migrationBuilder.DropTable(
                name: "SyllabusExerciseTypes");

            migrationBuilder.AddColumn<int>(
                name: "SyllabusId",
                table: "ExerciseTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTypes_SyllabusId",
                table: "ExerciseTypes",
                column: "SyllabusId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTypes_Syllabuses_SyllabusId",
                table: "ExerciseTypes",
                column: "SyllabusId",
                principalTable: "Syllabuses",
                principalColumn: "Id");
        }
    }
}
