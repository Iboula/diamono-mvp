namespace Diamono.Application.Administration;

public sealed record UserReadModel(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsEnabled,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastLoginAt);
