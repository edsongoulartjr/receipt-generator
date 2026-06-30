using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptServiceDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceEndDate",
                table: "Receipts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceStartDate",
                table: "Receipts",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceEndDate",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ServiceStartDate",
                table: "Receipts");
        }
    }
}
