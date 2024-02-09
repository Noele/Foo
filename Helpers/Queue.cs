using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;

namespace Foo.Helpers;

public class Queue
{
    public List<LavalinkTrack> Tracks;
    
    /// <summary>
    /// Init
    /// </summary>
    /// <param name="track">The initial track to add to the queue.</param>
    public Queue(LavalinkTrack track)
    {
        this.Tracks = new List<LavalinkTrack> { track };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tracks"></param>
    public Queue(List<LavalinkTrack> tracks)
    {
        this.Tracks = tracks;
    }
}