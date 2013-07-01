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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Database
    {
        private ServerConnection m_Connection = null;
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByArtist = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByGenre = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<AlbumMetadata, ISet<string>> m_SongPathsByAlbum = new SortedDictionary<AlbumMetadata, ISet<string>>();
        private IDictionary<SongMetadata, AlbumMetadata> m_AlbumBySong = new SortedDictionary<SongMetadata, AlbumMetadata>();
        private IDictionary<string, SongMetadata> m_SongInfo = new SortedDictionary<string, SongMetadata>(StringComparer.Ordinal);
        
        public Database(ServerConnection connection, ServerStatus status)
        {
            m_Connection = connection;
            Artists = new List<string>();
            Genres = new List<string>();
        }

        public bool RefreshCollection()
        {
            m_AlbumsByArtist.Clear();
            m_AlbumsByGenre.Clear();
            m_SongPathsByAlbum.Clear();
            m_AlbumBySong.Clear();
            m_SongInfo.Clear();

            Artists = new List<string>();
            Genres = new List<string>();

            if (m_Connection.Status == ServerConnection.State.Connected)
            {
                PopulateSongInfo(m_Connection);
                
                PopulateArtists();
                PopulateGenres();
                                
                PopulateAlbumsByArtist();
                PopulateAlbumsByGenre();
                PopulateSongPathsByAlbum();
                PopulateAlbumsBySong();
            }

            return true;
        }

        public IEnumerable<string> Artists
        {
            get;
            private set;
        }

        public IEnumerable<string> Genres
        {
            get;
            private set;
        }

        public IEnumerable<SongMetadata> Songs
        {
            get
            {
                return m_SongInfo.Values;
            }
        }

        public ISet<AlbumMetadata> AlbumsByArtist(string artist)
        {
            ISet<AlbumMetadata> result = new SortedSet<AlbumMetadata>();

            if (m_AlbumsByArtist.ContainsKey(artist))
            {
                foreach (AlbumMetadata album in m_AlbumsByArtist[artist])
                {
                    result.Add(album);
                }
            }

            return result;
        }

        public ISet<AlbumMetadata> AlbumsByGenre(string genre)
        {
            ISet<AlbumMetadata> result = new SortedSet<AlbumMetadata>();

            if (m_AlbumsByGenre.ContainsKey(genre))
            {
                foreach (AlbumMetadata album in m_AlbumsByGenre[genre])
                {
                    result.Add(album);
                }
            }

            return result;
        }

        public AlbumMetadata AlbumOfSong(SongMetadata song)
        {
            AlbumMetadata album = null;

            if (m_AlbumBySong.TryGetValue(song, out album))
            {
                return album;
            }
            else
            {
                return null;
            }
        }

        public ISet<SongMetadata> SongsByAlbum(AlbumMetadata byAlbum)
        {
            SortedSet<SongMetadata> result = new SortedSet<SongMetadata>();

            if (m_SongPathsByAlbum.ContainsKey(byAlbum))
            {
                foreach (string path in m_SongPathsByAlbum[byAlbum])
                {
                    if (m_SongInfo.ContainsKey(path))
                    {
                        result.Add(m_SongInfo[path]);
                    }
                }
            }

            return result;
        }

        public SongMetadata SongByPath(string path)
        {
            SongMetadata result;
            
            if (m_SongInfo.TryGetValue(path, out result))
            {
                return result;
            }

            return null;
        }
                
        private void PopulateSongInfo(ServerConnection connection)
        {
            ServerResponse response = Protocol.ListAllInfo(connection);

            if (response != null && response.IsOK)
            {
                SongMetadata song = new SongMetadata();

                foreach (ServerResponseLine line in response.ResponseLines)
                {
                    if (line.Name == "file")
                    {
                        if (song.Path != null)
                        {
                            m_SongInfo.Add(song.Path, song);
                        }

                        song = new SongMetadata();
                        song.Path = line.Value;
                    }
                    else if (line.Name == "Title")
                    {
                        song.Title = line.Value;
                    }
                    else if (line.Name == "Artist")
                    {
                        song.Artist = line.Value;
                    }
                    else if (line.Name == "Album")
                    {
                        song.Album = line.Value;
                    }
                    else if (line.Name == "Genre")
                    {
                        song.Genre = line.Value;
                    }
                    else if (line.Name == "Time")
                    {
                        song.Length = Utils.StringToInt(line.Value);
                    }
                    else if (line.Name == "Date")
                    {
                        song.Year = Utils.StringToInt(line.Value);
                    }
                    else if (line.Name == "Track")
                    {
                        song.Track = Utils.StringToInt(line.Value);
                    }
                }

                if (song.Path != null)
                {
                    m_SongInfo.Add(song.Path, song);
                }
            }
        }

        private void PopulateArtists()
        {
            ISet<string> uniqueArtists = new SortedSet<string>();

            foreach (SongMetadata song in m_SongInfo.Values)
            {
                uniqueArtists.Add(song.Artist);
            }

            Artists = uniqueArtists;
        }

        private void PopulateGenres()
        {
            ISet<string> uniqueGenres = new SortedSet<string>();

            foreach (SongMetadata song in m_SongInfo.Values)
            {
                uniqueGenres.Add(song.Genre);
            }

            Genres = uniqueGenres;
        }
        
        private void PopulateAlbumsByArtist()
        {
            foreach (SongMetadata song in m_SongInfo.Values)
            {
                if (!m_AlbumsByArtist.ContainsKey(song.Artist))
                {
                    m_AlbumsByArtist[song.Artist] = new SortedSet<AlbumMetadata>();
                }

                AlbumMetadata album = new AlbumMetadata();
                album.Artist = song.Artist;
                album.Title = song.Album;

                m_AlbumsByArtist[song.Artist].Add(album);
            }
        }

        private void PopulateAlbumsByGenre()
        {
            foreach (SongMetadata song in m_SongInfo.Values)
            {
                if (!m_AlbumsByGenre.ContainsKey(song.Genre))
                {
                    m_AlbumsByGenre[song.Genre] = new SortedSet<AlbumMetadata>();
                }

                AlbumMetadata album = new AlbumMetadata();
                album.Artist = song.Artist;
                album.Title = song.Album;

                m_AlbumsByGenre[song.Genre].Add(album);
            }
        }

        private void PopulateAlbumsBySong()
        {
            foreach (KeyValuePair<AlbumMetadata, ISet<string>> albumAndSongs in m_SongPathsByAlbum)
            {
                foreach (string songPath in albumAndSongs.Value)
                {
                    m_AlbumBySong.Add(SongByPath(songPath), albumAndSongs.Key);
                }
            }
        }

        private void PopulateSongPathsByAlbum()
        {
            foreach (SongMetadata song in m_SongInfo.Values)
            {
                // Note that we are now making copies of all album metadata. We
                // could reference the metadata in m_AlbumsByArtist instead.
                AlbumMetadata album = new AlbumMetadata(song.Artist, song.Album);

                if (!m_SongPathsByAlbum.ContainsKey(album))
                {
                    m_SongPathsByAlbum[album] = new SortedSet<string>();
                }

                m_SongPathsByAlbum[album].Add(song.Path);
            }
        }
    }
}
