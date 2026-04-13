using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanConfirm",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDispatch",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
                
            // Set Admin role to have all permissions
            migrationBuilder.Sql(
                "UPDATE \"Roles\" SET \"CanConfirm\" = true, \"CanDispatch\" = true WHERE \"Name\" = 'Admin'");
                
            // Set Manager role to have dispatch permissions
            migrationBuilder.Sql(
                "UPDATE \"Roles\" SET \"CanDispatch\" = true WHERE \"Name\" = 'Manager'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanConfirm",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanDispatch",
                table: "Roles");
        }
    }
}