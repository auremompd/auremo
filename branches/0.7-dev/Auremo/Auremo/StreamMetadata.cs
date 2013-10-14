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

        private string m_Path = null;
        private string m_Name = null;

        public StreamMetadata(string path, string name)
        {
            Path = path;
            Name = name;
        }

        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                if (value != m_Path)
                {
                    m_Path = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                if (value != m_Name)
                {
                    m_Name = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        public string Title
        {
            get
            {
                return Name == null ? Path : Name;
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
