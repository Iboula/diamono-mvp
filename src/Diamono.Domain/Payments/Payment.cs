namespace Diamono.Domain.Payments;

public sealed class Payment
{
    public const string MvpCurrency = "XOF";
    public const string ManualProvider = "Manual";

    private Payment() { }

    public Payment(
        Guid bookingId,
        decimal amount,
        PaymentMethod method,
        string provider = ManualProvider,
        string currency = MvpCurrency)
    {
        if (bookingId == Guid.Empty) throw new ArgumentException("Booking id is required.", nameof(bookingId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Le montant du paiement doit etre positif.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));

        Id = Guid.NewGuid();
        BookingId = bookingId;
        Reference = $"PAY-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}";
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Method = method;
        Provider = provider.Trim();
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = MvpCurrency;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string Provider { get; private set; } = ManualProvider;
    public string? ProviderTransactionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }

    public void MarkPaid(string? providerTransactionId = null)
    {
        EnsureStatus(PaymentStatus.Pending, "Seul un paiement en attente peut etre marque paye.");

        Status = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId) ? null : providerTransactionId.Trim();
    }

    public void MarkFailed(string? providerTransactionId = null)
    {
        EnsureStatus(PaymentStatus.Pending, "Seul un paiement en attente peut etre marque en echec.");

        Status = PaymentStatus.Failed;
        FailedAt = DateTimeOffset.UtcNow;
        ProviderTransactionId = string.IsNullOrWhiteSpace(providerTransactionId) ? null : providerTransactionId.Trim();
    }

    private void EnsureStatus(PaymentStatus expected, string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
    }
}
