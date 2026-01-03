using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StoreId_IsActive",
                table: "Employees",
                columns: new[] { "StoreId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_StoreId_IsActive",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employees");
        }
    }
}
