using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieReservation.Application.Features.Reservations.Expire;

namespace MovieReservation.Infrastructure.BackgroundJobs;

/// <summary>
/// Job chạy nền mỗi 30 giây: gửi ExpireReservationsCommand để dọn đơn Pending quá hạn.
/// Job chỉ là "đồng hồ báo thức" — logic nằm ở handler để test được bằng FakeTimeProvider.
/// Job sống suốt đời app (singleton) còn DbContext là scoped → mỗi vòng tạo scope mới.
/// </summary>
public class ReservationExpiryJob(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        // WaitForNextTickAsync trả false khi app tắt (stoppingToken bị huỷ) → thoát vòng lặp
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                int soDon = await sender.Send(new ExpireReservationsCommand(), stoppingToken);
                if (soDon > 0)
                    logger.LogInformation("Đã chuyển {SoDon} đơn quá hạn sang Expired", soDon);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Không để 1 vòng lỗi (mất kết nối DB...) giết cả job — log rồi chờ vòng sau
                logger.LogError(ex, "Lỗi khi dọn đơn quá hạn");
            }
        }
    }
}
