using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "Receipts");
        }
    }
}
