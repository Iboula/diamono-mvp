using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Audit;
using Diamono.Domain.Security;
using Diamono.Domain.Settings;

namespace Diamono.Application.Settings;

/// <summary>
/// Lecture/ecriture des parametres depuis le backoffice, sous permission.
/// Le site public n'emprunte pas ce service : la tarification et les
/// disponibilites consomment <see cref="IStadiumBookingSettingsRepository"/>
/// directement et restent donc anonymes.
/// </summary>
public sealed class StadiumSettingsApplicationService(
    IStadiumBookingSettingsRepository repository,
    IPermissionGuard permissionGuard,
    IAuditWriter auditWriter)
{
    public async Task<StadiumBookingSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.SettingsView, cancellationToken);
        return await repository.GetAsync(cancellationToken);
    }

    public async Task<StadiumBookingSettings> UpdateSettingsAsync(
        UpdateStadiumBookingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.SettingsManage, cancellationToken);

        var settings = await repository.GetAsync(cancellationToken);
        var oldValues = Snapshot(settings);
        settings.Update(
            request.OpensAt,
            request.ClosesAt,
            request.MinimumDurationHours,
            request.MaximumDurationHours,
            request.StandardHourlyRate,
            request.LocalAscHourlyRate,
            request.LightingHourlyRate,
            request.LightingStartsAt,
            request.DepositAmount,
            request.MaximumAdvanceBookingDays,
            request.PaymentDeadlineHours,
            request.ApprovalRequired,
            request.UpdatedBy);

        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.SettingsUpdated,
            "StadiumBookingSettings",
            settings.Id.ToString(),
            "Parametres metier modifies.",
            OldValues: oldValues,
            NewValues: Snapshot(settings)),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static object Snapshot(StadiumBookingSettings settings)
        => new
        {
            opensAt = settings.OpensAt.ToString("HH:mm"),
            closesAt = settings.ClosesAt.ToString("HH:mm"),
            settings.MinimumDurationHours,
            settings.MaximumDurationHours,
            settings.StandardHourlyRate,
            settings.LocalAscHourlyRate,
            settings.LightingHourlyRate,
            lightingStartsAt = settings.LightingStartsAt.ToString("HH:mm"),
            settings.DepositAmount,
            settings.MaximumAdvanceBookingDays,
            settings.PaymentDeadlineHours,
            settings.ApprovalRequired
        };
}
