using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class StreamMetadata : Playable, IComparable, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        public StreamMetadata(string path, string title)
        {
            Path = path;
            Title = title;
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
                if (m_Title != value)
                {
                    m_Title = value;
                    NotifyPropertyChanged("Title");
                }
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
                return StringComparer.Ordinal.Compare(Title, rhs.Title);
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
