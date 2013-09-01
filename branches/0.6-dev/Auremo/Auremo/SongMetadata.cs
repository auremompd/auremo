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
using System.Linq;
using System.Text;

namespace Auremo
{
    public class SongMetadata : Playable, IComparable
    {
        public SongMetadata()
        {
            Path = null;
            Length = null;
            Track = null;
            Date = null;

            Artist = "Unknown Artist";
            Genre = "No Genre";
            Album = "Unknown Album";
        }

        public string Path
        {
            get;
            set;
        }

        private string m_Title = "";

        public string Title
        {
            get
            {
                if (m_Title.Length > 0)
                {
                    return m_Title;
                }
                else
                {
                    return Utils.SplitPath(Path).Item2;
                }
            }
            set
            {
                m_Title = value;
            }
        }

        public string Artist
        {
            get;
            set;
        }

        public string Album
        {
            get;
            set;
        }

        public string Genre
        {
            get;
            set;
        }

        public int? Length
        {
            get;
            set;
        }

        public int? Track
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
                if (Date == null)
                {
                    return null;
                }
                else
                {
                    return Date.Substring(0, 4);
                }
            }
        }

        public override string ToString()
        {
            return Artist + ": " + Title + " (" + Album + ")";
        }

        public int CompareTo(object o)
        {
            if (o is SongMetadata)
            {
                SongMetadata rhs = (SongMetadata)o;
                return StringComparer.Ordinal.Compare(Path, rhs.Path);
            }
            else if (o is StreamMetadata)
            {
                return -1;
            }
            else
            {
                throw new Exception("SongMetadata: attempt to compare to an incompatible object");
            }
        }
    }
}
