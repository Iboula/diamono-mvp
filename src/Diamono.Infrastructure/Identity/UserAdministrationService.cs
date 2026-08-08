using System.Security.Cryptography;
using Diamono.Application.Abstractions;
using Diamono.Application.Administration;
using Diamono.Application.Audit;
using Diamono.Application.Security;
using Diamono.Domain.Audit;
using Diamono.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace Diamono.Infrastructure.Identity;

public sealed class UserAdministrationService(
    UserManager<ApplicationUser> userManager,
    IPermissionGuard permissionGuard,
    ICurrentUserAccessor currentUserAccessor,
    IAuditWriter auditWriter)
    : IUserAdministrationService
{
    private static readonly string[] FunctionalRoles =
    [
        DiamonoRoles.Gestionnaire,
        DiamonoRoles.Caissier,
        DiamonoRoles.Lecteur
    ];

    public async Task<IReadOnlyList<UserReadModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);

        var users = userManager.Users
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .ToList();

        var result = new List<UserReadModel>(users.Count);
        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(await ToReadModelAsync(user));
        }

        return result;
    }

    public async Task<UserReadModel> GetUserAsync(string id, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);
        return await ToReadModelAsync(await GetRequiredUserAsync(id));
    }

    public async Task<CreatedUserResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);
        EnsureFunctionalRole(request.Role);

        var email = NormalizeRequired(request.Email, "L'email est requis.");
        var displayName = NormalizeRequired(request.DisplayName, "Le nom est requis.");

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Un utilisateur existe deja avec cet email.");

        var temporaryPassword = GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            LockoutEnabled = true
        };

        await EnsureSucceededAsync(userManager.CreateAsync(user, temporaryPassword));
        await EnsureSucceededAsync(userManager.AddToRoleAsync(user, request.Role));
        await EnsureSucceededAsync(userManager.UpdateSecurityStampAsync(user));
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.UserCreated,
            "ApplicationUser",
            user.Id.ToString(),
            "Utilisateur cree.",
            NewValues: new
            {
                user.Email,
                user.DisplayName,
                role = request.Role,
                isEnabled = IsEnabled(user)
            }),
            cancellationToken);
        await auditWriter.SaveChangesAsync(cancellationToken);

        return new CreatedUserResult(await ToReadModelAsync(user), temporaryPassword);
    }

    public async Task UpdateUserRoleAsync(string id, string role, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);
        EnsureFunctionalRole(role);

        var user = await GetRequiredUserAsync(id);
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);
        var currentRoles = await userManager.GetRolesAsync(user);
        var oldRole = RoleFrom(currentRoles);

        if (currentRoles.Contains(DiamonoRoles.SuperAdmin, StringComparer.Ordinal))
        {
            if (IsCurrentUser(user, currentUser))
                throw new InvalidOperationException("Vous ne pouvez pas retirer votre propre role SuperAdmin.");

            await EnsureAnotherEnabledSuperAdminExistsAsync(user.Id);
        }

        var rolesToRemove = currentRoles
            .Where(r => FunctionalRoles.Contains(r, StringComparer.Ordinal) || r == DiamonoRoles.SuperAdmin)
            .ToArray();

        if (rolesToRemove.Length > 0)
            await EnsureSucceededAsync(userManager.RemoveFromRolesAsync(user, rolesToRemove));

        await EnsureSucceededAsync(userManager.AddToRoleAsync(user, role));
        await EnsureSucceededAsync(userManager.UpdateSecurityStampAsync(user));
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.UserRoleChanged,
            "ApplicationUser",
            user.Id.ToString(),
            "Role utilisateur modifie.",
            OldValues: new { role = oldRole },
            NewValues: new { role },
            Metadata: new { user.Email, user.DisplayName }),
            cancellationToken);
        await auditWriter.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableUserAsync(string id, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);

        var user = await GetRequiredUserAsync(id);
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);
        var wasEnabled = IsEnabled(user);

        if (await userManager.IsInRoleAsync(user, DiamonoRoles.SuperAdmin))
        {
            if (IsCurrentUser(user, currentUser))
                throw new InvalidOperationException("Vous ne pouvez pas desactiver votre propre compte SuperAdmin.");

            await EnsureAnotherEnabledSuperAdminExistsAsync(user.Id);
        }

        await EnsureSucceededAsync(userManager.SetLockoutEnabledAsync(user, true));
        await EnsureSucceededAsync(userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100)));
        await EnsureSucceededAsync(userManager.UpdateSecurityStampAsync(user));
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.UserDisabled,
            "ApplicationUser",
            user.Id.ToString(),
            "Utilisateur desactive.",
            OldValues: new { isEnabled = wasEnabled },
            NewValues: new { isEnabled = IsEnabled(user) },
            Metadata: new { user.Email, user.DisplayName }),
            cancellationToken);
        await auditWriter.SaveChangesAsync(cancellationToken);
    }

    public async Task EnableUserAsync(string id, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);

        var user = await GetRequiredUserAsync(id);
        var wasEnabled = IsEnabled(user);
        await EnsureSucceededAsync(userManager.SetLockoutEnabledAsync(user, true));
        await EnsureSucceededAsync(userManager.SetLockoutEndDateAsync(user, null));
        await EnsureSucceededAsync(userManager.UpdateSecurityStampAsync(user));
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.UserEnabled,
            "ApplicationUser",
            user.Id.ToString(),
            "Utilisateur reactive.",
            OldValues: new { isEnabled = wasEnabled },
            NewValues: new { isEnabled = IsEnabled(user) },
            Metadata: new { user.Email, user.DisplayName }),
            cancellationToken);
        await auditWriter.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> GetRequiredUserAsync(string id)
    {
        if (!Guid.TryParse(id, out var userId))
            throw new KeyNotFoundException("Utilisateur introuvable.");

        return await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");
    }

    private async Task<CurrentUser> GetRequiredCurrentUserAsync(CancellationToken cancellationToken)
        => await currentUserAccessor.GetCurrentUserAsync(cancellationToken)
            ?? throw new InvalidOperationException("Operateur courant introuvable.");

    private async Task<UserReadModel> ToReadModelAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = RoleFrom(roles);

        return new UserReadModel(
            user.Id.ToString(),
            user.Email ?? string.Empty,
            string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? string.Empty : user.DisplayName,
            role,
            IsEnabled(user),
            CreatedAt: null,
            LastLoginAt: null);
    }

    private async Task EnsureAnotherEnabledSuperAdminExistsAsync(Guid excludedUserId)
    {
        var superAdmins = await userManager.GetUsersInRoleAsync(DiamonoRoles.SuperAdmin);
        if (!superAdmins.Any(user => user.Id != excludedUserId && IsEnabled(user)))
            throw new InvalidOperationException("Le dernier SuperAdmin actif doit etre conserve.");
    }

    private static string RoleFrom(IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        return roleList.Contains(DiamonoRoles.SuperAdmin, StringComparer.Ordinal)
            ? DiamonoRoles.SuperAdmin
            : roleList.FirstOrDefault(r => FunctionalRoles.Contains(r, StringComparer.Ordinal)) ?? string.Empty;
    }

    private static bool IsEnabled(ApplicationUser user)
        => user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow;

    private static bool IsCurrentUser(ApplicationUser user, CurrentUser currentUser)
        => string.Equals(user.Id.ToString(), currentUser.Id, StringComparison.OrdinalIgnoreCase);

    private static void EnsureFunctionalRole(string role)
    {
        if (!FunctionalRoles.Contains(role, StringComparer.Ordinal))
            throw new InvalidOperationException("Role non autorise pour l'administration MVP.");
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }

    private static async Task EnsureSucceededAsync(Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private static string GenerateTemporaryPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@$?_-";
        const string all = lower + upper + digits + symbols;

        var chars = new List<char>
        {
            RandomChar(upper),
            RandomChar(lower),
            RandomChar(digits),
            RandomChar(symbols)
        };

        while (chars.Count < 18)
            chars.Add(RandomChar(all));

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private static char RandomChar(string source)
        => source[RandomNumberGenerator.GetInt32(source.Length)];
}
