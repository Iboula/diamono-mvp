namespace Diamono.Application.Administration;

public sealed record CreatedUserResult(
    UserReadModel User,
    string TemporaryPassword);
