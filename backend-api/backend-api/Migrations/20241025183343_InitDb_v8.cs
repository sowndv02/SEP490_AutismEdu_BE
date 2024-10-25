using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDb_v8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercise_Exercisese_ExerciseId",
                table: "SyllabusExercise");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercise_ExerciseTypes_ExerciseTypeId",
                table: "SyllabusExercise");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercise_Syllabuses_SyllabusId",
                table: "SyllabusExercise");

            migrationBuilder.DropTable(
                name: "ExerciseTypeExercises");

            migrationBuilder.DropTable(
                name: "SyllabusExerciseTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyllabusExercise",
                table: "SyllabusExercise");

            migrationBuilder.RenameTable(
                name: "SyllabusExercise",
                newName: "SyllabusExercises");

            migrationBuilder.RenameIndex(
                name: "IX_SyllabusExercise_ExerciseTypeId",
                table: "SyllabusExercises",
                newName: "IX_SyllabusExercises_ExerciseTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_SyllabusExercise_ExerciseId",
                table: "SyllabusExercises",
                newName: "IX_SyllabusExercises_ExerciseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyllabusExercises",
                table: "SyllabusExercises",
                columns: new[] { "SyllabusId", "ExerciseTypeId", "ExerciseId" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercises_Exercisese_ExerciseId",
                table: "SyllabusExercises",
                column: "ExerciseId",
                principalTable: "Exercisese",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercises_ExerciseTypes_ExerciseTypeId",
                table: "SyllabusExercises",
                column: "ExerciseTypeId",
                principalTable: "ExerciseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercises_Syllabuses_SyllabusId",
                table: "SyllabusExercises",
                column: "SyllabusId",
                principalTable: "Syllabuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercisese_ExerciseTypes_ExerciseTypeId",
                table: "Exercisese");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercises_Exercisese_ExerciseId",
                table: "SyllabusExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercises_ExerciseTypes_ExerciseTypeId",
                table: "SyllabusExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusExercises_Syllabuses_SyllabusId",
                table: "SyllabusExercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercisese_ExerciseTypeId",
                table: "Exercisese");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyllabusExercises",
                table: "SyllabusExercises");

            migrationBuilder.RenameTable(
                name: "SyllabusExercises",
                newName: "SyllabusExercise");

            migrationBuilder.RenameIndex(
                name: "IX_SyllabusExercises_ExerciseTypeId",
                table: "SyllabusExercise",
                newName: "IX_SyllabusExercise_ExerciseTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_SyllabusExercises_ExerciseId",
                table: "SyllabusExercise",
                newName: "IX_SyllabusExercise_ExerciseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyllabusExercise",
                table: "SyllabusExercise",
                columns: new[] { "SyllabusId", "ExerciseTypeId", "ExerciseId" });

            migrationBuilder.CreateTable(
                name: "ExerciseTypeExercises",
                columns: table => new
                {
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercise_Exercisese_ExerciseId",
                table: "SyllabusExercise",
                column: "ExerciseId",
                principalTable: "Exercisese",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercise_ExerciseTypes_ExerciseTypeId",
                table: "SyllabusExercise",
                column: "ExerciseTypeId",
                principalTable: "ExerciseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyllabusExercise_Syllabuses_SyllabusId",
                table: "SyllabusExercise",
                column: "SyllabusId",
                principalTable: "Syllabuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
