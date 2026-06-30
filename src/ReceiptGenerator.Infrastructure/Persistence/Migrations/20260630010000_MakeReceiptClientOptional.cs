using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeReceiptClientOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Clients_ClientId",
                table: "Receipts");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Receipts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Clients_ClientId",
                table: "Receipts",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Clients_ClientId",
                table: "Receipts");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Clients_ClientId",
                table: "Receipts",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
