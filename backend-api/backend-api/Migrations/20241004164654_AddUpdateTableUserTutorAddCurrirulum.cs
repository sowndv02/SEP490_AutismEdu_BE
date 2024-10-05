using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class AddUpdateTableUserTutorAddCurrirulum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_SubmiterId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ImageLocalPathUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageLocalUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FormalName",
                table: "Tutors");

            migrationBuilder.AlterColumn<string>(
                name: "SubmiterId",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "TutorRegistrationRequestId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TutorRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartAge = table.Column<int>(type: "int", nullable: false),
                    EndAge = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorRegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorRegistrationRequests_Users_ApprovedId",
                        column: x => x.ApprovedId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Curriculums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeFrom = table.Column<int>(type: "int", nullable: false),
                    AgeEnd = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApprove = table.Column<bool>(type: "bit", nullable: false),
                    TutorId = table.Column<int>(type: "int", nullable: true),
                    TutorInfoUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TutorRegistrationRequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Curriculums_TutorRegistrationRequests_TutorRegistrationRequestId",
                        column: x => x.TutorRegistrationRequestId,
                        principalTable: "TutorRegistrationRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Curriculums_Tutors_TutorInfoUserId",
                        column: x => x.TutorInfoUserId,
                        principalTable: "Tutors",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TutorRegistrationRequestId",
                table: "Certificates",
                column: "TutorRegistrationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_TutorInfoUserId",
                table: "Curriculums",
                column: "TutorInfoUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_TutorRegistrationRequestId",
                table: "Curriculums",
                column: "TutorRegistrationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorRegistrationRequests_ApprovedId",
                table: "TutorRegistrationRequests",
                column: "ApprovedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_TutorRegistrationRequests_TutorRegistrationRequestId",
                table: "Certificates",
                column: "TutorRegistrationRequestId",
                principalTable: "TutorRegistrationRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_SubmiterId",
                table: "Certificates",
                column: "SubmiterId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_TutorRegistrationRequests_TutorRegistrationRequestId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Users_SubmiterId",
                table: "Certificates");

            migrationBuilder.DropTable(
                name: "Curriculums");

            migrationBuilder.DropTable(
                name: "TutorRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_TutorRegistrationRequestId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "TutorRegistrationRequestId",
                table: "Certificates");

            migrationBuilder.AddColumn<string>(
                name: "ImageLocalPathUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageLocalUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormalName",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SubmiterId",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Users_SubmiterId",
                table: "Certificates",
                column: "SubmiterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
