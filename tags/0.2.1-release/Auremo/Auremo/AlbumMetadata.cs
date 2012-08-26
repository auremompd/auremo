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
            m_AlbumTitle = "Unknown Album";
        }

        public AlbumMetadata(string artist, string albumTitle, int? year)
        {
            m_Artist = artist;
            m_AlbumTitle = albumTitle;
            m_Year = year;
        }

        private string m_Artist = null;
        public string Artist
        {
            get { return m_Artist; }
            set { m_Artist = value; }
        }

        private string m_AlbumTitle = null;
        public string Title
        {
            get { return m_AlbumTitle; }
            set { m_AlbumTitle = value; }
        }

        private int? m_Year = null;
        public int? Year
        {
            get { return m_Year; }
            set { m_Year = value; }
        }

        public string PrintableYear
        {
            get
            {
                return m_Year != null ? m_Year.ToString() : "N/A";
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
