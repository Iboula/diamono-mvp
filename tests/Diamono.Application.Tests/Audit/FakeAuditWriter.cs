using Diamono.Application.Audit;

namespace Diamono.Application.Tests;

internal sealed class FakeAuditWriter : IAuditWriter
{
    public List<AuditWriteRequest> Entries { get; } = [];
    public int SaveChangesCallCount { get; private set; }

    public Task WriteAsync(AuditWriteRequest request, CancellationToken cancellationToken = default)
    {
        Entries.Add(request);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
