namespace Diamono.Application.Payments;

public sealed record PaymentProviderResult(bool Success, string? ProviderTransactionId = null, string? Error = null)
{
    public static PaymentProviderResult Paid(string? providerTransactionId = null) => new(true, providerTransactionId);
    public static PaymentProviderResult Failed(string error, string? providerTransactionId = null) => new(false, providerTransactionId, error);
}
