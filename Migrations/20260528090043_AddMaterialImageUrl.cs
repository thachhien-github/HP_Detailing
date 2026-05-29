using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HP_Detailing.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Materials",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Materials");
        }
    }
}
