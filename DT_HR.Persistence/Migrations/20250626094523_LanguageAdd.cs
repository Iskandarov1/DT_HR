using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DT_HR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LanguageAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "uz");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "users");
        }
    }
}
