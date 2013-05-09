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
    public class SongMetadata : IComparable
    {
        public SongMetadata()
        {
            Path = null;
            Length = null;
            Track = null;
            Year = null;

            Artist = "Unknown Artist";
            Genre = "No Genre";
            Album = "Unknown Album";
            Title = "Unknown Title";
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

        public int? Year
        {
            get;
            set;
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
                return Path.CompareTo(rhs.Path);
            }
            else
            {
                throw new Exception("SongMetadata: attempt to compare to an incompatible object");
            }
        }
    }
}
