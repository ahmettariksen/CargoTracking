using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnCourierToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReturnCourierId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ReturnCourierId",
                table: "Shipments",
                column: "ReturnCourierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_CourierProfiles_ReturnCourierId",
                table: "Shipments",
                column: "ReturnCourierId",
                principalTable: "CourierProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_CourierProfiles_ReturnCourierId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_ReturnCourierId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ReturnCourierId",
                table: "Shipments");
        }
    }
}
