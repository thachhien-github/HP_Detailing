using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HP_Detailing.Migrations
{
    /// <inheritdoc />
    public partial class AddCarAndTicketMaterialUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Plate",
                table: "Tickets",
                type: "nvarchar(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Plate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Plate);
                });

            migrationBuilder.Sql(@"
                INSERT INTO Cars (Plate, CreatedAt)
                SELECT DISTINCT Plate, GETUTCDATE()
                FROM Tickets
                WHERE Plate IS NOT NULL AND Plate <> '' AND Plate NOT IN (SELECT Plate FROM Cars)
            ");

            migrationBuilder.CreateTable(
                name: "TicketMaterialUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsChargedToCustomer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMaterialUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMaterialUsages_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketMaterialUsages_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Plate",
                table: "Tickets",
                column: "Plate");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMaterialUsages_MaterialId",
                table: "TicketMaterialUsages",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMaterialUsages_TicketId",
                table: "TicketMaterialUsages",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Cars_Plate",
                table: "Tickets",
                column: "Plate",
                principalTable: "Cars",
                principalColumn: "Plate",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Cars_Plate",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "TicketMaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Plate",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "Plate",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldNullable: true);
        }
    }
}
