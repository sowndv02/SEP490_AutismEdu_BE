using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class Exercise : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TutorRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "TutorRegistrationRequests");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "TutorProfileUpdateRequests",
                newName: "PriceFrom");

            migrationBuilder.AlterColumn<string>(
                name: "AboutMe",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceEnd",
                table: "Tutors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFrom",
                table: "Tutors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<float>(
                name: "SessionHours",
                table: "Tutors",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "AboutMe",
                table: "TutorRegistrationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceEnd",
                table: "TutorRegistrationRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFrom",
                table: "TutorRegistrationRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<float>(
                name: "SessionHours",
                table: "TutorRegistrationRequests",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceEnd",
                table: "TutorProfileUpdateRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<float>(
                name: "SessionHours",
                table: "TutorProfileUpdateRequests",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateTable(
                name: "ExerciseType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExerciseTypeId = table.Column<int>(type: "int", nullable: false),
                    ExerciseContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercise_ExerciseType_ExerciseTypeId",
                        column: x => x.ExerciseTypeId,
                        principalTable: "ExerciseType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exercise_Tutors_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutors",
                        principalColumn: "TutorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_ExerciseTypeId",
                table: "Exercise",
                column: "ExerciseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercise_TutorId",
                table: "Exercise",
                column: "TutorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Exercise");

            migrationBuilder.DropTable(
                name: "ExerciseType");

            migrationBuilder.DropColumn(
                name: "PriceEnd",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "PriceFrom",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "SessionHours",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "AboutMe",
                table: "TutorRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "PriceEnd",
                table: "TutorRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "PriceFrom",
                table: "TutorRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "SessionHours",
                table: "TutorRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "PriceEnd",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.DropColumn(
                name: "SessionHours",
                table: "TutorProfileUpdateRequests");

            migrationBuilder.RenameColumn(
                name: "PriceFrom",
                table: "TutorProfileUpdateRequests",
                newName: "Price");

            migrationBuilder.AlterColumn<string>(
                name: "AboutMe",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Tutors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TutorRegistrationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "TutorRegistrationRequests",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
