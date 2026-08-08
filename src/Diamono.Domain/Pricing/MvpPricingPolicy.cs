using Diamono.Domain.Bookings;

namespace Diamono.Domain.Pricing;

public sealed class MvpPricingPolicy
{
    public const decimal StandardHourlyRate = 25_000m;
    public const decimal LocalAscHourlyRate = 15_000m;
    public const decimal LightingHourlyRate = 5_000m;
    public const decimal Deposit = 25_000m;
    public static readonly TimeOnly LightingStartsAt = new(19, 0);

    public PriceQuote Quote(DateTimeOffset startsAt, DateTimeOffset endsAt, CustomerCategory category)
    {
        var hours = (decimal)(endsAt - startsAt).TotalHours;
        if (hours < 2 || hours > 6) throw new ArgumentOutOfRangeException(nameof(endsAt), "MVP bookings must be between 2 and 6 hours.");

        var rate = category == CustomerCategory.LocalAsc ? LocalAscHourlyRate : StandardHourlyRate;
        var rental = hours * rate;
        var lightingHours = CalculateLightingHours(startsAt, endsAt);
        var lighting = lightingHours * LightingHourlyRate;
        return new PriceQuote(rental, lighting, Deposit);
    }

    private static decimal CalculateLightingHours(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        var lightingStart = new DateTimeOffset(startsAt.Date.Add(LightingStartsAt.ToTimeSpan()), startsAt.Offset);
        var chargeFrom = startsAt > lightingStart ? startsAt : lightingStart;
        if (endsAt <= chargeFrom) return 0;
        return (decimal)(endsAt - chargeFrom).TotalHours;
    }
}

public readonly record struct PriceQuote(decimal RentalAmount, decimal LightingAmount, decimal DepositAmount)
{
    public decimal TotalAmount => RentalAmount + LightingAmount + DepositAmount;
}
