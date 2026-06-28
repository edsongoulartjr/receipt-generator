using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptGenerator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\" = 'SystemAdmin' WHERE \"Role\" = 'SuperAdmin'");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\" = 'Driver' WHERE \"Role\" = 'Operator'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\" = 'SuperAdmin' WHERE \"Role\" = 'SystemAdmin'");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Role\" = 'Operator' WHERE \"Role\" = 'Driver'");
        }
    }
}
