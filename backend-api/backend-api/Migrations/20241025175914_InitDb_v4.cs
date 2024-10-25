using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class InitDb_v4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Syllabuses_Syllabuses_OriginalId",
                table: "Syllabuses");

            migrationBuilder.DropIndex(
                name: "IX_Syllabuses_OriginalId",
                table: "Syllabuses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Syllabuses");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Syllabuses");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Syllabuses");

            migrationBuilder.CreateTable(
                name: "SyllabusExercise",
                columns: table => new
                {
                    SyllabusId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusExercise", x => new { x.SyllabusId, x.ExerciseTypeId, x.ExerciseId });
                    table.ForeignKey(
                        name: "FK_SyllabusExercise_Exercisese_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercisese",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusExercise_ExerciseTypes_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyllabusExercise_Syllabuses_SyllabusId",
                        column: x => x.SyllabusId,
                        principalTable: "Syllabuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusExercise_ExerciseId",
                table: "SyllabusExercise",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusExercise_ExerciseTypeId",
                table: "SyllabusExercise",
                column: "ExerciseTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyllabusExercise");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Syllabuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "Syllabuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Syllabuses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Syllabuses_OriginalId",
                table: "Syllabuses",
                column: "OriginalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Syllabuses_Syllabuses_OriginalId",
                table: "Syllabuses",
                column: "OriginalId",
                principalTable: "Syllabuses",
                principalColumn: "Id");
        }
    }
}
