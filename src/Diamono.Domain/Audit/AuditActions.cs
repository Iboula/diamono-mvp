namespace Diamono.Domain.Audit;

public static class AuditActions
{
    public const string BookingApproved = "BookingApproved";
    public const string BookingRejected = "BookingRejected";
    public const string BookingMarkedPaid = "BookingMarkedPaid";
    public const string BookingCancelled = "BookingCancelled";
    public const string BookingRequestReceiptGenerated = "BookingRequestReceiptGenerated";
    public const string PaymentCreated = "PaymentCreated";
    public const string PaymentPaid = "PaymentPaid";
    public const string PaymentFailed = "PaymentFailed";
    public const string PaymentReceiptGenerated = "PaymentReceiptGenerated";
    public const string BookingBlockCreated = "BookingBlockCreated";
    public const string BookingBlockCancelled = "BookingBlockCancelled";
    public const string SettingsUpdated = "SettingsUpdated";
    public const string UserCreated = "UserCreated";
    public const string UserRoleChanged = "UserRoleChanged";
    public const string UserDisabled = "UserDisabled";
    public const string UserEnabled = "UserEnabled";
}
