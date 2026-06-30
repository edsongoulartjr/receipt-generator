using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptPayerTaxId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerTaxId",
                table: "Receipts",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayerTaxId",
                table: "Receipts");
        }
    }
}
