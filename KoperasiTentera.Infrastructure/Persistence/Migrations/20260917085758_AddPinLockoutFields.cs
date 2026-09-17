using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoperasiTentera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPinLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedPinAttempts",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinLockedUntilUtc",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "FailedPinAttempts", "PinLockedUntilUtc" },
                values: new object[] { 0, null });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "FailedPinAttempts", "PinLockedUntilUtc" },
                values: new object[] { 0, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedPinAttempts",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PinLockedUntilUtc",
                table: "Customers");
        }
    }
}
