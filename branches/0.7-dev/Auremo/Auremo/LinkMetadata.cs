using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class LinkMetadata : Playable
    {
        public LinkMetadata()
        {
        }

        public string Path
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Artist
        {
            get
            {
                return "";
            }
        }

        public string Album
        {
            get
            {
                return "";
            }
        }

        public bool IsLocal
        {
            get
            {
                return Path.StartsWith("local:");
            }
        }

        public bool IsSpotify
        {
            get
            {
                return Path.StartsWith("spotify:");
            }
        }

        public bool IsSoundCloud
        {
            get
            {
                return false;
            }
        }

        public string DisplayName
        {
            get
            {
                return Title;
            }
        }
    }
}
