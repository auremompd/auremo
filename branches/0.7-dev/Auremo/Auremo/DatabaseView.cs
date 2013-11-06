﻿/*
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

        private ISet<string> m_SelectedGenres = new SortedSet<string>();
        private ISet<AlbumMetadata> m_SelectedAlbumsOfSelectedGenres = new SortedSet<AlbumMetadata>();

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
            SelectedArtists = new ObservableCollection<MusicCollectionItem>();
            SelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();
            SelectedSongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<MusicCollectionItem>();

            Genres = new ObservableCollection<MusicCollectionItem>();
            AlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<MusicCollectionItem>();

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

        public ObservableCollection<MusicCollectionItem> Artists
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

        public ObservableCollection<MusicCollectionItem> SelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public ObservableCollection<MusicCollectionItem> SelectedSongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public void OnSelectedArtistsChanged()
        {
            // Check is anything really changed to prevent infinite recursion.
            ISet<string> currentlySelectedArtists = CollectSelectedElements<string>(Artists);
            bool changed = currentlySelectedArtists.Count != SelectedArtists.Count;

            if (!changed)
            {
                foreach (MusicCollectionItem artist in SelectedArtists)
                {
                    if (artist.IsSelected != currentlySelectedArtists.Contains(artist.Content as string))
                    {
                        changed = true;
                        break;
                    }
                }

                if (!changed)
                {
                    return;
                }
            }
            
            // Changes detected, proceed.
            SelectedArtists.Clear();
            AlbumsBySelectedArtists.Clear();
            SelectedAlbumsBySelectedArtists.Clear();
            SongsOnSelectedAlbumsBySelectedArtists.Clear();
            SelectedSongsOnSelectedAlbumsBySelectedArtists.Clear();

            foreach (MusicCollectionItem artist in Artists)
            {
                if (artist.IsSelected)
                {
                    SelectedArtists.Add(new MusicCollectionItem(artist.Content, SelectedArtists.Count, true));

                    foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByArtist(artist.Content as string))
                    {
                        AlbumsBySelectedArtists.Add(new MusicCollectionItem(album, AlbumsBySelectedArtists.Count));
                    }
                }
            }
        }

        public void OnSelectedAlbumsBySelectedArtistsChanged()
        {
            // Check is anything really changed to prevent infinite recursion.
            ISet<AlbumMetadata> currentlySelectedAlbums = CollectSelectedElements<AlbumMetadata>(AlbumsBySelectedArtists);
            bool changed = currentlySelectedAlbums.Count != SelectedAlbumsBySelectedArtists.Count;

            if (!changed)
            {
                foreach (MusicCollectionItem album in SelectedAlbumsBySelectedArtists)
                {
                    if (album.IsSelected != currentlySelectedAlbums.Contains(album.Content as AlbumMetadata))
                    {
                        changed = true;
                        break;
                    }
                }

                if (!changed)
                {
                    return;
                }
            }
            
            // Changes detected, proceed.
            SelectedAlbumsBySelectedArtists.Clear();
            SongsOnSelectedAlbumsBySelectedArtists.Clear();
            SelectedSongsOnSelectedAlbumsBySelectedArtists.Clear();

            foreach (MusicCollectionItem album in AlbumsBySelectedArtists)
            {
                if (album.IsSelected)
                {
                    SelectedAlbumsBySelectedArtists.Add(new MusicCollectionItem(album.Content, SelectedAlbumsBySelectedArtists.Count, true));

                    foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album.Content as AlbumMetadata))
                    {
                        SongsOnSelectedAlbumsBySelectedArtists.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsBySelectedArtists.Count));
                    }
                }
            }
        }

        public void ShowSongsInArtistList(IEnumerable<SongMetadata> selectedSongs)
        {
            foreach (MusicCollectionItem artistItem in Artists)
            {
                artistItem.IsSelected = false;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (artistItem.Content as string == selectedSong.Artist)
                    {
                        artistItem.IsSelected = true;
                    }
                }
            }

            OnSelectedArtistsChanged();

            foreach (MusicCollectionItem albumItem in AlbumsBySelectedArtists)
            {
                albumItem.IsSelected = false;
                AlbumMetadata album = albumItem.Content as AlbumMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (album.Artist == selectedSong.Artist && album.Title == selectedSong.Album)
                    {
                        albumItem.IsSelected = true;
                    }
                }
            }

            OnSelectedAlbumsBySelectedArtistsChanged();

            foreach (MusicCollectionItem songItem in SongsOnSelectedAlbumsBySelectedArtists)
            {
                songItem.IsSelected = false;
                SongMetadata songInView = songItem.Content as SongMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (songInView.Path == selectedSong.Path)
                    {
                        songItem.IsSelected = true;
                    }
                }
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

        public void OnSelectedGenresChanged()
        {
            ISet<string> newSelectedGenres = CollectSelectedElements<string>(Genres);

            if (!SelectionsAreEqual(newSelectedGenres, m_SelectedGenres))
            {
                m_SelectedGenres = newSelectedGenres;
                m_SelectedAlbumsOfSelectedGenres.Clear();
                AlbumsOfSelectedGenres.Clear();
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();

                foreach (MusicCollectionItem genre in Genres)
                {
                    if (genre.IsSelected)
                    {
                        // TODO: this will add a single album multiple times if it contains songs
                        // of multiple selected genres.
                        foreach (AlbumMetadata album in m_DataModel.Database.AlbumsByGenre(genre.Content as string))
                        {
                            AlbumsOfSelectedGenres.Add(new MusicCollectionItem(album, AlbumsOfSelectedGenres.Count));
                        }
                    }
                }
            }
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged()
        {
            ISet<AlbumMetadata> currentlySelectedAlbums = CollectSelectedElements<AlbumMetadata>(AlbumsOfSelectedGenres);

            if (!SelectionsAreEqual(currentlySelectedAlbums, m_SelectedAlbumsOfSelectedGenres))
            {
                m_SelectedAlbumsOfSelectedGenres = currentlySelectedAlbums;
                SongsOnSelectedAlbumsOfSelectedGenres.Clear();

                foreach (MusicCollectionItem album in AlbumsOfSelectedGenres)
                {
                    if (album.IsSelected)
                    {
                        foreach (SongMetadata song in m_DataModel.Database.SongsByAlbum(album.Content as AlbumMetadata))
                        {
                            if (m_SelectedGenres.Contains(song.Genre))
                            {
                                SongsOnSelectedAlbumsOfSelectedGenres.Add(new MusicCollectionItem(song, SongsOnSelectedAlbumsOfSelectedGenres.Count));
                            }
                        }
                    }
                }
            }
        }

        public void ShowSongsInGenreList(IEnumerable<SongMetadata> selectedSongs)
        {
            foreach (MusicCollectionItem genreItem in Genres)
            {
                genreItem.IsSelected = false;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (genreItem.Content as string == selectedSong.Genre)
                    {
                        genreItem.IsSelected = true;
                    }
                }
            }
            
            OnSelectedGenresChanged();

            foreach (MusicCollectionItem albumItem in AlbumsOfSelectedGenres)
            {
                albumItem.IsSelected = false;
                AlbumMetadata album = albumItem.Content as AlbumMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (album.Artist == selectedSong.Artist && album.Title == selectedSong.Album)
                    {
                        albumItem.IsSelected = true;
                    }
                }
            }
            
            OnSelectedAlbumsOfSelectedGenresChanged();

            foreach (MusicCollectionItem songItem in SongsOnSelectedAlbumsOfSelectedGenres)
            {
                songItem.IsSelected = false;
                SongMetadata songInView = songItem.Content as SongMetadata;

                foreach (SongMetadata selectedSong in selectedSongs)
                {
                    if (songInView.Path == selectedSong.Path)
                    {
                        songItem.IsSelected = true;
                    }
                }
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

        public void ShowSongsInArtistTree(IEnumerable<SongMetadata> selectedSongs)
        {
            ISet<string> selectedArtists = new SortedSet<string>();
            ISet<AlbumMetadata> selectedAlbums = new SortedSet<AlbumMetadata>();
            ISet<string> selectedSongPaths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (SongMetadata song in selectedSongs)
            {
                if (song.IsLocal)
                {
                    selectedArtists.Add(song.Artist);
                    selectedAlbums.Add(new AlbumMetadata(song.Artist, song.Album, null));
                    selectedSongPaths.Add(song.Path);
                }
            }

            ArtistTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in ArtistTreeController.RootLevelNodes)
            {
                ArtistTreeViewNode artistNode = rootNode as ArtistTreeViewNode;
                artistNode.IsExpanded = false;

                if (selectedArtists.Contains(artistNode.Artist))
                {
                    artistNode.IsExpanded = true;

                    foreach (TreeViewNode midNode in artistNode.Children)
                    {
                        AlbumMetadataTreeViewNode albumNode = midNode as AlbumMetadataTreeViewNode;
                        albumNode.IsExpanded = false;

                        if (selectedAlbums.Contains(albumNode.Album))
                        {
                            albumNode.IsExpanded = true;

                            foreach (TreeViewNode leafNode in albumNode.Children)
                            {
                                SongMetadataTreeViewNode songNode = leafNode as SongMetadataTreeViewNode;

                                if (selectedSongPaths.Contains(songNode.Song.Path))
                                {
                                    songNode.IsMultiSelected = true;
                                }
                            }
                        }
                    }
                }
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

        public void ShowSongsInGenreTree(IEnumerable<SongMetadata> selectedSongs)
        {
            ISet<string> selectedGenres = new SortedSet<string>();
            ISet<AlbumMetadata> selectedAlbums = new SortedSet<AlbumMetadata>();
            ISet<string> selectedSongPaths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (SongMetadata song in selectedSongs)
            {
                if (song.IsLocal)
                {
                    selectedGenres.Add(song.Genre);
                    selectedAlbums.Add(new AlbumMetadata(song.Artist, song.Album, null));
                    selectedSongPaths.Add(song.Path);
                }
            }

            GenreTreeController.ClearMultiSelection();

            foreach (TreeViewNode rootNode in GenreTreeController.RootLevelNodes)
            {
                GenreTreeViewNode genreNode = rootNode as GenreTreeViewNode;
                genreNode.IsExpanded = false;

                if (selectedGenres.Contains(genreNode.Genre))
                {
                    genreNode.IsExpanded = true;

                    foreach (TreeViewNode midNode in genreNode.Children)
                    {
                        AlbumMetadataTreeViewNode albumNode = midNode as AlbumMetadataTreeViewNode;
                        albumNode.IsExpanded = false;

                        if (selectedAlbums.Contains(albumNode.Album))
                        {
                            albumNode.IsExpanded = true;

                            foreach (TreeViewNode leafNode in albumNode.Children)
                            {
                                SongMetadataTreeViewNode songNode = leafNode as SongMetadataTreeViewNode;

                                if (selectedSongPaths.Contains(songNode.Song.Path))
                                {
                                    songNode.IsMultiSelected = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        #endregion

        #region Utility

        private ISet<T> CollectSelectedElements<T>(IEnumerable<MusicCollectionItem> collection) where T : class
        {
            ISet<T> result = new SortedSet<T>();

            foreach (MusicCollectionItem item in collection)
            {
                if (item.IsSelected)
                {
                    result.Add(item.Content as T);
                }
            }

            return result;
        }

        private bool SelectionsAreEqual<T>(IEnumerable<T> lhs, IEnumerable<T> rhs) where T : IComparable
        {
            IEnumerator<T> left = lhs.GetEnumerator();
            IEnumerator<T> right = rhs.GetEnumerator();
            bool equal = lhs.Count() == rhs.Count();

            while (equal && left.MoveNext())
            {
                right.MoveNext();
                equal = left.Current.CompareTo(right.Current) == 0;
            }

            return equal;
        }

        #endregion
    }
}