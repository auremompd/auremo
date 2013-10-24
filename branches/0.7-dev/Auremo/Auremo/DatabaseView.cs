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

        private IList<MusicCollectionItem> m_SelectedSearchResults = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedArtists = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedAlbumsBySelectedArtists = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedSongsOnSelectedAlbumsBySelectedArtists = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedGenres = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedAlbumsOfSelectedGenres = new List<MusicCollectionItem>();
        private IList<MusicCollectionItem> m_SelectedSongsOnSelectedAlbumsOfSelectedGenres = new List<MusicCollectionItem>();

        private ISet<string> m_SelectedGenresLookup = new SortedSet<string>();

        public delegate ISet<AlbumMetadata> AlbumsUnderRoot(string root);
        public delegate ISet<SongMetadata> SongsOnAlbum(AlbumMetadata album);

        #region Construction and setup

        public DatabaseView(DataModel dataModel)
        {
            m_DataModel = dataModel;

            SearchResults = new ObservableCollection<MusicCollectionItem>();

            Artists = new ObservableCollection<MusicCollectionItem>();
            AlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();
            SongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();
            SelectedSongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();

            Genres = new ObservableCollection<MusicCollectionItem>();
            AlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();
            SelectedSongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();

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
                Artists.Add(new MusicCollectionItem(artist, Artists.Count));
            }
        }

        private void PopulateGenres()
        {
            Genres.Clear();

            foreach (string genre in m_DataModel.Database.Genres)
            {
                Genres.Add(new MusicCollectionItem(genre, Genres.Count));
            }
        }
        
        private void PopulateArtistTree()
        {
            ArtistTree.Clear();
            ArtistTreeController.MultiSelection.Clear();

            foreach (string artist in m_DataModel.Database.Artists)
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

            foreach (string genre in m_DataModel.Database.Genres)
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
                SearchResults = new ObservableCollection<MusicCollectionItem>(m_DataModel.CollectionSearch.SearchResults);
                NotifyPropertyChanged("SearchResults");
            }
        }

        public IList<MusicCollectionItem> SearchResults
        {
            get;
            private set;
        }

        public IList<MusicCollectionItem> SelectedSearchResults
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

        public IList<MusicCollectionItem> Artists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> AlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public IList<MusicCollectionItem> SelectedArtists
        {
            get
            {
                return m_SelectedArtists;
            }
            set
            {
                m_SelectedArtists = value;
                AlbumsBySelectedArtists.Clear();

                foreach (MusicCollectionItem artistItem in value)
                {
                    foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artistItem.Content as string))
                    {
                        AlbumsBySelectedArtists.Add(new MusicCollectionItem(album, AlbumsBySelectedArtists.Count));
                    }
                }

                NotifyPropertyChanged("SelectedArtists");
            }
        }

        public IList<MusicCollectionItem> SelectedAlbumsBySelectedArtists
        {
            get
            {
                return m_SelectedAlbumsBySelectedArtists;
            }
            set
            {
                m_SelectedAlbumsBySelectedArtists = value;
                SongsOnSelectedAlbumsBySelectedArtists.Clear();

                foreach (MusicCollectionItem albumItem in value)
                {
                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(albumItem.Content as AlbumMetadata))
                    {
                        SongsOnSelectedAlbumsBySelectedArtists.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsBySelectedArtists.Count));
                    }
                }

                NotifyPropertyChanged("SelectedAlbumsBySelectedArtists");
            }
        }

        public IList<MusicCollectionItem> SelectedSongsOnSelectedAlbumsBySelectedArtists
        {
            get
            {
                return m_SelectedSongsOnSelectedAlbumsBySelectedArtists;
            }
            set
            {
                m_SelectedSongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>(value);
                NotifyPropertyChanged("SelectedSongsOnSelectedAlbumsBySelectedArtists");
            }
        }
                
        #endregion

        #region Genre/album/artist view

        public IList<MusicCollectionItem> Genres
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> AlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SongsOnSelectedAlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public IList<MusicCollectionItem> SelectedGenres
        {
            get
            {
                return m_SelectedGenres;
            }
            set
            {
                m_SelectedGenres = value;
                AlbumsOfSelectedGenres.Clear();
                m_SelectedGenresLookup.Clear();

                foreach (MusicCollectionItem genreItem in value)
                {
                    string genre = genreItem.Content as string;
                    m_SelectedGenresLookup.Add(genre);

                    foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre))
                    {
                        AlbumsOfSelectedGenres.Add(new MusicCollectionItem(album, AlbumsOfSelectedGenres.Count));
                    }
                }

                NotifyPropertyChanged("SelectedGenres");
            }
        }

        public IList<MusicCollectionItem> SelectedAlbumsOfSelectedGenres
        {
            get
            {
                return m_SelectedAlbumsOfSelectedGenres;
            }
            set
            {
                m_SelectedAlbumsOfSelectedGenres = value;
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();

                foreach (MusicCollectionItem albumItem in value)
                {
                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(albumItem.Content as AlbumMetadata))
                    {
                        if (m_SelectedGenresLookup.Contains(song.Genre))
                        {
                            SongsOnSelectedAlbumsOfSelectedGenres.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsOfSelectedGenres.Count));
                        }
                    }
                }

                NotifyPropertyChanged("SelectedAlbumsOfSelectedGenres");
            }
        }

        public IList<MusicCollectionItem> SelectedSongsOnSelectedAlbumsOfSelectedGenres
        {
            get
            {
                return m_SelectedSongsOnSelectedAlbumsOfSelectedGenres;
            }
            set
            {
                m_SelectedSongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>(value);
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
