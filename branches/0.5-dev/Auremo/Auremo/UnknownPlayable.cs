using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class UnknownPlayable : Playable
    {
        private UnknownPlayable()
        {
        }

        public UnknownPlayable(string path)
        {
            Path = path;
            Title = Utils.SplitPath(path).Item2;
        }

        public string Path
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public string Artist
        {
            get
            {
                return null;
            }
        }

        public string Album
        {
            get
            {
                return null;
            }
        }

        public int? Year
        {
            get
            {
                return null;
            }
        }
    }
}
