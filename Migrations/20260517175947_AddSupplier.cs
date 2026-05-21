using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HP_Detailing.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "StockImports");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "StockImports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockImports_SupplierId",
                table: "StockImports",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockImports_Suppliers_SupplierId",
                table: "StockImports",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockImports_Suppliers_SupplierId",
                table: "StockImports");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_StockImports_SupplierId",
                table: "StockImports");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "StockImports");

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "StockImports",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
