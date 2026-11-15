using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageLockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAt",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_NextRetryAt_LockedUntil",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "NextRetryAt", "LockedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_DeadLetteredAt_NextRetryAt_LockedUntil",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");
        }
    }
}
