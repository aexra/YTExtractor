using System.Collections.Generic;

namespace YTExtractor
{
    internal class PlaylistData
    {
        public List<string> Ids;
        public string Title;
        public string Description;
        public string Thumbnail;
        public string ChannelTitle;
        public string ChannelThumbnail;
        public string ChannelId;
        public int Count;

        public PlaylistData()
        {
            Ids = new List<string>();
        }
    }
}