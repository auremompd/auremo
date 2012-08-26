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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Database
    {
        private ISet<string> m_ArtistInfo = new SortedSet<string>();
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByArtist = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<AlbumMetadata, ISet<string>> m_SongPathsByAlbum = new SortedDictionary<AlbumMetadata, ISet<string>>();
        private IDictionary<string, SongMetadata> m_SongInfo = new SortedDictionary<string, SongMetadata>();
        private IList<string> m_Artists = new ObservableCollection<string>();
        private IList<AlbumMetadata> m_AlbumsBySelectedArtists = new ObservableCollection<AlbumMetadata>();
        private IList<SongMetadata> m_SongsOnSelectedAlbums = new ObservableCollection<SongMetadata>();

        private IList<ITreeViewModel> m_DirectoryTree = new ObservableCollection<ITreeViewModel>();
        private ITreeViewModel m_DirectoryTreeRoot = null;

        public Database()
        {
        }

        public bool Refresh(ServerConnection connection)
        {
            m_ArtistInfo.Clear();
            m_AlbumsByArtist.Clear();
            m_SongPathsByAlbum.Clear();
            m_SongInfo.Clear();

            m_Artists.Clear();

            if (connection.Status == ServerConnection.State.Connected)
            {
                PopulateSongInfo(connection);
                PopulateArtists();
                PopulateAlbumsByArtist();
                PopulateSongPathsByAlbum();
                PopulateDirectoryTree();
            }

            return true;
        }

        public IList<string> Artists
        {
            get
            {
                return m_Artists;
            }
        }

        public void OnSelectedArtistsChanged(IList selection)
        {
            m_AlbumsBySelectedArtists.Clear();
            ISet<string> sortedArtists = new SortedSet<string>();

            foreach (object o in selection)
            {
                sortedArtists.Add(o as string);
            }

            foreach (string artist in sortedArtists)
            {
                foreach (AlbumMetadata album in m_AlbumsByArtist[artist])
                {
                    m_AlbumsBySelectedArtists.Add(album);
                }
            }
        }

        public IList<AlbumMetadata> AlbumsBySelectedArtists
        {
            get
            {
                return m_AlbumsBySelectedArtists;
            }
        }

        public void OnSelectedAlbumsChanged(IList selection)
        {
            m_SongsOnSelectedAlbums.Clear();
            ISet<AlbumMetadata> sortedAlbums = new SortedSet<AlbumMetadata>();

            foreach (object o in selection)
            {
                sortedAlbums.Add(o as AlbumMetadata);
            }

            foreach (AlbumMetadata album in sortedAlbums)
            {
                foreach (string song in m_SongPathsByAlbum[album])
                {
                    m_SongsOnSelectedAlbums.Add(m_SongInfo[song]);
                }
            }
        }

        public IList<SongMetadata> SongsOnSelectedAlbums
        {
            get
            {
                return m_SongsOnSelectedAlbums;
            }
        }

        public IList<ITreeViewModel> DirectoryTree
        {
            get
            {
                return m_DirectoryTree;
            }
        }

        public TreeViewMultiSelection DirectoryTreeMultiSelection
        {
            get
            {
                return m_DirectoryTreeRoot.MultiSelection;
            }
        }


        public ISet<SongMetadataTreeViewModel> DirectoryTreeSelectedSongs
        {
            get
            {
                return m_DirectoryTreeRoot.MultiSelection.Songs;
            }
        }

        public ISet<AlbumMetadata> Albums(string byArtist)
        {
            ISet<AlbumMetadata> result = new SortedSet<AlbumMetadata>();

            if (m_AlbumsByArtist.ContainsKey(byArtist))
            {
                foreach (AlbumMetadata album in m_AlbumsByArtist[byArtist])
                {
                    result.Add(album);
                }
            }

            return result;
        }

        public ISet<SongMetadata> Songs(AlbumMetadata byAlbum)
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

        public SongMetadata Song(string byPath)
        {
            if (m_SongInfo.ContainsKey(byPath))
            {
                return m_SongInfo[byPath];
            }
            else
            {
                return new SongMetadata();
            }
        }

        private void PopulateSongInfo(ServerConnection connection)
        {
            ServerResponse response = Protocol.ListAllInfo(connection);

            if (response != null && response.IsOK)
            {
                SongMetadata song = new SongMetadata();

                foreach (ServerResponseLine line in response.Lines)
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

            foreach (string artist in uniqueArtists)
            {
                m_Artists.Add(artist);
            }
        }

        private void PopulateAlbumsByArtist()
        {
            foreach (SongMetadata song in m_SongInfo.Values)
            {
                // TODO: handle cases where a single album has songs from
                // multiple years; use the maximum year as the album year.
                // The solution should involve removing an existing album
                // from the dictionary, maxing the year and reinserting for
                // each song.

                if (!m_AlbumsByArtist.ContainsKey(song.Artist))
                {
                    m_AlbumsByArtist[song.Artist] = new SortedSet<AlbumMetadata>();
                }

                AlbumMetadata album = new AlbumMetadata();
                album.Artist = song.Artist;
                album.Title = song.Album;
                album.Year = song.Year;

                m_AlbumsByArtist[song.Artist].Add(album);
            }
        }

        private void PopulateSongPathsByAlbum()
        {
            foreach (SongMetadata song in m_SongInfo.Values)
            {
                // Note that we are now making copies of all album metadata. We
                // could reference the metadata in m_AlbumsByArtist instead.
                AlbumMetadata album = new AlbumMetadata(song.Artist, song.Album, song.Year);

                if (!m_SongPathsByAlbum.ContainsKey(album))
                {
                    m_SongPathsByAlbum[album] = new SortedSet<string>();
                }

                m_SongPathsByAlbum[album].Add(song.Path);
            }
        }

        private void PopulateDirectoryTree()
        {
            TreeViewMultiSelection multiSelection = new TreeViewMultiSelection(m_DirectoryTree);   
            m_DirectoryTree.Clear();
            m_DirectoryTreeRoot = new DirectoryTreeViewModel("/", null, multiSelection);
            IDictionary<string, ITreeViewModel> directoryLookup = new SortedDictionary<string, ITreeViewModel>();
            directoryLookup[m_DirectoryTreeRoot.DisplayString] = m_DirectoryTreeRoot;

            foreach (KeyValuePair<string, SongMetadata> entry in m_SongInfo)
            {
                Tuple<string, string> directoryAndFile = Utils.SplitPath(entry.Key);
                ITreeViewModel parent = FindDirectoryViewModel(directoryAndFile.Item1, directoryLookup, multiSelection);
                SongMetadataTreeViewModel leaf = new SongMetadataTreeViewModel(directoryAndFile.Item2, entry.Value, parent, multiSelection);
                parent.AddChild(leaf);
            }

            AssignTreeViewModelHierarchyIDs(m_DirectoryTreeRoot, 0);

            m_DirectoryTree.Add(m_DirectoryTreeRoot);
            m_DirectoryTreeRoot.IsExpanded = true;
        }

        private ITreeViewModel FindDirectoryViewModel(string path, IDictionary<string, ITreeViewModel> lookup, TreeViewMultiSelection multiSelection)
        {
            if (path == "")
            {
                return m_DirectoryTreeRoot;
            }
            else if (lookup.ContainsKey(path))
            {
                return lookup[path];
            }
            else
            {
                Tuple<string, string> parentAndSelf = Utils.SplitPath(path);
                ITreeViewModel parent = FindDirectoryViewModel(parentAndSelf.Item1, lookup, multiSelection);
                ITreeViewModel self = new DirectoryTreeViewModel(parentAndSelf.Item2, parent, multiSelection);
                parent.AddChild(self);
                lookup[path] = self;
                return self;
            }
        }

        int AssignTreeViewModelHierarchyIDs(ITreeViewModel node, int nodeID)
        {
            node.HierarchyID = nodeID;
            int nextNodeID = nodeID + 1;

            foreach (ITreeViewModel child in node.Children)
            {
                nextNodeID = AssignTreeViewModelHierarchyIDs(child, nextNodeID);
            }

            return nextNodeID;
        }
    }
}
