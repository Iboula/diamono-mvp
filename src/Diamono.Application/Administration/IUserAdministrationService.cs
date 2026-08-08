namespace Diamono.Application.Administration;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserReadModel>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserReadModel> GetUserAsync(string id, CancellationToken cancellationToken = default);

    Task<CreatedUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task UpdateUserRoleAsync(string id, string role, CancellationToken cancellationToken = default);

    Task DisableUserAsync(string id, CancellationToken cancellationToken = default);

    Task EnableUserAsync(string id, CancellationToken cancellationToken = default);
}
