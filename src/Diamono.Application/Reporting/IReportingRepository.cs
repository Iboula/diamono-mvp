namespace Diamono.Application.Reporting;

public interface IReportingRepository
{
    Task<ReportingData> GetReportingDataAsync(
        ReportingPeriod period,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
