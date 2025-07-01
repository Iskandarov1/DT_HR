using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DT_HR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BirthDayAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "users",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "users");
        }
    }
}
