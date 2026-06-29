namespace Twitch.Rx.Helix;

public sealed record Page<T>(IReadOnlyList<T> Items, string? Cursor)
{
    public bool HasMore => Cursor is not null;
}
