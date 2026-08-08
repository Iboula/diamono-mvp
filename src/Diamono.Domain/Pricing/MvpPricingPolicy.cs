using Diamono.Domain.Bookings;
using Diamono.Domain.Settings;

namespace Diamono.Domain.Pricing;

public sealed class MvpPricingPolicy
{
    public PriceQuote Quote(
        StadiumBookingSettings settings,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CustomerCategory category)
    {
        var hours = (decimal)(endsAt - startsAt).TotalHours;
        if (hours < settings.MinimumDurationHours || hours > settings.MaximumDurationHours)
            throw new ArgumentOutOfRangeException(nameof(endsAt), "Booking duration is outside configured limits.");

        var rate = category == CustomerCategory.LocalAsc
            ? settings.LocalAscHourlyRate
            : settings.StandardHourlyRate;
        var rental = hours * rate;
        var lightingHours = CalculateLightingHours(settings, startsAt, endsAt);
        var lighting = lightingHours * settings.LightingHourlyRate;
        return new PriceQuote(rental, lighting, settings.DepositAmount);
    }

    private static decimal CalculateLightingHours(
        StadiumBookingSettings settings,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        var lightingStart = new DateTimeOffset(startsAt.Date.Add(settings.LightingStartsAt.ToTimeSpan()), startsAt.Offset);
        var chargeFrom = startsAt > lightingStart ? startsAt : lightingStart;
        if (endsAt <= chargeFrom) return 0;
        return (decimal)(endsAt - chargeFrom).TotalHours;
    }
}

public readonly record struct PriceQuote(decimal RentalAmount, decimal LightingAmount, decimal DepositAmount)
{
    public decimal TotalAmount => RentalAmount + LightingAmount + DepositAmount;
}
