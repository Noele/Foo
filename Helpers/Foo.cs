using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace Foo.Helpers;

public class Foo
{
    public static async Task Send(InteractionContext ctx, string message)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder
            {
                Content = message
            });
    }
}