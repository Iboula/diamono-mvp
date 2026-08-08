using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class AuditReader(
    DiamonoDbContext db,
    IPermissionGuard permissionGuard)
    : IAuditReader
{
    public async Task<IReadOnlyList<AuditReadModel>> GetAuditEntriesAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);

        var entries = db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.Date is not null)
        {
            var start = new DateTimeOffset(query.Date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            entries = entries.Where(x => x.OccurredAt >= start && x.OccurredAt < end);
        }

        if (!string.IsNullOrWhiteSpace(query.User))
        {
            var user = query.User.Trim();
            entries = entries.Where(x =>
                x.UserEmail.Contains(user) ||
                x.UserId.Contains(user));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
            entries = entries.Where(x => x.Action == query.Action);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            entries = entries.Where(x => x.EntityType == query.EntityType);

        return await entries
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(query.Take, 1, 500))
            .Select(x => new AuditReadModel(
                x.Id,
                x.OccurredAt,
                x.UserId,
                x.UserEmail,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.Description,
                x.OldValuesJson,
                x.NewValuesJson,
                x.MetadataJson))
            .ToListAsync(cancellationToken);
    }
}
