using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class MPDSongResponseBlock
    {
        public MPDSongResponseBlock(string file)
        {
            File = file;
            Album = "Unknown Album";
            Artist = "Unknown Artist";
            Date = null;
            Genre = "No Genre";
            Id = -1;
            Pos = -1; 
            Time = null;
            Title = null;
            Track = -1;
        }

        public string File
        {
            get;
            set;
        }

        public string Album
        {
            get;
            set;
        }

        public string Artist
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Genre
        {
            get;
            set;
        }

        public int Id
        {
            get;
            set;
        }

        public int Pos
        {
            get;
            set;
        }

        public int? Time
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public int Track
        {
            get;
            set;
        }
    }
}
