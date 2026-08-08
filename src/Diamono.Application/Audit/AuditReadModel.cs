namespace Diamono.Application.Audit;

public sealed record AuditReadModel(
    Guid Id,
    DateTimeOffset OccurredAt,
    string UserId,
    string UserEmail,
    string Action,
    string EntityType,
    string EntityId,
    string Description,
    string? OldValuesJson,
    string? NewValuesJson,
    string? MetadataJson);
