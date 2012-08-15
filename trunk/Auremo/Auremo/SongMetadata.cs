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
    public class SongMetadata : IComparable
    {
        public SongMetadata()
        {
        }

        private string m_Path = null;
        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        private string m_Title = null;
        public string Title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        private string m_Artist = null;
        public string Artist
        {
            get { return m_Artist; }
            set { m_Artist = value; }
        }

        private string m_Album = null;
        public string Album
        {
            get { return m_Album; }
            set { m_Album = value; }
        }

        private string m_Genre = null;
        public string Genre
        {
            get { return m_Genre; }
            set { m_Genre = value; }
        }

        private int? m_Length = null;
        public int? Length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }

        private int? m_Track = null;
        public int? Track
        {
            get { return m_Track; }
            set { m_Track = value; }
        }

        private int? m_Year = null;
        public int? Year
        {
            get { return m_Year; }
            set { m_Year = value; }
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
