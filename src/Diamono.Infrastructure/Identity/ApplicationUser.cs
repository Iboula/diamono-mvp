using Microsoft.AspNetCore.Identity;

namespace Diamono.Infrastructure.Identity;

/// <summary>Compte operateur du backoffice.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Nom affiche dans le bandeau; retombe sur l'email si vide.</summary>
    public string? DisplayName { get; set; }
}
