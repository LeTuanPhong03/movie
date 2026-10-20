using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Movies.Create;
using MovieReservation.Application.Features.Movies.Delete;
using MovieReservation.Application.Features.Movies.DTOs;
using MovieReservation.Application.Features.Movies.GetMovieById;
using MovieReservation.Application.Features.Movies.GetMovies;
using MovieReservation.Application.Features.Movies.Update;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint phim. Mặc định chỉ Admin (AdminOnly trên class → action mới quên gắn vẫn bị khóa);
/// các action xem phim mở cho mọi người bằng [AllowAnonymous] vì khách chưa đăng nhập vẫn được xem phim.
/// </summary>
[ApiController]
[Route("api/movies")]
[Authorize(Policy = "AdminOnly")]
public class MoviesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<MovieDto>>> GetMovies(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMoviesQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<MovieDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMovieByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MovieDto>> Create(CreateMovieCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        // ASP.NET tự sinh URL /api/movies/{id} từ route của GetById → không gõ chuỗi tay
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MovieDto>> Update(int id, UpdateMovieCommand command, CancellationToken cancellationToken)
    {
        // Id lấy từ URL, gắn vào command bằng `with` (tạo bản sao record với Id mới)
        var result = await sender.Send(command with { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hardDelete, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteMovieCommand(id, hardDelete), cancellationToken);
        return NoContent();
    }
}
