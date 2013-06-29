using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class StreamMetadata : Playable, IComparable
    {
        public StreamMetadata()
        {
            Path = null;
            Title = null;
        }

        public string Path
        {
            get;
            set;
        }

        private string m_Title = null;

        public string Title
        {
            get
            {
                if (m_Title == null)
                {
                    return Path;
                }
                else
                {
                    return m_Title;
                }
            }
            set
            {
                m_Title = value;
            }
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

        public int CompareTo(object o)
        {
            if (o is StreamMetadata)
            {
                StreamMetadata rhs = (StreamMetadata)o;
                return StringComparer.Ordinal.Compare(Path, rhs.Path);
            }
            else if (o is SongMetadata)
            {
                return 1;
            }
            else
            {
                throw new Exception("StreamMetadata: attempt to compare to an incompatible object");
            }
        }
    }
}
