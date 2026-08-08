using Diamono.Application.Security;

namespace Diamono.Application.Abstractions;

/// <summary>Acces a l'identite de l'operateur courant (preparation audit DIA-017).</summary>
public interface ICurrentUserAccessor
{
    /// <summary>Retourne null si la requete est anonyme (site public).</summary>
    Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
