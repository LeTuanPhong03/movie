using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Exceptions;

namespace MovieReservation.Api.Middleware;

/// <summary>
/// Đổi exception thành response lỗi theo chuẩn ProblemDetails: lỗi nghiệp vụ → 400/401/404/409 kèm message,bug → 500 không lộ chi tiết (chỉ ghi log). Handler MediatR ở tầng Application chỉ ném exception vì nó không biết mình chạy qua HTTP (test, background job cũng gọi nó), nên tầng Api dịch sang mã HTTP tại đây.
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException   => (StatusCodes.Status400BadRequest, "Dữ liệu không hợp lệ"),
            NotFoundException     => (StatusCodes.Status404NotFound, "Không tìm thấy tài nguyên"),
            ConflictException     => (StatusCodes.Status409Conflict, "Xung đột tài nguyên"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Xác thực thất bại"),
            _                     => (StatusCodes.Status500InternalServerError, "Đã có lỗi xảy ra")
        };

        // Bug thật (500): ghi log đầy đủ ở server, KHÔNG trả ex.Message cho client
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Lỗi không xác định.");
        ProblemDetails problem;
        if(exception is ValidationException validationException)
        {
            problem = new ValidationProblemDetails(
                validationException.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }
        else
        {
            problem = new ProblemDetails {Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message };
        }
        problem.Status = status;
        problem.Title = title;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
        problem, problem.GetType(), (JsonSerializerOptions?)null, "application/problem+json", cancellationToken);

        return true;   // luôn xử lý xong, không để rơi vào đường dự phòng
    }
}
