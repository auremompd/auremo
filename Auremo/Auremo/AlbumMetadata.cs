﻿/*
 * Copyright 2014 Mikko Teräs and Niilo Säämänen.
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
using System.Windows.Media;

namespace Auremo
{
    public class AlbumMetadata : IComparable, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private ImageSource m_Cover;

        public AlbumMetadata(string artist, string albumTitle, string date)
        {
            Artist = artist;
            Title = albumTitle;
            Date = date;
            Cover = null;
        }

        public string Artist
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Year
        {
            get
            {
                return Utils.ExtractYearFromDateString(Date);
            }
        }

        public ImageSource Cover
        {
            get
            {
                return m_Cover;
            }
            set
            {
                if (value != m_Cover)
                {
                    m_Cover = value;
                    NotifyPropertyChanged("Cover");
                }
            }
        }

        public override string ToString()
        {
            return Artist + ": " + Title;
        }

        public int CompareTo(object o)
        {
            if (o is AlbumMetadata)
            {
                AlbumMetadata rhs = o as AlbumMetadata;

                if (Artist != rhs.Artist)
                {
                    return StringComparer.Ordinal.Compare(Artist, rhs.Artist);
                }
                else
                {
                    return StringComparer.Ordinal.Compare(Title, rhs.Title);
                }
            }
            else
            {
                throw new Exception("SongMetadata: attempt to compare to an incompatible object");
            }
        }
    }
}
