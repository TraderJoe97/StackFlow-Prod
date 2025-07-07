using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackFlow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "users");

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isVerified",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "isVerified",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "role_description", "role_name" },
                values: new object[] { 1, "", "Admin" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "name", "password", "RoleId", "role_id" },
                values: new object[] { 1, new DateTime(2025, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "sipho@example.com", "Siphokazi", "placeholder-password", null, 1 });

            migrationBuilder.InsertData(
                table: "projects",
                columns: new[] { "id", "created_by", "project_description", "project_due_date", "project_name", "project_start_date", "project_status" },
                values: new object[] { 1, 1, "This is a test project.", null, "Seed Project", null, "Active" });
        }
    }
}
