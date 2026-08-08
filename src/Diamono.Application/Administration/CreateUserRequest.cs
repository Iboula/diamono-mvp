namespace Diamono.Application.Administration;

public sealed record CreateUserRequest(
    string DisplayName,
    string Email,
    string Role);
