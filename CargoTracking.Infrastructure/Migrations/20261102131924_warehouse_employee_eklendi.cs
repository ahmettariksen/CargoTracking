using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class warehouse_employee_eklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WareHouseEmployeeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WareHouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WareHouseEmployeeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WareHouseEmployeeProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WareHouseEmployeeProfiles_Warehouses_WareHouseId",
                        column: x => x.WareHouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WareHouseEmployeeProfiles_UserId",
                table: "WareHouseEmployeeProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WareHouseEmployeeProfiles_WareHouseId",
                table: "WareHouseEmployeeProfiles",
                column: "WareHouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WareHouseEmployeeProfiles");
        }
    }
}
