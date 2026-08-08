namespace Diamono.Application.Settings;

public sealed record UpdateStadiumBookingSettingsRequest(
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    int MinimumDurationHours,
    int MaximumDurationHours,
    decimal StandardHourlyRate,
    decimal LocalAscHourlyRate,
    decimal LightingHourlyRate,
    TimeOnly LightingStartsAt,
    decimal DepositAmount,
    int MaximumAdvanceBookingDays,
    int PaymentDeadlineHours,
    bool ApprovalRequired,
    string? UpdatedBy = null);
