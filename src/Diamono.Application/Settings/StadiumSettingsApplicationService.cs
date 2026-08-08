using Diamono.Application.Abstractions;
using Diamono.Domain.Settings;

namespace Diamono.Application.Settings;

public sealed class StadiumSettingsApplicationService(IStadiumBookingSettingsRepository repository)
{
    public async Task<StadiumBookingSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        => await repository.GetAsync(cancellationToken);

    public async Task<StadiumBookingSettings> UpdateSettingsAsync(
        UpdateStadiumBookingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
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
