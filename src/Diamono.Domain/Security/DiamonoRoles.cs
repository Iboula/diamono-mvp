namespace Diamono.Domain.Security;

/// <summary>Roles du backoffice MVP.</summary>
public static class DiamonoRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Gestionnaire = "Gestionnaire";
    public const string Caissier = "Caissier";
    public const string Lecteur = "Lecteur";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        Gestionnaire,
        Caissier,
        Lecteur
    ];
}
