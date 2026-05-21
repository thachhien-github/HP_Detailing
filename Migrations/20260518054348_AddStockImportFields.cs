using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HP_Detailing.Migrations
{
    /// <inheritdoc />
    public partial class AddStockImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceLink",
                table: "StockImports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "StockImports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "StockImports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceLink",
                table: "StockImports");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "StockImports");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "StockImports");
        }
    }
}
