namespace Diamono.Application.Security;

/// <summary>
/// Levee par un cas d'usage lorsque l'utilisateur courant ne detient pas la
/// permission requise. Masquer un bouton ne suffit pas : le refus doit venir
/// du cas d'usage lui-meme.
/// </summary>
public sealed class PermissionDeniedException(string permission)
    : Exception($"Permission requise : {permission}.")
{
    public string Permission { get; } = permission;
}
