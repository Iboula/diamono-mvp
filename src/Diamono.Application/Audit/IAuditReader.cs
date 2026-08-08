namespace Diamono.Application.Audit;

public interface IAuditReader
{
    Task<IReadOnlyList<AuditReadModel>> GetAuditEntriesAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);
}
