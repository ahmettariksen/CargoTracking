using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetWarehouseToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetWarehouseId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TargetWarehouseId",
                table: "Shipments",
                column: "TargetWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Warehouses_TargetWarehouseId",
                table: "Shipments",
                column: "TargetWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Warehouses_TargetWarehouseId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TargetWarehouseId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TargetWarehouseId",
                table: "Shipments");
        }
    }
}
