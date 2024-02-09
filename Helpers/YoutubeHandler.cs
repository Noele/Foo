using System.Text.RegularExpressions;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Common;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Foo.Helpers;

public class YoutubeHandler
{
    public static YouTubeService youtubeService;

    public static async Task<string> ProcessYoutubePlaylist(InteractionContext ctx, LavalinkGuildPlayer? connection, string query)
    {
        var search = await connection.LoadTracksAsync(query);
        var tracks = search.GetResultAs<LavalinkPlaylist>().Tracks;

        if (tracks.Empty())
        {
            return $"Nothing found for {query}";
        }

        if (tracks.Count < 3)
        {
            return "Playlist too small, 3+ songs required.";
        }

        if (connection.CurrentTrack != null)
        {
            foreach (var track in tracks)
            {
                MusicManager.QueueSong(ctx.GuildId!.Value, track);
            }

            return $"Queued {tracks.Count} songs.";
        }
     
        await connection.PlayAsync(tracks[0]);
        tracks.RemoveAt(0);
        foreach (var track in tracks)
        {
            MusicManager.QueueSong(ctx.GuildId!.Value, track);
        }
        return $"Queued {tracks.Count + 1} songs.";
    
    }
    
    public static async Task<string> ProcessYoutubeSearch(InteractionContext ctx, LavalinkGuildPlayer? connection, string query)
    {
        LavalinkTrack track;
        var url = ExtractVideoID(query);
        if (url != null)
        {
            var tracks = await connection.LoadTracksAsync(url);
            
            if (tracks.LoadType is LavalinkLoadResultType.Error or LavalinkLoadResultType.Empty)
            {
                return $"Track search failed for `{query}`.";
            }
            
            track = tracks.GetResultAs<LavalinkTrack>();
        }
        else
        {
            var loadingResult = await connection.LoadTracksAsync(LavalinkSearchType.Youtube, query);
            if (loadingResult.LoadType is LavalinkLoadResultType.Error or LavalinkLoadResultType.Empty)
            {
                return $"Track search failed for `{query}`.";
            }
            track = loadingResult.GetResultAs<List<LavalinkTrack>>().First();
        }
        
        if (connection.CurrentTrack == null)
        {
            await connection.PlayAsync(track.Info.Identifier);
            return $"Playing {track.Info.Title}.";
        }
        else
        {
            MusicManager.QueueSong(ctx.GuildId!.Value, track);
            return $"Added {track.Info.Title} to the queue";
        }
        
    }
    
    
    
    /// <summary>
    /// Gets a youtube videos information
    /// </summary>
    /// <param name="query">The youtube video url</param>
    /// <returns>The youtube videos information, search terms being returned as a SearchResult?, and urls being returned  as a Video?</returns>
    public static Video? GetYoutubeVideoInfo(string url)
    {
        var videoId = ExtractVideoID(url);
        if (videoId != null)
        {
            var searchListRequest = youtubeService.Videos.List("snippet, statistics");
            searchListRequest.Id = videoId;
            searchListRequest.MaxResults = 1;
            var searchListResponse = searchListRequest.Execute();

            if (searchListResponse?.Items?.Any() == true)
            {
                return searchListResponse.Items[0];
            }
        }
        return null;
    }
    
    /// <summary>
    /// Gets the video id from a youtube url
    /// https://www.youtube.com/watch?v=dQw4w9WgXcQ => dQw4w9WgXcQ
    /// </summary>
    /// <param name="url">The url to get the id from</param>
    /// <returns>The youtube id</returns>
    private static string? ExtractVideoID(String url)
    {
        // Define the regex pattern
        var pattern = @"^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=|\?v=)([^#\&\?]*).*";

        // Match the regex pattern
        var match = Regex.Match(url, pattern);

        // Check if there's a match
        if (match.Success)
        {
            return match.Groups[2].Value;
        }

        // If no match, return null
        return null;
    }
    
}