using Diamono.Application.Security;

namespace Diamono.Application.Abstractions;

/// <summary>
/// Point de controle d'autorisation des cas d'usage. L'implementation Web
/// s'appuie sur les policies ASP.NET Core; l'abstraction reste ignorante du
/// fournisseur d'identite pour permettre un passage a OIDC sans reecrire les
/// services applicatifs.
/// </summary>
public interface IPermissionGuard
{
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>Leve <see cref="PermissionDeniedException"/> si la permission manque.</summary>
    Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default);
}
