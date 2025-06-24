using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackFlow.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "role_description", "role_name" },
                values: new object[] { 1, "", "Admin" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "name", "password", "RoleId", "role_id" },
                values: new object[] { 1, new DateTime(2025, 6, 24, 21, 57, 35, 989, DateTimeKind.Utc).AddTicks(4079), "sipho@example.com", "Siphokazi", "placeholder-password", null, 1 });

            migrationBuilder.InsertData(
                table: "projects",
                columns: new[] { "id", "created_by", "project_description", "project_due_date", "project_name", "project_start_date", "project_status" },
                values: new object[] { 1, 1, "This is a test project.", null, "Seed Project", null, "Active" });

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets",
                column: "assigned_to",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets");

            migrationBuilder.DeleteData(
                table: "projects",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets",
                column: "assigned_to",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
