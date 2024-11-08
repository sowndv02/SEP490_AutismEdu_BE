using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePackagePayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentHistories_PacketPayments_PackagePeymentId",
                table: "PaymentHistories");

            migrationBuilder.DropTable(
                name: "PacketPayments");

            migrationBuilder.RenameColumn(
                name: "PackagePeymentId",
                table: "PaymentHistories",
                newName: "PackagePaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentHistories_PackagePeymentId",
                table: "PaymentHistories",
                newName: "IX_PaymentHistories_PackagePaymentId");

            migrationBuilder.CreateTable(
                name: "PackagePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<double>(type: "float", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubmitterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackagePayments_PackagePayments_OriginalId",
                        column: x => x.OriginalId,
                        principalTable: "PackagePayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackagePayments_Users_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackagePayments_OriginalId",
                table: "PackagePayments",
                column: "OriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagePayments_SubmitterId",
                table: "PackagePayments",
                column: "SubmitterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentHistories_PackagePayments_PackagePaymentId",
                table: "PaymentHistories",
                column: "PackagePaymentId",
                principalTable: "PackagePayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentHistories_PackagePayments_PackagePaymentId",
                table: "PaymentHistories");

            migrationBuilder.DropTable(
                name: "PackagePayments");

            migrationBuilder.RenameColumn(
                name: "PackagePaymentId",
                table: "PaymentHistories",
                newName: "PackagePeymentId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentHistories_PackagePaymentId",
                table: "PaymentHistories",
                newName: "IX_PaymentHistories_PackagePeymentId");

            migrationBuilder.CreateTable(
                name: "PacketPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalId = table.Column<int>(type: "int", nullable: true),
                    SubmitterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacketPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacketPayments_PacketPayments_OriginalId",
                        column: x => x.OriginalId,
                        principalTable: "PacketPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PacketPayments_Users_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PacketPayments_OriginalId",
                table: "PacketPayments",
                column: "OriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_PacketPayments_SubmitterId",
                table: "PacketPayments",
                column: "SubmitterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentHistories_PacketPayments_PackagePeymentId",
                table: "PaymentHistories",
                column: "PackagePeymentId",
                principalTable: "PacketPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
