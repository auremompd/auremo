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
        private IList<string> m_Artists = new ObservableCollection<string>();
        private IList<string> m_Genres = new ObservableCollection<string>();
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByArtist = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<string, ISet<AlbumMetadata>> m_AlbumsByGenre = new SortedDictionary<string, ISet<AlbumMetadata>>();
        private IDictionary<AlbumMetadata, ISet<string>> m_SongPathsByAlbum = new SortedDictionary<AlbumMetadata, ISet<string>>();
        private IDictionary<string, SongMetadata> m_SongInfo = new SortedDictionary<string, SongMetadata>();
        private IList<AlbumMetadata> m_AlbumsBySelectedArtists = new ObservableCollection<AlbumMetadata>();
        private IList<SongMetadata> m_SongsOnSelectedArtistAlbums = new ObservableCollection<SongMetadata>();
        private IList<TreeViewNode> m_DirectoryTree = new ObservableCollection<TreeViewNode>();
        private TreeViewNode m_DirectoryTreeRoot = null;
        private TreeViewController m_ArtistTreeController = null;

        public Database()
        {
            ArtistTree = new ObservableCollection<TreeViewNode>();
            GenreTree = new ObservableCollection<TreeViewNode>();
        }

        public bool Refresh(ServerConnection connection)
        {
            m_AlbumsByArtist.Clear();
            m_AlbumsByGenre.Clear();
            m_SongPathsByAlbum.Clear();
            m_SongInfo.Clear();

            m_Artists.Clear();
            m_Genres.Clear();

            if (connection.Status == ServerConnection.State.Connected)
            {
                PopulateSongInfo(connection);
                
                PopulateArtists();
                PopulateGenres();

                PopulateAlbumsByArtist();
                PopulateAlbumsByGenre();

                PopulateSongPathsByAlbum();
                
                PopulateDirectoryTree();
                PopulateArtistTree();
                PopulateGenreTree();
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

        public IList<string> Genres
        {
            get
            {
                return m_Genres;
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

        public void OnSelectedArtistAlbumsChanged(IList selection)
        {
            m_SongsOnSelectedArtistAlbums.Clear();
            ISet<AlbumMetadata> sortedAlbums = new SortedSet<AlbumMetadata>();

            foreach (object o in selection)
            {
                sortedAlbums.Add(o as AlbumMetadata);
            }

            foreach (AlbumMetadata album in sortedAlbums)
            {
                foreach (string song in m_SongPathsByAlbum[album])
                {
                    m_SongsOnSelectedArtistAlbums.Add(m_SongInfo[song]);
                }
            }
        }

        public IList<SongMetadata> SongsOnSelectedArtistAlbums
        {
            get
            {
                return m_SongsOnSelectedArtistAlbums;
            }
        }

        public IList<TreeViewNode> DirectoryTree
        {
            get
            {
                return m_DirectoryTree;
            }
        }

        public TreeViewController DirectoryTreeController
        {
            get
            {
                return m_DirectoryTreeRoot.Controller;
            }
        }

        public ISet<SongMetadataTreeViewNode> DirectoryTreeSelectedSongs
        {
            get
            {
                return m_DirectoryTreeRoot.Controller.Songs;
            }
        }

        public IList<TreeViewNode> ArtistTree
        {
            get;
            private set;
        }

        public TreeViewController ArtistTreeController
        {
            get
            {
                return m_ArtistTreeController;
            }
        }

        public ISet<SongMetadataTreeViewNode> ArtistTreeSelectedSongs
        {
            get
            {
                return m_ArtistTreeController.Songs;
            }
        }

        public IList<TreeViewNode> GenreTree
        {
            get;
            private set;
        }

        public TreeViewController GenreTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> GenreTreeSelectedSongs
        {
            get
            {
                return GenreTreeController.Songs;
            }
        }

        public ISet<AlbumMetadata> ArtistAlbums(string artist)
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

            foreach (string artist in uniqueArtists)
            {
                m_Artists.Add(artist);
            }
        }

        private void PopulateGenres()
        {
            ISet<string> uniqueGenres = new SortedSet<string>();

            foreach (SongMetadata song in m_SongInfo.Values)
            {
                uniqueGenres.Add(song.Genre);
            }

            foreach (string genre in uniqueGenres)
            {
                m_Genres.Add(genre);
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
                album.Year = song.Year;

                m_AlbumsByGenre[song.Genre].Add(album);
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
            TreeViewController controller = new TreeViewController(m_DirectoryTree);   
            m_DirectoryTree.Clear();
            m_DirectoryTreeRoot = new DirectoryTreeViewNode("/", null, controller);
            IDictionary<string, TreeViewNode> directoryLookup = new SortedDictionary<string, TreeViewNode>();
            directoryLookup[m_DirectoryTreeRoot.DisplayString] = m_DirectoryTreeRoot;

            foreach (KeyValuePair<string, SongMetadata> entry in m_SongInfo)
            {
                Tuple<string, string> directoryAndFile = Utils.SplitPath(entry.Key);
                TreeViewNode parent = FindDirectoryNode(directoryAndFile.Item1, directoryLookup, controller);
                SongMetadataTreeViewNode leaf = new SongMetadataTreeViewNode(directoryAndFile.Item2, entry.Value, parent, controller);
                parent.AddChild(leaf);
            }

            AssignTreeViewNodeIDs(m_DirectoryTreeRoot, 0);

            m_DirectoryTree.Add(m_DirectoryTreeRoot);
            m_DirectoryTreeRoot.IsExpanded = true;
        }

        private TreeViewNode FindDirectoryNode(string path, IDictionary<string, TreeViewNode> lookup, TreeViewController controller)
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
                TreeViewNode parent = FindDirectoryNode(parentAndSelf.Item1, lookup, controller);
                TreeViewNode self = new DirectoryTreeViewNode(parentAndSelf.Item2, parent, controller);
                parent.AddChild(self);
                lookup[path] = self;
                return self;
            }
        }

        private void PopulateArtistTree()
        {
            m_ArtistTreeController = new TreeViewController(ArtistTree);
            ArtistTree.Clear();

            foreach (string artist in m_Artists)
            {
                ArtistTreeViewNode artistNode = new ArtistTreeViewNode(artist, null, m_ArtistTreeController);

                foreach (AlbumMetadata album in ArtistAlbums(artist))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, artistNode, m_ArtistTreeController);
                    artistNode.AddChild(albumNode);

                    foreach (SongMetadata song in Songs(album))
                    {
                        SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, m_ArtistTreeController);
                        albumNode.AddChild(songNode);
                    }
                }

                ArtistTree.Add(artistNode); // Insert now that branch is fully populated.
            }

            int id = 0;

            foreach (TreeViewNode baseNode in ArtistTree)
            {
                id = AssignTreeViewNodeIDs(baseNode, id);
            }
        }

        private void PopulateGenreTree()
        {
            GenreTreeController = new TreeViewController(GenreTree);
            GenreTree.Clear();

            foreach (string genre in m_Genres)
            {
                GenreTreeViewNode genreNode = new GenreTreeViewNode(genre, null, GenreTreeController);

                foreach (AlbumMetadata album in AlbumsByGenre(genre))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, genreNode, GenreTreeController);
                    genreNode.AddChild(albumNode);

                    foreach (SongMetadata song in Songs(album))
                    {
                        SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, GenreTreeController);
                        albumNode.AddChild(songNode);
                    }
                }
                
                GenreTree.Add(genreNode);
            }

            int id = 0;

            foreach (TreeViewNode baseNode in GenreTree)
            {
                id = AssignTreeViewNodeIDs(baseNode, id);
            }
        }

        private int AssignTreeViewNodeIDs(TreeViewNode node, int nodeID)
        {
            node.ID = nodeID;
            int nextNodeID = nodeID + 1;

            foreach (TreeViewNode child in node.Children)
            {
                nextNodeID = AssignTreeViewNodeIDs(child, nextNodeID);
            }

            return nextNodeID;
        }
    }
}
