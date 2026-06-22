namespace Twitch.Rx.Api.Models;

public sealed record User(
    string Id, string Login, string DisplayName,
    string BroadcasterType, string Description,
    string ProfileImageUrl, string CreatedAt);
