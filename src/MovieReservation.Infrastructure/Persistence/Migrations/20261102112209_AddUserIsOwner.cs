using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieReservation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOwner",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOwner",
                table: "Users");
        }
    }
}
