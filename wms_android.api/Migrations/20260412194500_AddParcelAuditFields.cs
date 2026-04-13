using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddParcelAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Parcels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedById",
                table: "Parcels",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_ConfirmedById",
                table: "Parcels",
                column: "ConfirmedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels",
                column: "ConfirmedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_ConfirmedById",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "ConfirmedById",
                table: "Parcels");
        }
    }
}