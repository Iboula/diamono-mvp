using Diamono.Application.Reporting;

namespace Diamono.Application.Tests;

internal sealed class FakeReportingRepository(ReportingData data) : IReportingRepository
{
    public int CallCount { get; private set; }

    public Task<ReportingData> GetReportingDataAsync(
        ReportingPeriod period,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(data);
    }
}
