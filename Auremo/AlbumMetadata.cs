/*
 * Copyright 2012 Mikko Teräs
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
    public class AlbumMetadata : IComparable
    {
        public AlbumMetadata()
        {
            Title = "Unknown Album";
        }

        public AlbumMetadata(string artist, string albumTitle, int? year)
        {
            Artist = artist;
            Title = albumTitle;
            Year = year;
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

        public int? Year
        {
            get;
            set;
        }

        public string PrintableYear
        {
            get
            {
                return Year != null ? Year.ToString() : "N/A";
            }
        }

        public int CompareTo(object o)
        {
            if (o is AlbumMetadata)
            {
                AlbumMetadata rhs = (AlbumMetadata)o;

                if (Artist != rhs.Artist)
                {
                    return Artist.CompareTo(rhs.Artist);
                }

                if (Year != rhs.Year)
                {
                    if (Year == null)
                        return 1;
                    else if (rhs.Year == null)
                        return -1;
                    else
                        return Year.Value.CompareTo(rhs.Year.Value);
                }

                return Title.CompareTo(rhs.Title);
            }
            else
            {
                throw new Exception("AlbumMetadata: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return Artist + ": " + Title + " (" + PrintableYear + ")";
        }
    }
}
