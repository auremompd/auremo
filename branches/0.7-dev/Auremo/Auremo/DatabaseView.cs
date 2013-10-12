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
    public class DatabaseView : INotifyPropertyChanged
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

        private DataModel m_DataModel = null;

        private IList<object> m_SelectedSearchResults = new List<object>();
        private IList<string> m_SelectedArtists = new List<string>();
        private IList<AlbumMetadata> m_SelectedAlbumsBySelectedArtists = new List<AlbumMetadata>();
        private IList<SongMetadata> m_SelectedSongsOnSelectedAlbumsBySelectedArtists = new List<SongMetadata>();
        private IList<string> m_SelectedGenres = new List<string>();
        private IList<AlbumMetadata> m_SelectedAlbumsOfSelectedGenres = new List<AlbumMetadata>();
        private IList<SongMetadata> m_SelectedSongsOnSelectedAlbumsOfSelectedGenres = new List<SongMetadata>();

        public delegate ISet<AlbumMetadata> AlbumsUnderRoot(string root);
        public delegate ISet<SongMetadata> SongsOnAlbum(AlbumMetadata album);

        #region Construction and setup

        public DatabaseView(DataModel dataModel)
        {
            m_DataModel = dataModel;

            SearchResults = new ObservableCollection<CollectionSearch.SearchResultTuple>();

            Artists = new ObservableCollection<string>();
            AlbumsBySelectedArtists = new ObservableCollection<AlbumMetadata>();
            SongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<SongMetadata>();
            SelectedSongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<SongMetadata>();

            Genres = new ObservableCollection<string>();
            AlbumsOfSelectedGenres = new ObservableCollection<AlbumMetadata>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<SongMetadata>();
            SelectedSongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<SongMetadata>();

            ArtistTree = new ObservableCollection<TreeViewNode>();
            ArtistTreeController = new TreeViewController(ArtistTree);

            GenreTree = new ObservableCollection<TreeViewNode>();
            GenreTreeController = new TreeViewController(GenreTree);

            DirectoryTree = new ObservableCollection<TreeViewNode>();
            DirectoryTreeController = new TreeViewController(DirectoryTree);

            m_DataModel.Database.PropertyChanged += new PropertyChangedEventHandler(OnDatabasePropertyChanged);
            m_DataModel.CollectionSearch.PropertyChanged += new PropertyChangedEventHandler(OnCollectionSearchResultsPropertyChanged);
        }

        private void OnDatabasePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Database")
            {
                PopulateArtists();
                AlbumsBySelectedArtists.Clear();
                SongsOnSelectedAlbumsBySelectedArtists.Clear();
                PopulateGenres();
                AlbumsOfSelectedGenres.Clear();
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();
                PopulateDirectoryTree();
                PopulateArtistTree();
                PopulateGenreTree();
            }
        }

        private void PopulateArtists()
        {
            Artists.Clear();

            foreach (string artist in m_DataModel.Database.Artists)
            {
                Artists.Add(artist);
            }
        }

        private void PopulateGenres()
        {
            Genres.Clear();

            foreach (string genre in m_DataModel.Database.Genres)
            {
                Genres.Add(genre);
            }
        }

        private void PopulateArtistTree()
        {
            ArtistTree.Clear();
            ArtistTreeController.MultiSelection.Clear();

            foreach (string artist in Artists)
            {
                ArtistTreeViewNode artistNode = new ArtistTreeViewNode(artist, null, ArtistTreeController);

                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artist))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, artistNode, ArtistTreeController);
                    artistNode.AddChild(albumNode);

                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, ArtistTreeController);
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
            GenreTreeController.ClearMultiSelection();
            GenreTree.Clear();

            foreach (string genre in Genres)
            {
                GenreTreeViewNode genreNode = new GenreTreeViewNode(genre, null, GenreTreeController);

                foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre))
                {
                    AlbumMetadataTreeViewNode albumNode = new AlbumMetadataTreeViewNode(album, genreNode, GenreTreeController);
                    genreNode.AddChild(albumNode);

                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        if (song.Genre == genre)
                        {
                            SongMetadataTreeViewNode songNode = new SongMetadataTreeViewNode("", song, albumNode, GenreTreeController);
                            albumNode.AddChild(songNode);
                        }
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

        private void PopulateDirectoryTree()
        {
            DirectoryTreeController.ClearMultiSelection();
            DirectoryTree.Clear();

            DirectoryTreeViewNode rootNode = new DirectoryTreeViewNode("/", null, DirectoryTreeController);
            IDictionary<string, TreeViewNode> directoryLookup = new SortedDictionary<string, TreeViewNode>();
            directoryLookup[rootNode.DisplayString] = rootNode;

            foreach (SongMetadata song in m_DataModel.Database.Songs)
            {
                TreeViewNode parent = FindDirectoryNode(song.Directory, directoryLookup, rootNode);
                SongMetadataTreeViewNode leaf = new SongMetadataTreeViewNode(song.Filename, song, parent, DirectoryTreeController);
                parent.AddChild(leaf);
            }

            AssignTreeViewNodeIDs(rootNode, 0);
            
            if (rootNode.Children.Count > 0)
            {
                DirectoryTree.Add(rootNode);
                rootNode.IsExpanded = true;
            }
        }

        private TreeViewNode FindDirectoryNode(string path, IDictionary<string, TreeViewNode> lookup, TreeViewNode rootNode)
        {
            if (path == "")
            {
                return rootNode;
            }
            else if (lookup.ContainsKey(path))
            {
                return lookup[path];
            }
            else
            {
                Tuple<string, string> parentAndSelf = Utils.SplitPath(path);
                TreeViewNode parent = FindDirectoryNode(parentAndSelf.Item1, lookup, rootNode);
                TreeViewNode self = new DirectoryTreeViewNode(parentAndSelf.Item2, parent, DirectoryTreeController);
                parent.AddChild(self);
                lookup[path] = self;
                return self;
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

        #endregion

        #region Search

        private void OnCollectionSearchResultsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SearchResults")
            {
                SearchResults = new ObservableCollection<CollectionSearch.SearchResultTuple>(m_DataModel.CollectionSearch.SearchResults);
                NotifyPropertyChanged("SearchResults");
            }
        }

        public IList<CollectionSearch.SearchResultTuple> SearchResults
        {
            get;
            private set;
        }

        public IList<object> SelectedSearchResults
        {
            get
            {
                return m_SelectedSearchResults;
            }
            set
            {
                m_SelectedSearchResults = value;
                NotifyPropertyChanged("SelectedSearchResults");
            }
        }

        #endregion

        #region Artist/album/song view

        public IList<string> Artists
        {
            get;
            private set;
        }

        public ObservableCollection<AlbumMetadata> AlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<SongMetadata> SongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }
        
        public IList<string> SelectedArtists
        {
            get
            {
                return m_SelectedArtists;
            }
            set
            {
                m_SelectedArtists = value;
                AlbumsBySelectedArtists.Clear();

                foreach (string artist in value)
                {
                    foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artist))
                    {
                        AlbumsBySelectedArtists.Add(album);
                    }
                }

                NotifyPropertyChanged("SelectedArtists");
            }
        }

        public IList<AlbumMetadata> SelectedAlbumsBySelectedArtists
        {
            get
            {
                return m_SelectedAlbumsBySelectedArtists;
            }
            set
            {
                m_SelectedAlbumsBySelectedArtists = value;
                SongsOnSelectedAlbumsBySelectedArtists.Clear();

                foreach (AlbumMetadata album in value)
                {
                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        SongsOnSelectedAlbumsBySelectedArtists.Add(song);
                    }
                }

                NotifyPropertyChanged("SelectedAlbumsBySelectedArtists");
            }
        }

        public IList<SongMetadata> SelectedSongsOnSelectedAlbumsBySelectedArtists
        {
            get
            {
                return m_SelectedSongsOnSelectedAlbumsBySelectedArtists;
            }
            set
            {
                m_SelectedSongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<SongMetadata>(value);
                NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsBySelectedArtists");
            }
        }
                
        #endregion

        #region Genre/album/artist view

        public IList<string> Genres
        {
            get;
            private set;
        }

        public ObservableCollection<AlbumMetadata> AlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public ObservableCollection<SongMetadata> SongsOnSelectedAlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public IList<string> SelectedGenres
        {
            get
            {
                return m_SelectedGenres;
            }
            set
            {
                m_SelectedGenres = value;
                AlbumsOfSelectedGenres.Clear();

                foreach (string genre in value)
                {
                    foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre))
                    {
                        AlbumsOfSelectedGenres.Add(album);
                    }
                }

                NotifyPropertyChanged("SelectedGenres");
            }
        }

        public IList<AlbumMetadata> SelectedAlbumsOfSelectedGenres
        {
            get
            {
                return m_SelectedAlbumsOfSelectedGenres;
            }
            set
            {
                m_SelectedAlbumsOfSelectedGenres = value;
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();

                foreach (AlbumMetadata album in value)
                {
                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album))
                    {
                        // TODO: O(n*m), but O(n log(m)) is possible. (m is small.)
                        if (SelectedGenres.Contains(song.Genre))
                        {
                            SongsOnSelectedAlbumsOfSelectedGenres.Add(song);
                        }
                    }
                }

                NotifyPropertyChanged("SelectedAlbumsOfSelectedGenres");
            }
        }

        public IList<SongMetadata> SelectedSongsOnSelectedAlbumsOfSelectedGenres
        {
            get
            {
                return m_SelectedSongsOnSelectedAlbumsOfSelectedGenres;
            }
            set
            {
                m_SelectedSongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<SongMetadata>(value);
                NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsOfSelectedGenres");
            }
        }

        #endregion

        #region Artist/album/song tree view

        public IList<TreeViewNode> ArtistTree
        {
            get;
            private set;
        }

        public TreeViewController ArtistTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> ArtistTreeSelectedSongs
        {
            get
            {
                return ArtistTreeController.Songs;
            }
        }

        #endregion

        #region Genre/album/song tree view

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

        #endregion

        #region Directory tree view

        public IList<TreeViewNode> DirectoryTree
        {
            get;
            private set;
        }

        public TreeViewController DirectoryTreeController
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> DirectoryTreeSelectedSongs
        {
            get
            {
                return DirectoryTreeController.Songs;
            }
        }

        #endregion
    }
}
