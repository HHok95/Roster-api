using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRosters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RosterDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalShiftId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartSlot = table.Column<int>(type: "int", nullable: false),
                    EndSlot = table.Column<int>(type: "int", nullable: false),
                    BreaksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RolesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_RosterDays_RosterDayId",
                        column: x => x.RosterDayId,
                        principalTable: "RosterDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RosterDays_StoreId_Date",
                table: "RosterDays",
                columns: new[] { "StoreId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_EmployeeId",
                table: "Shifts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_RosterDayId_ExternalShiftId",
                table: "Shifts",
                columns: new[] { "RosterDayId", "ExternalShiftId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "RosterDays");
        }
    }
}
