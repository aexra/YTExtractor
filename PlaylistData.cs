using System.Collections.Generic;

namespace YTExtractor
{
    internal class PlaylistData
    {
        public List<string> ids;
        public string title;
        public string description;
        public string thumbnail;
        public string channelTitle;
        public string channelThumbnail;
        public string channelId;
        public int n;

        public PlaylistData()
        {
            ids = new List<string>();
        }
    }
}