using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateDatabaseForCurriculumWorkCertificate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "WorkExperiences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalCurriculumId",
                table: "WorkExperiences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "WorkExperiences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Curriculums",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalCurriculumId",
                table: "Curriculums",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Curriculums",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Certificates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalCurriculumId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Certificates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TutorProfileUpdateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TutorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutMe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorProfileUpdateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorProfileUpdateRequests_Users_ApprovedId",
                        column: x => x.ApprovedId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_OriginalCurriculumId",
                table: "WorkExperiences",
                column: "OriginalCurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_OriginalCurriculumId",
                table: "Curriculums",
                column: "OriginalCurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_OriginalCurriculumId",
                table: "Certificates",
                column: "OriginalCurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorProfileUpdateRequests_ApprovedId",
                table: "TutorProfileUpdateRequests",
                column: "ApprovedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Curriculums_OriginalCurriculumId",
                table: "Certificates",
                column: "OriginalCurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Curriculums_Curriculums_OriginalCurriculumId",
                table: "Curriculums",
                column: "OriginalCurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_Curriculums_OriginalCurriculumId",
                table: "WorkExperiences",
                column: "OriginalCurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Curriculums_OriginalCurriculumId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Curriculums_Curriculums_OriginalCurriculumId",
                table: "Curriculums");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_Curriculums_OriginalCurriculumId",
                table: "WorkExperiences");

            migrationBuilder.DropTable(
                name: "TutorProfileUpdateRequests");

            migrationBuilder.DropIndex(
                name: "IX_WorkExperiences_OriginalCurriculumId",
                table: "WorkExperiences");

            migrationBuilder.DropIndex(
                name: "IX_Curriculums_OriginalCurriculumId",
                table: "Curriculums");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_OriginalCurriculumId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "OriginalCurriculumId",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "WorkExperiences");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "OriginalCurriculumId",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Curriculums");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "OriginalCurriculumId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Certificates");
        }
    }
}
