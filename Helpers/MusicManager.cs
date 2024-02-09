using System.Text.RegularExpressions;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Common;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;
using DisCatSharp.Lavalink.EventArgs;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI;
using SpotifyAPI.Web;

namespace Foo.Helpers;

public static class MusicManager
{
    // Discord guild id, Queue
    private static Dictionary<ulong, Queue> queues = new Dictionary<ulong, Queue>();

    public static bool QueueExists(ulong guildID)
    {
        return queues.ContainsKey(guildID);
    }
    
    /// <summary>
    /// Adds a song to the queue
    /// </summary>
    /// <param name="guildID">The queues guild id</param>
    public static void QueueSong(ulong guildID, LavalinkTrack track)
    {
        if (queues.ContainsKey(guildID))
        {
            var tracks = queues[guildID].Tracks.Append(track);
            queues[guildID] = new Queue(tracks.ToList());
        }
        else
        {
            queues.Add(guildID, new Queue(track));
        }
    }

    /// <summary>
    /// Stops the currently playing song and replaces it with the next one in the queue, also deques that song
    /// </summary>
    /// <param name="guildID">The guild id of the song to skip</param>
    /// <param name="player">The lavalink player of the guild</param>
    public static void Skip(ulong guildID, LavalinkGuildPlayer player)
    {
        if (queues.ContainsKey(guildID) && queues[guildID].Tracks.Count > 0)
        {
            var track = Dequeue(guildID);
            player.PlayAsync(track);
        }
        else
        {
            player.StopAsync();
        }
    }

    /// <summary>
    /// Dequeues the next song
    /// </summary>
    /// <param name="guildID">The queues guild id</param>
    /// <returns>The dequeued song</returns>
    public static LavalinkTrack Dequeue(ulong guildID)
    {
        var track = queues[guildID].Tracks[0];
        queues[guildID].Tracks.RemoveAt(0);

        return track;
    }
    
    /// <summary>
    /// Removes a queue
    /// </summary>
    /// <param name="guildID">Guild ID of the queue we want to remove.</param>
    public static void ClearQueue(ulong guildID)
    {
        if (queues.ContainsKey(guildID))
        {
            queues.Remove(guildID);
        }
    }
    
    /// <summary>
    /// Event: Fired when a track ends.
    /// </summary>
    /// <param name="player">The player of the track that ended</param>
    /// <param name="args">The event args, contains info about the event such as the track that ended and the reason</param>
    public static Task TrackEnded(LavalinkGuildPlayer player, LavalinkTrackEndedEventArgs args)
    {
        if (args.Reason == LavalinkTrackEndReason.Replaced || args.Reason == LavalinkTrackEndReason.Stopped) return Task.CompletedTask;
        if(queues.ContainsKey(args.GuildId))
            if (!queues[args.GuildId].Tracks.Empty())
            {
                var nextTrack = Dequeue(args.GuildId);
                player.PlayAsync(nextTrack);
            }

        return Task.CompletedTask;
    }
  
    /// <summary>
    /// Sends an error message to the user
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="errorMessage"></param>
    public static async Task SendError(InteractionContext ctx, string errorMessage)
    {
        await ctx.FollowUpAsync(errorMessage);
    }
    
    /// <summary>
    /// Gets the guilds lavalink player
    /// </summary>
    /// <param name="ctx">Interaction context of the slash command</param>
    /// <returns>The guilds LavalinkGuildPlayer</returns>
    public static LavalinkGuildPlayer? GetLavalinkConnection(InteractionContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.Values.First();
        var connection = node.GetGuildPlayer(ctx.Member.VoiceState.Guild);
        return connection;
    }

    public static async Task Shuffle(ulong guildID)
    {
        queues[guildID].Tracks = Toolbox.Shuffle(queues[guildID].Tracks);
    }
    
    /// <summary>
    /// Validates if the bot is connected to the vc and the user is
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static async Task<bool> ValidateConnection(LavalinkGuildPlayer? connection, InteractionContext ctx)
    {
        if (connection == null)
        {
            await SendError(ctx, "The bot is not connected to the voice channel in this guild!");
            return false;
        }

        if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
        {
            await SendError(ctx, "You must be in the same voice channel as the bot!");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Returns the queue of the guild provided
    /// </summary>
    /// <param name="guildID">The guild id to return the guild of</param>
    /// <returns>The guild of the given id</returns>
    public static Queue GetQueue(ulong guildID)
    {
        return queues[guildID];
    }
    
}