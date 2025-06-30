using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DT_HR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoleUpdatedToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "Employee",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

        }
    }
}
