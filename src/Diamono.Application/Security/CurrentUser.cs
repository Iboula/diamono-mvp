namespace Diamono.Application.Security;

/// <summary>
/// Identite de l'operateur courant, exposee pour que DIA-017 puisse renseigner
/// ApprovedBy / RejectedBy / PaidBy / CreatedBy / CancelledBy.
/// DIA-016B se limite a rendre cette identite accessible : aucune colonne
/// d'audit n'est ecrite dans cette story.
/// </summary>
public sealed record CurrentUser(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
