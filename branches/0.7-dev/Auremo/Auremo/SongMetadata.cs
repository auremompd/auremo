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
using System.Text.RegularExpressions;

namespace Auremo
{
    public class SongMetadata : Playable, IComparable
    {
        private string m_Path = null;
        private string m_Title = null;
        private string m_PathTypePrefix = null; // This appears to be Mopidy-specific
        private string m_Directory = "";
        private string m_Filename = "";

        public SongMetadata()
        {
            Length = null;
            Track = null;
            Date = null;

            Artist = "Unknown Artist";
            Genre = "No Genre";
            Album = "Unknown Album";
        }

        public string Path
        {
            get
            {
                return m_Path;
            }
            set
            {
                m_Path = value;
                string strippedPath = value;

                if (strippedPath.StartsWith("local:track:"))
                {
                    m_PathTypePrefix = "local:track:";
                    strippedPath = strippedPath.Substring(12);
                }
                else if (strippedPath.StartsWith("spotify:track:"))
                {
                    m_PathTypePrefix = "spotify:track:";
                    strippedPath = strippedPath.Substring(14);
                }

                int lastSlash = strippedPath.LastIndexOf('/');

                if (lastSlash >= 0)
                {
                    m_Directory = strippedPath.Substring(0, lastSlash);
                    strippedPath = strippedPath.Substring(lastSlash + 1);
                }

                m_Filename = strippedPath;
            }
        }

        public string Directory
        {
            get
            {
                return m_Directory;
            }
        }

        public string Filename
        {
            get
            {
                return m_Filename;
            }
        }

        public string Title
        {
            get
            {
                if (m_Title == null)
                {
                    return m_Filename;
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
                return Utils.ExtractYearFromDateString(Date);
            }
        }

        public bool IsLocal
        {
            get
            {
                return m_PathTypePrefix == null || m_PathTypePrefix == "local:track:";
            }
        }

        public bool IsSpotify
        {
            get
            {
                return m_PathTypePrefix == "spotify:track:";
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
