using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixInitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_ticket_created_by",
                table: "tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets",
                column: "assigned_to",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_ticket_created_by",
                table: "tickets",
                column: "ticket_created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_users_ticket_created_by",
                table: "tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_assigned_to",
                table: "tickets",
                column: "assigned_to",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_users_ticket_created_by",
                table: "tickets",
                column: "ticket_created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
