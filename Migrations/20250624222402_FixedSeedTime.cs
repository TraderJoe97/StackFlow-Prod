using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixedSeedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 6, 24, 21, 57, 35, 989, DateTimeKind.Utc).AddTicks(4079));
        }
    }
}
