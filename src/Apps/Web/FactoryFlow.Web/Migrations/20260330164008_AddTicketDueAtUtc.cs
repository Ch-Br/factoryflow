using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryFlow.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketDueAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueAtUtc",
                table: "Tickets");
        }
    }
}
