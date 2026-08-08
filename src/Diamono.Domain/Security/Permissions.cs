namespace Diamono.Domain.Security;

/// <summary>
/// Permissions atomiques du backoffice. Ce sont ces valeurs, et non les roles,
/// qui sont evaluees par les policies d'autorisation et par les cas d'usage.
/// Garder les roles hors des tests conditionnels permet de remplacer le
/// fournisseur d'identite (Identity aujourd'hui, OIDC demain) sans toucher
/// aux regles d'acces.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "diamono.permission";

    public const string BookingsView = "Bookings.View";
    public const string BookingsApprove = "Bookings.Approve";
    public const string BookingsReject = "Bookings.Reject";

    public const string PaymentsMarkPaid = "Payments.MarkPaid";

    public const string BookingBlocksView = "BookingBlocks.View";
    public const string BookingBlocksManage = "BookingBlocks.Manage";

    public const string SettingsView = "Settings.View";
    public const string SettingsManage = "Settings.Manage";

    /// <summary>
    /// Reservee a l'administration des comptes et des roles. Aucun ecran ne la
    /// consomme dans DIA-016B : la policy est declaree pour que la gestion des
    /// utilisateurs (DIA-017+) s'y branche sans redefinir la matrice.
    /// </summary>
    public const string AdministrationManage = "Administration.Manage";

    public static readonly IReadOnlyList<string> All =
    [
        BookingsView,
        BookingsApprove,
        BookingsReject,
        PaymentsMarkPaid,
        BookingBlocksView,
        BookingBlocksManage,
        SettingsView,
        SettingsManage,
        AdministrationManage
    ];
}
