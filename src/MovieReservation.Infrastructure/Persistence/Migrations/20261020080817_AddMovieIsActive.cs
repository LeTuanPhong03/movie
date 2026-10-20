using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieReservation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Movies",
                type: "boolean",
                nullable: false,
                defaultValue: true); // sửa tay: phim đã có trước đó vẫn đang chiếu, không được bị ẩn
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Movies");
        }
    }
}
