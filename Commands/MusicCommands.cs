using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Common;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;
using Foo.Helpers;

namespace Foo.Commands;

/// <summary>
/// Playback control with these commands.
/// </summary>
public class MusicCommands : ApplicationCommandsModule
{

	/// <summary>
	/// Play music asynchronously.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("skip", "Skips the currently playing song.")]
	public static async Task SkipAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		if (connection.CurrentTrack == null)
		{
			await Helpers.Foo.Send(ctx, "Nothing is playing !");
			return;
		}

		MusicManager.Skip(ctx.GuildId.Value, connection);

		await Helpers.Foo.Send(ctx, "Skipping ...");
	}
	
	/// <summary>
	/// Slash command for playing a song
	/// </summary>
	/// <param name="ctx"></param>
	/// <param name="query"></param>
	[SlashCommand("play", "Play music asynchronously"), ApplicationCommandRequireGuild]
	public static async Task PlayAsync(InteractionContext ctx, [Option("query", "Search string or Youtube link")] string query)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
	
	    var connection = MusicManager.GetLavalinkConnection(ctx);
        var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
        if (!validateConnection) return;

        string content = query switch
        {
	        string a when a.Contains("/playlist?list=") => await YoutubeHandler.ProcessYoutubePlaylist(ctx, connection, query),
	        
	        string a when a.Contains("spotify.com/playlist") => await SpotifyHandler.ProcessSpotifyQuery(ctx, connection, query),
	        
	        _ => await YoutubeHandler.ProcessYoutubeSearch(ctx, connection, query)
        };

        await ctx.FollowUpAsync(content);
	    
	}
	/// <summary>
	/// Shuffles the playlist
	/// </summary>
	/// <param name="ctx"></param>
	[SlashCommand("shuffle", "Shuffles the queue"), ApplicationCommandRequireGuild]
	public static async Task ShuffleAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);
		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		if (!MusicManager.QueueExists(ctx.GuildId.Value))
		{
			await Helpers.Foo.Send(ctx, "Nothing to shuffle.");
		}

		await MusicManager.Shuffle(ctx.GuildId.Value);
		await Helpers.Foo.Send(ctx, "Shuffled.");
	}
	
	/// <summary>
	/// Pause playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("pause", "Pause playback")]
	public static async Task PauseAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		// Pause playback
		await connection.PauseAsync();

		await Helpers.Foo.Send(ctx, "Paused!");
	}

	/// <summary>
	/// Resume playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("resume", "Resume playback")]
	public static async Task ResumeAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		// Resume playback
		await connection.ResumeAsync();

		await Helpers.Foo.Send(ctx, $"Now playing `{connection.CurrentTrack?.Info.Title}`");
	}

	/// <summary>
	/// Stop playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("stop", "Stop playback")]
	public static async Task StopAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		await connection.StopAsync();
		MusicManager.ClearQueue(ctx.GuildId.Value);
		await Helpers.Foo.Send(ctx, "Playback is stopped!");
	}


	/// <summary>
	/// Displays the current queue
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	/// <param name="pageQuery">Page of the queue to display</param>
	[SlashCommand("queue", "Displays the current queue")]
	public static async Task QueueAsync(InteractionContext ctx, [Option("pageQuery", "The page of the playlist")] string pageQuery)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;

		var trackNames = MusicManager.GetQueue(ctx.GuildId.Value).Tracks.Select(track => track.Info.Title).ToList();

		if (trackNames.Empty())
		{
			await Helpers.Foo.Send(ctx, "Nothing is in the queue.");
			return;
		}

		var (page, pageCount, pageNumber) = Toolbox.CreatePageFromList(trackNames, pageQuery, false, 700, true);

		var embed = new DiscordEmbedBuilder
		{
			Author = new DiscordEmbedBuilder.EmbedAuthor
			{
				Name = $"Now playing: {connection.CurrentTrack.Info.Title}"
			},
			Description = page,
			Footer = new DiscordEmbedBuilder.EmbedFooter
			{
				Text = $"Page {pageNumber}/{pageCount}"
			},
		}.Build();
		var dirp = new DiscordInteractionResponseBuilder();
		dirp.AddEmbed(embed);
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, dirp);

	}

	/// <summary>
	/// Displays the current song
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("nowplaying", "Displays the currently playing song")]
	public static async Task NowPlayingAsync(InteractionContext ctx)
	{
		var connection = MusicManager.GetLavalinkConnection(ctx);

		var validateConnection = await MusicManager.ValidateConnection(connection, ctx);
		if (!validateConnection) return;
		
		var videoResult = YoutubeHandler.GetYoutubeVideoInfo(connection.CurrentTrack.Info.Uri.AbsoluteUri);
		var nextIcon = ":no_entry_sign:";
		var nextText = "`Nothing next in queue`";

		if (MusicManager.QueueExists(ctx.GuildId.Value))
		{
			var queue = MusicManager.GetQueue(ctx.GuildId.Value);
			if (queue.Tracks.Count > 0)
			{
				nextIcon = ":arrow_right:";
				nextText = $"**Next Up:** {queue.Tracks[0].Info.Title} **by** {queue.Tracks[0].Info.Author}";
			}
		}

		var embed = new DiscordEmbedBuilder
		{
			Description = $"**Currently Playing**:\n" +
			              $"[{connection.CurrentTrack.Info.Title}]({connection.CurrentTrack.Info.Uri})({connection.CurrentTrack.Info.Length.ToString("mm\\:ss")})\n\n" +
			              $"**By**\n" +
			              $"**{connection.CurrentTrack.Info.Author}**\n\n" +
			              $"**Uploaded**\n" +
			              $":calendar: {videoResult.Snippet.PublishedAtDateTimeOffset.Value.ToString("dd/MM/yy")}\n\n" +
			              $":thumbs_up:**Likes** {String.Concat(Enumerable.Repeat("\u200b ", 4))} :eye:**Views**\n" +
			              $"{videoResult.Statistics.LikeCount} {String.Concat(Enumerable.Repeat("\u200b ", 12))} {videoResult.Statistics.ViewCount}\n\n" +
			              $"**Playback Position**\n" +
			              $":arrow_forward: {connection.Player.PlayerState.Position.ToString("mm\\:ss")} **[{GenerateProgressBar(connection.Player.PlayerState.Position, connection.CurrentTrack.Info.Length, 20)}]** {connection.CurrentTrack.Info.Length.ToString("mm\\:ss")}\n\n" +
			              $"{nextIcon} {nextText}",
			Color = DiscordColor.Black,
			Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
			{
				Url = connection.CurrentTrack.Info.ArtworkUrl.AbsoluteUri
			},
			
		}.Build();
		var dirp = new DiscordInteractionResponseBuilder();
		dirp.AddEmbed(embed);
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, dirp);
	}
	
	/// <summary>
	/// Generates a progress bar e.x [------|------]
	/// </summary>
	/// <param name="currentPosition">The TimeSpan of how far through we are of the song</param>
	/// <param name="totalDuration">The TimeSpan of the entire songs length</param>
	/// <param name="progressBarLength">How long the progress bar should be, ie, how many - there are</param>
	/// <returns>The generated progress bar</returns>
	static string GenerateProgressBar(TimeSpan currentPosition, TimeSpan totalDuration, int progressBarLength)
	{
		var progressPercentage = currentPosition.TotalSeconds / totalDuration.TotalSeconds;
		var progressLength = (int)(progressBarLength * progressPercentage);

		var fixedLength = progressBarLength - 1; // Account for the fixed length of dashes and the "|" symbol
		var progressBar = new string('-', progressLength) + "|" + new string('-', fixedLength - progressLength);

		return progressBar;
	}
}
