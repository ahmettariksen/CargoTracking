using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class farkli_kuryeler_eklendi_enumlar_string_tutuldu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_CourierProfiles_CourierId",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "CourierId",
                table: "Shipments",
                newName: "PickupCourierId");

            migrationBuilder.RenameIndex(
                name: "IX_Shipments_CourierId",
                table: "Shipments",
                newName: "IX_Shipments_PickupCourierId");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Shipments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryCourierId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "CourierProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DeliveryCourierId",
                table: "Shipments",
                column: "DeliveryCourierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_CourierProfiles_DeliveryCourierId",
                table: "Shipments",
                column: "DeliveryCourierId",
                principalTable: "CourierProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_CourierProfiles_PickupCourierId",
                table: "Shipments",
                column: "PickupCourierId",
                principalTable: "CourierProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_CourierProfiles_DeliveryCourierId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_CourierProfiles_PickupCourierId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DeliveryCourierId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryCourierId",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "PickupCourierId",
                table: "Shipments",
                newName: "CourierId");

            migrationBuilder.RenameIndex(
                name: "IX_Shipments_PickupCourierId",
                table: "Shipments",
                newName: "IX_Shipments_CourierId");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Shipments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "VehicleType",
                table: "CourierProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_CourierProfiles_CourierId",
                table: "Shipments",
                column: "CourierId",
                principalTable: "CourierProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
