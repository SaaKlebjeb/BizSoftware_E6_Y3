using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class ReportService(IReportRepository reportRepository)
{
    public Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(Session session, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        ValidateRange(session, start, end);
        return reportRepository.GetDailySalesAsync(start.Date, end.Date.AddDays(1), cancellationToken);
    }

    public Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(Session session, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        ValidateRange(session, start, end);
        return reportRepository.GetTopProductsAsync(start.Date, end.Date.AddDays(1), cancellationToken);
    }

    private static void ValidateRange(Session session, DateTime start, DateTime end)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (end.Date < start.Date)
        {
            throw new ArgumentException("End date must be on or after the start date.");
        }
    }
}
