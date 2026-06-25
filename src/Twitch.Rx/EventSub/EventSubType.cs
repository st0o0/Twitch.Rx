namespace Twitch.Rx.EventSub;

public static class EventSubType
{
    public const string ChannelFollow = "channel.follow";
    public const string StreamOnline = "stream.online";
    public const string StreamOffline = "stream.offline";
    public const string ChatMessage = "channel.chat.message";
    public const string ChannelSubscribe = "channel.subscribe";
    public const string ChannelRaid = "channel.raid";
    public const string ChannelPointsRedemption = "channel.channel_points_custom_reward_redemption.add";
    public const string ChannelPollBegin = "channel.poll.begin";
    public const string ChannelPollProgress = "channel.poll.progress";
    public const string ChannelPollEnd = "channel.poll.end";
}
