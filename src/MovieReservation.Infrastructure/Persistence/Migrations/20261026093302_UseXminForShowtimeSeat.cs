using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieReservation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseXminForShowtimeSeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dùng cột hệ thống xmin (Postgres có sẵn ở mọi bảng) làm version, nên bỏ cột RowVersion tự tạo.
            // EF sinh nhầm thành RENAME sang xmin, mà Postgres cấm đặt tên trùng cột hệ thống, nên sửa tay.
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ShowtimeSeats");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
            name: "RowVersion",
            table: "ShowtimeSeats",
            type: "integer",
            nullable: false,
            defaultValue: 0);
        }
    }
}
