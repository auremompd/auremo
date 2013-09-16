/*
 * Copyright 2013 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

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
