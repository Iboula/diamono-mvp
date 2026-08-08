namespace Diamono.Application.Audit;

public sealed record AuditWriteRequest(
    string Action,
    string EntityType,
    string EntityId,
    string Description,
    object? OldValues = null,
    object? NewValues = null,
    object? Metadata = null);
