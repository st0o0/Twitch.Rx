using Microsoft.Extensions.Options;

namespace Twitch.Rx;

internal sealed class TwitchRxOptionsValidator : IValidateOptions<TwitchRxOptions>
{
    public ValidateOptionsResult Validate(string? name, TwitchRxOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            return ValidateOptionsResult.Fail("ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return ValidateOptionsResult.Fail("ClientSecret is required.");
        }

        return ValidateOptionsResult.Success;
    }
}