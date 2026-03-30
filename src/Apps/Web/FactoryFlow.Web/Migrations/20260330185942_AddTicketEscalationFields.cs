using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryFlow.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketEscalationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstEscalatedAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EscalationLevel",
                table: "Tickets",
                column: "EscalationLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_EscalationLevel",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FirstEscalatedAtUtc",
                table: "Tickets");
        }
    }
}
