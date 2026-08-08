namespace Diamono.Application.Audit;

public sealed record AuditQuery(
    DateOnly? Date = null,
    string? User = null,
    string? Action = null,
    string? EntityType = null,
    int Take = 200);
