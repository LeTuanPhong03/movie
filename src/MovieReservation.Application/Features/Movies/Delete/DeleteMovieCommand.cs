using MediatR;

namespace MovieReservation.Application.Features.Movies.Delete;

/// <summary>
/// Yêu cầu xóa phim (chỉ Admin). Mặc định là ẩn phim (soft delete, IsActive = false);
/// HardDelete = true thì xóa hẳn dòng trong DB, chỉ khi phim chưa từng có suất chiếu.
/// </summary>
public record DeleteMovieCommand(int Id, bool HardDelete = false) : IRequest;
