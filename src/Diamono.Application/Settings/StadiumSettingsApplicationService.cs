using Diamono.Application.Abstractions;
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
    IPermissionGuard permissionGuard)
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

        await repository.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
