using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateStudentProfileFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressReports_ChildInformations_ChildId",
                table: "ProgressReports");

            migrationBuilder.RenameColumn(
                name: "ChildId",
                table: "ProgressReports",
                newName: "StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressReports_ChildId",
                table: "ProgressReports",
                newName: "IX_ProgressReports_StudentProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressReports_StudentProfiles_StudentProfileId",
                table: "ProgressReports",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressReports_StudentProfiles_StudentProfileId",
                table: "ProgressReports");

            migrationBuilder.RenameColumn(
                name: "StudentProfileId",
                table: "ProgressReports",
                newName: "ChildId");

            migrationBuilder.RenameIndex(
                name: "IX_ProgressReports_StudentProfileId",
                table: "ProgressReports",
                newName: "IX_ProgressReports_ChildId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressReports_ChildInformations_ChildId",
                table: "ProgressReports",
                column: "ChildId",
                principalTable: "ChildInformations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
