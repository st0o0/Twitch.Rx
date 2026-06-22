namespace Twitch.Rx.EventSub.Events;

public sealed record EventSubError(string Message, Exception? Exception = null);
