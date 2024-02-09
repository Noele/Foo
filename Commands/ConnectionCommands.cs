using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using Foo.Helpers;

namespace Foo.Commands;

/// <summary>
/// Commands to connect and disconnect to the voice channel.
/// </summary>
public class ConnectionCommands : ApplicationCommandsModule
{
	/// <summary>
	/// Connect to the voice channel.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("connect", "Join the voice channel")]
	public static async Task ConnectAsync(InteractionContext ctx)
	{
		var lava = ctx.Client.GetLavalink();

		// Check if the user is currently connected to the voice channel
		if (ctx.Member.VoiceState == null)
		{
			await Helpers.Foo.Send(ctx, "You must be connected to a voice channel to use this command!");
			return;
		}

		// Check if Lavalink connection is established
		if (!lava.ConnectedSessions.Any())
		{
			await Helpers.Foo.Send(ctx, "The Lavalink connection is not established!");
			return;
		}

		var node = lava.ConnectedSessions.Values.First();

		// Connect to the channel
		await ctx.Member.VoiceState.Channel.ConnectAsync(node);
		var connection = node.GetGuildPlayer(ctx.Member.VoiceState.Guild);
		connection.TrackEnded += MusicManager.TrackEnded;
		
		await Helpers.Foo.Send(ctx, $"The bot has joined the channel {ctx.Member.VoiceState.Channel.Name.InlineCode()}");
	}
	
	/// <summary>
	/// Disconnect from the voice channel.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("leave", "Leave the voice channel")]
	public static async Task LeaveAsync(InteractionContext ctx)
	{
		var lava = ctx.Client.GetLavalink();
		if (!lava.ConnectedSessions.Any())
		{
			await Helpers.Foo.Send(ctx, "The Lavalink connection is not established!");
			return;
		}

		var node = lava.ConnectedSessions.Values.First();

		// Get the current Lavalink connection in the guild.
		var connection = node.GetGuildPlayer(ctx.Guild);

		if (connection == null)
		{
			await Helpers.Foo.Send(ctx, "The bot is not connected to the voice channel in this guild!");
			return;
		}

		await connection.DisconnectAsync();
		
		await Helpers.Foo.Send(ctx, "The bot left the voice channel");
	}
}
