namespace Diamono.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(AuditWriteRequest request, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
