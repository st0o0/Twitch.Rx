namespace Twitch.Rx.Api.Models;

public sealed record TwitchStream(
    string Id, string UserId, string UserLogin, string UserName,
    string GameId, string GameName, string Type, string Title,
    int ViewerCount, string StartedAt, string Language,
    string ThumbnailUrl, bool IsMature);
