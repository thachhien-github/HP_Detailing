using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HP_Detailing.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketStaffAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedStaffId",
                table: "Tickets",
                column: "AssignedStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Staff_AssignedStaffId",
                table: "Tickets",
                column: "AssignedStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Staff_AssignedStaffId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedStaffId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssignedStaffId",
                table: "Tickets");
        }
    }
}
