namespace MovieReservation.Application.Common.Models;

/// <summary>
/// Kết quả phân trang dùng chung cho mọi danh sách: các phần tử của trang hiện tại kèm thông tin
/// để frontend vẽ thanh phân trang ("75 phim · Trang 2/8").
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    // Làm tròn lên: 75 phim, 10 phim/trang → 8 trang
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
