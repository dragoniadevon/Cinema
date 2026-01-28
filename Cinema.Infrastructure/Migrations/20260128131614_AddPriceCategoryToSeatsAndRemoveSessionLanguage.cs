using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCategoryToSeatsAndRemoveSessionLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Sessions");

            migrationBuilder.AddColumn<int>(
                name: "Pricecategoryid",
                table: "Seats",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Pricecategoryid",
                table: "Seats",
                column: "Pricecategoryid");

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_Pricecategories_Pricecategoryid",
                table: "Seats",
                column: "Pricecategoryid",
                principalTable: "Pricecategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seats_Pricecategories_Pricecategoryid",
                table: "Seats");

            migrationBuilder.DropIndex(
                name: "IX_Seats_Pricecategoryid",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "Pricecategoryid",
                table: "Seats");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
