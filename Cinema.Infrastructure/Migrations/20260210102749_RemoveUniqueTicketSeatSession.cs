using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueTicketSeatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Sessionid_Seatid",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Sessionid",
                table: "Tickets",
                column: "Sessionid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Sessionid",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Sessionid_Seatid",
                table: "Tickets",
                columns: new[] { "Sessionid", "Seatid" },
                unique: true);
        }
    }
}
