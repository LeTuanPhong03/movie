using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Reports.Capacity;
using MovieReservation.Application.Features.Reports.Revenue;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Báo cáo cho Admin. Ví dụ: GET /api/reports/revenue?from=2026-11-01&amp;to=2026-11-07&amp;groupBy=Movie,
/// GET /api/reports/capacity?from=2026-11-01&amp;to=2026-11-07
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] GetRevenueReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpGet("capacity")]
    public async Task<ActionResult<CapacityReportDto>> Capacity([FromQuery] GetCapacityReportQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }
}
