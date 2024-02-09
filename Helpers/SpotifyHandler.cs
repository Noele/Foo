using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using DisCatSharp.Lavalink.Enums;
using SpotifyAPI.Web;

namespace Foo.Helpers;

public class SpotifyHandler
{
    public static SpotifyClient SpotifyClient;
    
    
    /// <summary>
    /// Gets a spotify songs information
    /// </summary>
    /// <param name="query">A Spotify url</param>
    /// <returns>The spotify songs information</returns>
    public static List<string>? GetSpotifySongInfo(string query)
    {
        var parsedUrl = ParseUrl(query);
        if (parsedUrl == null)
        {
            return null;
        }
        
        var list = SpotifyClient.Playlists.GetItems(parsedUrl, new PlaylistGetItemsRequest { Offset = 0 });
        var result = list.Result;
        if (result.Items == null) return null;
        var tracklist = new List<PlaylistTrack<IPlayableItem>>();
        while (!(result.Items.Count == 0))
        {

            foreach(var track in result.Items)
            {
                tracklist.Add(track);
            }
            result.Offset += 100;
            list = SpotifyClient.Playlists.GetItems(parsedUrl, new PlaylistGetItemsRequest { Offset = result.Offset });
            result = list.Result;
        }
        var queueableList = new List<string>();
        foreach (var item in tracklist)
        {
            if (item.Track is FullTrack track)
            {
                var artist = track.Artists.Count == 0 ? "" : track.Artists[0].Name;
                queueableList.Add($"{artist} {track.Name}");
            }
        }
        return queueableList;
    }
    
      
    /// <summary>
    /// Gets the spotify id from a playlist url
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string? ParseUrl(string url)
    {
        if (url.StartsWith("https://open.spotify.com"))
        {
            url = url.Replace("https://open.spotify.com/playlist/", "");
            url = url.Replace("https://open.spotify.com/track/", "");
            if (url.Contains("?si="))
            {
                var index = url.IndexOf("?si", StringComparison.Ordinal);
                url = url[..index];
            }

            return url;
        }

        return null;
    } 
    
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="connection"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public static async Task<string> ProcessSpotifyQuery(InteractionContext ctx, LavalinkGuildPlayer connection, string query)
    {
        var tracks = GetSpotifySongInfo(query);
        if(tracks == null) return $"No playlist found for {query}";

        foreach (var track in tracks)
        {
            var searchResult = connection.LoadTracksAsync(LavalinkSearchType.Youtube, track).Result.GetResultAs<List<LavalinkTrack>>()[0];
            if(searchResult == null) continue;
            if (connection.CurrentTrack == null)
            {
                await connection.PlayAsync(searchResult);
            }
            else
            {
                MusicManager.QueueSong(ctx.GuildId.Value, searchResult);
            }
        }

        return $"Queued {tracks.Count} songs.";
    }
}