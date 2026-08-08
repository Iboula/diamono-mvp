namespace Diamono.Domain.Settings;

public sealed class StadiumBookingSettings
{
    public static readonly Guid SingletonId = Guid.Parse("2df08bf4-26d9-4f65-9e40-0b9b62c9a001");

    private StadiumBookingSettings() { }

    public StadiumBookingSettings(
        TimeOnly opensAt,
        TimeOnly closesAt,
        int minimumDurationHours,
        int maximumDurationHours,
        decimal standardHourlyRate,
        decimal localAscHourlyRate,
        decimal lightingHourlyRate,
        TimeOnly lightingStartsAt,
        decimal depositAmount,
        int maximumAdvanceBookingDays,
        int paymentDeadlineHours,
        bool approvalRequired)
    {
        Id = SingletonId;
        Update(
            opensAt,
            closesAt,
            minimumDurationHours,
            maximumDurationHours,
            standardHourlyRate,
            localAscHourlyRate,
            lightingHourlyRate,
            lightingStartsAt,
            depositAmount,
            maximumAdvanceBookingDays,
            paymentDeadlineHours,
            approvalRequired);
    }

    public Guid Id { get; private set; }
    public TimeOnly OpensAt { get; private set; }
    public TimeOnly ClosesAt { get; private set; }
    public int MinimumDurationHours { get; private set; }
    public int MaximumDurationHours { get; private set; }
    public decimal StandardHourlyRate { get; private set; }
    public decimal LocalAscHourlyRate { get; private set; }
    public decimal LightingHourlyRate { get; private set; }
    public TimeOnly LightingStartsAt { get; private set; }
    public decimal DepositAmount { get; private set; }
    public int MaximumAdvanceBookingDays { get; private set; }
    public int PaymentDeadlineHours { get; private set; }
    public bool ApprovalRequired { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static StadiumBookingSettings MvpDefaults()
        => new(
            new TimeOnly(8, 0),
            new TimeOnly(23, 0),
            minimumDurationHours: 2,
            maximumDurationHours: 6,
            standardHourlyRate: 25_000m,
            localAscHourlyRate: 15_000m,
            lightingHourlyRate: 5_000m,
            lightingStartsAt: new TimeOnly(19, 0),
            depositAmount: 25_000m,
            maximumAdvanceBookingDays: 60,
            paymentDeadlineHours: 24,
            approvalRequired: true);

    public void Update(
        TimeOnly opensAt,
        TimeOnly closesAt,
        int minimumDurationHours,
        int maximumDurationHours,
        decimal standardHourlyRate,
        decimal localAscHourlyRate,
        decimal lightingHourlyRate,
        TimeOnly lightingStartsAt,
        decimal depositAmount,
        int maximumAdvanceBookingDays,
        int paymentDeadlineHours,
        bool approvalRequired,
        string? updatedBy = null)
    {
        Validate(
            opensAt,
            closesAt,
            minimumDurationHours,
            maximumDurationHours,
            standardHourlyRate,
            localAscHourlyRate,
            lightingHourlyRate,
            depositAmount,
            maximumAdvanceBookingDays,
            paymentDeadlineHours);

        OpensAt = opensAt;
        ClosesAt = closesAt;
        MinimumDurationHours = minimumDurationHours;
        MaximumDurationHours = maximumDurationHours;
        StandardHourlyRate = standardHourlyRate;
        LocalAscHourlyRate = localAscHourlyRate;
        LightingHourlyRate = lightingHourlyRate;
        LightingStartsAt = lightingStartsAt;
        DepositAmount = depositAmount;
        MaximumAdvanceBookingDays = maximumAdvanceBookingDays;
        PaymentDeadlineHours = paymentDeadlineHours;
        ApprovalRequired = approvalRequired;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? null : updatedBy.Trim();
    }

    public static void Validate(
        TimeOnly opensAt,
        TimeOnly closesAt,
        int minimumDurationHours,
        int maximumDurationHours,
        decimal standardHourlyRate,
        decimal localAscHourlyRate,
        decimal lightingHourlyRate,
        decimal depositAmount,
        int maximumAdvanceBookingDays,
        int paymentDeadlineHours)
    {
        if (opensAt >= closesAt) throw new ArgumentException("L'heure d'ouverture doit etre avant l'heure de fermeture.");
        if (minimumDurationHours <= 0) throw new ArgumentOutOfRangeException(nameof(minimumDurationHours), "La duree minimale doit etre positive.");
        if (maximumDurationHours < minimumDurationHours) throw new ArgumentOutOfRangeException(nameof(maximumDurationHours), "La duree maximale doit etre superieure ou egale a la duree minimale.");
        if (standardHourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(standardHourlyRate), "Le tarif standard doit etre positif ou nul.");
        if (localAscHourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(localAscHourlyRate), "Le tarif ASC doit etre positif ou nul.");
        if (lightingHourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(lightingHourlyRate), "Le tarif eclairage doit etre positif ou nul.");
        if (depositAmount < 0) throw new ArgumentOutOfRangeException(nameof(depositAmount), "La caution doit etre positive ou nulle.");
        if (maximumAdvanceBookingDays <= 0) throw new ArgumentOutOfRangeException(nameof(maximumAdvanceBookingDays), "Le delai de reservation anticipee doit etre positif.");
        if (paymentDeadlineHours <= 0) throw new ArgumentOutOfRangeException(nameof(paymentDeadlineHours), "Le delai de paiement doit etre positif.");
    }
}
