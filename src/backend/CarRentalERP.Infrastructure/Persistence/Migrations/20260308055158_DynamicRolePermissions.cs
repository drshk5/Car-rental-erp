using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DynamicRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleType",
                table: "Roles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleType",
                table: "Roles",
                column: "RoleType",
                unique: true);
        }
    }
}
