using System.Text.Json;
using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Audit;

namespace Diamono.Infrastructure.Persistence;

public sealed class AuditWriter(
    DiamonoDbContext db,
    ICurrentUserAccessor currentUserAccessor)
    : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task WriteAsync(AuditWriteRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var userId = currentUser?.Id ?? string.Empty;
        var userEmail = currentUser?.Email ?? "system";

        var entry = new AuditEntry(
            userId,
            userEmail,
            request.Action,
            request.EntityType,
            request.EntityId,
            request.Description,
            Serialize(request.OldValues),
            Serialize(request.NewValues),
            Serialize(request.Metadata));

        await db.AuditLogs.AddAsync(entry, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);

    private static string? Serialize(object? value)
        => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
