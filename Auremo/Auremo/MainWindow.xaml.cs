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

using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Auremo.Properties;

namespace Auremo
{
    public partial class MainWindow : Window
    {
        private SettingsWindow m_SettingsWindow = null;
        private TextWindow m_LicenseWindow = null;
        private AboutWindow m_AboutWindow = null;
        private DispatcherTimer m_Timer = null;
        private object m_DragSource = null;
        private IList<object> m_DragDropPayload = null;
        private string m_DragDropData = null;
        private Nullable<Point> m_DragStartPosition = null;
        private bool m_PropertyUpdateInProgress = false;
        private bool m_OnlineMode = true;
        private string m_AutoSearchString = "";
        private DateTime m_TimeOfLastAutoSearch = DateTime.MinValue;
        private object m_LastAutoSearchSender = null;

        private const int m_AutoSearchMaxKeystrokeGap = 2500;

        private const string AddSearchResults = "add_search_results";
        private const string AddArtists = "add_artists";
        private const string AddGenres = "add_genres";
        private const string AddAlbums = "add_albums";
        private const string AddSongs = "add_songs";
        private const string AddStreams = "add_streams";
        private const string LoadPlaylist = "load_playlist";
        private const string AddPlaylistItems = "add_playlist_items";
        private const string MovePlaylistItems = "move_playlist_items";

        #region Start-up, construction and destruction

        public MainWindow()
        {
            InitializeComponent();
            InitializeComplexObjects();
            SetUpDataBindings();
            SetUpTreeViewControllers();
            CreateTimer(Settings.Default.ViewUpdateInterval);
            ApplyInitialSettings();
            SetInitialWindowState();
            Update();
        }

        public DataModel DataModel
        {
            get;
            private set;
        }

        private void InitializeComplexObjects()
        {
            DataModel = new DataModel();
        }

        private void SetUpDataBindings()
        {
            NameScope.SetNameScope(m_StreamsViewContextMenu, NameScope.GetNameScope(this));
            NameScope.SetNameScope(m_SavedPlaylistsViewContextMenu, NameScope.GetNameScope(this));
            DataContext = DataModel;
            DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        private void SetUpTreeViewControllers()
        {
            m_DirectoryTree.Tag = DataModel.DatabaseView.DirectoryTreeController;
            m_ArtistTree.Tag = DataModel.DatabaseView.ArtistTreeController;
            m_GenreTree.Tag = DataModel.DatabaseView.GenreTreeController;
        }

        private void CreateTimer(int interval)
        {
            m_Timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
            m_Timer.Tick += new EventHandler(OnTimerTick);
            SetTimerInterval(interval);
            m_Timer.Start();
        }

        private void SetOnlineMode(bool to)
        {
            m_OnlineMode = to;

            if (m_OnlineMode)
            {
                ConnectTo(Settings.Default.Server, Settings.Default.Port);
            }
            else
            {
                Disconnect();
            }
        }

        private void ConnectTo(string host, int port)
        {
            DataModel.ServerConnection.SetHost(host, port);
            DataModel.ServerConnection.StartConnecting();
            SetTimerInterval(100); // Run with tight frequency until connected.
        }

        private void DoPostConnectInit()
        {
            string password = Crypto.DecryptPassword(Settings.Default.Password);

            if (password.Length > 0)
            {
                Protocol.Password(DataModel.ServerConnection, password);
            }
            
            DataModel.Database.RefreshCollection();
            DataModel.DatabaseView.RefreshCollection();
            DataModel.SavedPlaylists.Refresh(DataModel.ServerConnection);
            SetTimerInterval(Settings.Default.ViewUpdateInterval); // Normal operation.
        }

        private void Disconnect()
        {
            if (DataModel.ServerConnection.Status == ServerConnection.State.Connected)
            {
                Protocol.Close(DataModel.ServerConnection);
            }

            DataModel.ServerConnection.Disconnect();
            DataModel.Database.RefreshCollection();
            DataModel.DatabaseView.RefreshCollection();
            DataModel.SavedPlaylists.Refresh(DataModel.ServerConnection);
        }

        private void SetInitialWindowState()
        {
            Show();

            if (!Settings.Default.InitialSetupDone)
            {
                BringUpSettingsWindow();
            }
        }

        #endregion

        #region Updating logic and helpers

        private void SetTimerInterval(int interval)
        {
            m_Timer.Interval = new TimeSpan(0, 0, 0, 0, interval);
        }

        public void OnTimerTick(Object sender, EventArgs e)
        {
            Update();
        }

        private void Update()
        {
            if (DataModel.ServerConnection.Status == ServerConnection.State.Connecting)
            {
                if (DataModel.ServerConnection.IsReadyToConnect)
                {
                    ServerResponse banner = DataModel.ServerConnection.FinishConnecting();

                    if (banner != null && banner.IsOK)
                    {
                        DoPostConnectInit();
                    }
                    else
                    {
                        Disconnect();
                    }
                }
            }
            else if (DataModel.ServerConnection.Status == ServerConnection.State.Disconnected)
            {
                if (m_OnlineMode &&
                    Settings.Default.ReconnectInterval > 0 &&
                    DataModel.ServerConnection.TimeSinceDisconnect.TotalSeconds >= Settings.Default.ReconnectInterval)
                {
                    ConnectTo(Settings.Default.Server, Settings.Default.Port);
                }
            }

            DataModel.ServerStatus.Update(DataModel.ServerConnection);
            DataModel.OutputCollection.Update(DataModel.ServerConnection);
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            m_PropertyUpdateInProgress = true;

            if (e.PropertyName == "OK")
            {
                if (!DataModel.ServerStatus.OK)
                {
                    m_SearchBox.Text = "";
                }
            }
            else if (e.PropertyName == "PlayPosition")
            {
                OnPlayPositionChanged();
            }
            else if (e.PropertyName == "Volume")
            {
                OnVolumeChanged();
            }
            else if (e.PropertyName == "DatabaseUpdateTime")
            {
                DataModel.Database.RefreshCollection();
                DataModel.DatabaseView.RefreshCollection();
            }

            m_PropertyUpdateInProgress = false;
        }

        private void OnPlayPositionChanged()
        {
            if (m_SeekBarPositionFromUser < 0)
            {
                // The user is not dragging the slider, so set the
                // values freely.
                m_SeekBar.Value = DataModel.ServerStatus.PlayPosition;
                m_PlayPosition.Content = Utils.IntToTimecode(DataModel.ServerStatus.PlayPosition);
            }
        }

        private void OnVolumeChanged()
        {
            m_VolumeControl.IsEnabled = DataModel.ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            m_VolumeControl.Value = DataModel.ServerStatus.Volume.HasValue ? DataModel.ServerStatus.Volume.Value : 0;
        }

        #endregion

        #region Selection notifications to data model

        private void OnSelectedSearchResultsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedSearchResults = SearchResultsCellsToObjects();
        }

        private void OnSelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedArtists = Utils.ToTypedList<string>(m_ArtistsView.SelectedItems);
            m_AlbumsBySelectedArtistsView.SelectedIndex = -1;
        }

        private void OnSelectedAlbumsBySelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedAlbumsBySelectedArtists = Utils.ToTypedList<AlbumMetadata>(m_AlbumsBySelectedArtistsView.SelectedItems);
            m_SongsOnSelectedAlbumsView.SelectedIndex = -1;
        }

        private void OnSelectedSongsOnSelectedAlbumsBySelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedSongsOnSelectedAlbumsBySelectedArtists = Utils.ToTypedList<SongMetadata>(m_SongsOnSelectedAlbumsView.SelectedItems);
        }

        private void OnSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedGenres = Utils.ToTypedList<string>(m_GenresView.SelectedItems);
            m_AlbumsOfSelectedGenresView.SelectedIndex = -1;
        }

        private void OnSelectedAlbumsOfSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedAlbumsOfSelectedGenres = Utils.ToTypedList<AlbumMetadata>(m_AlbumsOfSelectedGenresView.SelectedItems);
            m_SongsOnSelectedAlbumsView.SelectedIndex = -1;
        }

        private void OnSelectedSongsOnSelectedAlbumsOfSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.DatabaseView.SelectedSongsOnSelectedAlbumsOfSelectedGenres = Utils.ToTypedList<SongMetadata>(m_SongsOnSelectedGenreAlbumsView.SelectedItems);
        }

        private void OnSelectedSavedPlaylistChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.SavedPlaylists.SelectedPlaylist = m_SavedPlaylistsView.SelectedItem as string;
        }

        private void OnSelectedPlaylistItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            DataModel.Playlist.SelectedItems = Utils.ToTypedList<PlaylistItem>(m_PlaylistView.SelectedItems);
        }

        #endregion

        #region Whole window operations

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.MediaPreviousTrack)
                {
                    Back();
                    e.Handled = true;
                }
                else if (e.Key == Key.Space && !m_SearchBox.IsFocused && m_StringQueryOverlay.Visibility != Visibility.Visible && !AutoSearchInProgrss)
                {
                    TogglePlayPause();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaPlayPause)
                {
                    TogglePlayPause();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaStop)
                {
                    Stop();
                    e.Handled = true;
                }
                else if (e.Key == Key.MediaNextTrack)
                {
                    Skip();
                    e.Handled = true;
                }
                else if (e.Key == Key.VolumeDown)
                {
                    VolumeDown();
                    e.Handled = true;
                }
                else if (e.Key == Key.VolumeUp)
                {
                    VolumeUp();
                    e.Handled = true;
                }
            }
        }

        private void OnExit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataModel.CollectionSearch.Terminate();
            Disconnect();

            if (m_SettingsWindow != null)
            {
                m_SettingsWindow.Close();
                m_SettingsWindow = null;
            }

            if (m_LicenseWindow != null)
            {
                m_LicenseWindow.Close();
                m_LicenseWindow = null;
            }

            if (m_AboutWindow != null)
            {
                m_AboutWindow.Close();
                m_AboutWindow = null;
            }
        }

        #endregion

        #region Main menu

        private void OnEditSettingsClicked(object sender, RoutedEventArgs e)
        {
            BringUpSettingsWindow();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnConnectClicked(object sender, RoutedEventArgs e)
        {
            SetOnlineMode(true);
        }

        private void OnDisconnectClicked(object sender, RoutedEventArgs e)
        {
            SetOnlineMode(false);
        }

        private void OnResetConnectionClicked(object sender, RoutedEventArgs e)
        {
            if (m_OnlineMode)
            {
                SetOnlineMode(false);
            }

            SetOnlineMode(true);
        }

        private void OnEnableDisbaleOutput(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            Output output = checkBox.DataContext as Output;

            if (output.IsEnabled)
            {
                Protocol.DisableOutput(DataModel.ServerConnection, output.Index);
            }
            else
            {
                Protocol.EnableOutput(DataModel.ServerConnection, output.Index);
            }

            Update();
        }

        private void OnViewLicenseClicked(object sender, RoutedEventArgs e)
        {
            BringUpLicenseWindow();
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            BringUpAboutWindow();
        }

        private void OnNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

        #region Music collection and playlist

        #region Key, mouse and menu events common to multiple controls

        private void OnMusicCollectionDataGridKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    DataGrid grid = sender as DataGrid;
                    bool stringsAreArtists = grid == m_ArtistsView;

                    foreach (object item in grid.SelectedItems)
                    {
                        AddObjectToPlaylist(item, stringsAreArtists);
                    }

                    Update();
                    e.Handled = true;
                }
                else if (sender == m_StreamsView)
                {
                    OnStreamsViewKeyDown(sender, e);
                }
            }
        }

        private void OnCollectionTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Handled && e.Text != null && e.Text.Length == 1)
            {
                CollectionAutoSearch(sender, e.Text[0]);
            }
        }

        private void OnMusicCollectionDataGridDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                DataGrid grid = sender as DataGrid;
                DataGridRow row = DataGridRowBeingClicked(grid, e);
                bool stringsAreArtists = grid == m_ArtistsView;

                if (row != null)
                {
                    AddObjectToPlaylist(row.Item, stringsAreArtists);
                    Update();
                    e.Handled = true;
                }
            }
        }

        public void OnAddToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            ContextMenu menu = item.Parent as ContextMenu;
            UIElement element = menu.PlacementTarget;

            if (element is DataGrid)
            {
                DataGrid list = element as DataGrid;
                bool selectionMayIncludeArtists = list != m_GenresView;

                foreach (object o in list.SelectedItems)
                {
                    AddObjectToPlaylist(o, selectionMayIncludeArtists);
                }
            }
            else if (element is TreeView)
            {
                TreeViewController controller = TreeViewControllerOf(element as TreeView);
                ISet<SongMetadataTreeViewNode> selection = controller.Songs;

                if (selection != null)
                {
                    foreach (SongMetadataTreeViewNode node in selection)
                    {
                        AddSongToPlaylist(node.Song);
                    }
                }
            }
        }

        public void OnRescanMusicCollectionClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Update(DataModel.ServerConnection);
        }

        #endregion

        #region List selection and drag & drop

        private void OnDataGridRowMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            dataGrid.Focus();

            if (e.ClickCount == 1)
            {
                DataGridRow row = DataGridRowBeingClicked(dataGrid, e);

                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (row == null)
                    {
                        dataGrid.UnselectAll();
                        dataGrid.CurrentItem = null;
                    }
                    else
                    {
                        if (row.IsSelected)
                        {
                            e.Handled = true;
                        }
                        else
                        {
                            dataGrid.UnselectAll();
                            row.IsSelected = true;
                        }

                        dataGrid.CurrentItem = row.Item;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    if (row != null)
                    {
                        if (dataGrid.CurrentItem == null)
                        {
                            dataGrid.UnselectAll();
                            row.IsSelected = true;
                            dataGrid.CurrentItem = row.Item;
                        }
                        else
                        {
                            int startIndex = (dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.CurrentItem) as DataGridRow).GetIndex();
                            int endIndex = row.GetIndex();
                            dataGrid.UnselectAll();

                            int minIndex = Math.Min(startIndex, endIndex);
                            int maxIndex = Math.Max(startIndex, endIndex);

                            for (int i = minIndex; i <= maxIndex; ++i)
                            {
                                (dataGrid.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow).IsSelected = true;
                            }
                        }
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (row != null)
                    {
                        dataGrid.CurrentItem = row.Item;
                        row.IsSelected = !row.IsSelected;
                        e.Handled = true;
                    }
                }

                if (row != null && row.IsSelected)
                {
                    m_DragSource = dataGrid;
                    m_DragStartPosition = e.GetPosition(null);
                }
            }
        }

        private void OnDataGridRowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DataGrid dataGrid = sender as DataGrid;

                if (dataGrid.SelectionMode == DataGridSelectionMode.Extended)
                {
                    DataGridRow row = DataGridRowBeingClicked(dataGrid, e);

                    if (row != null && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        dataGrid.UnselectAll();
                        dataGrid.SelectedItem = dataGrid.CurrentItem;
                    }
                }

                m_DragSource = null;
                m_DragStartPosition = null;
            }
        }

        private void OnDataGridCellMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            dataGrid.Focus();
            
            if (e.ClickCount == 1)
            {
                DataGridCell cell = DataGridCellBeingClicked(dataGrid, e);

                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    if (cell == null)
                    {
                        dataGrid.UnselectAllCells();
                        dataGrid.CurrentCell = new DataGridCellInfo();
                    }
                    else
                    {
                        if (cell.IsSelected)
                        {
                            e.Handled = true;
                        }
                        else
                        {
                            dataGrid.UnselectAllCells();
                            cell.IsSelected = true;
                        }

                        dataGrid.CurrentCell = new DataGridCellInfo(cell);
                        cell.IsSelected = true;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    if (cell != null)
                    {
                        if (!dataGrid.CurrentCell.IsValid)
                        {
                            dataGrid.UnselectAllCells();
                            cell.IsSelected = true;
                            dataGrid.CurrentCell = new DataGridCellInfo(cell);
                        }
                        else
                        {
                            int startRowIndex = (dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.CurrentCell.Item) as DataGridRow).GetIndex();
                            int endRowIndex = (dataGrid.ItemContainerGenerator.ContainerFromItem(new DataGridCellInfo(cell).Item) as DataGridRow).GetIndex();
                            int startColumnIndex = dataGrid.CurrentCell.Column.DisplayIndex;
                            int endColumnIndex = cell.Column.DisplayIndex;

                            dataGrid.UnselectAllCells();

                            int minRowIndex = Math.Min(startRowIndex, endRowIndex);
                            int maxRowIndex = Math.Max(startRowIndex, endRowIndex);
                            int minColumnIndex = Math.Min(startColumnIndex, endColumnIndex);
                            int maxColumnIndex = Math.Max(startColumnIndex, endColumnIndex);

                            for (int y = minRowIndex; y <= maxRowIndex; ++y)
                            {
                                DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex(y) as DataGridRow;

                                for (int x = minColumnIndex; x <= maxColumnIndex; ++x)
                                {
                                    (dataGrid.ColumnFromDisplayIndex(x).GetCellContent(row).Parent as DataGridCell).IsSelected = true;
                                }
                            }
                        }
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (cell != null)
                    {
                        cell.IsSelected = !cell.IsSelected;
                    }
                }

                if (cell != null && cell.IsSelected)
                {
                    m_DragSource = dataGrid;
                    m_DragStartPosition = e.GetPosition(null);
                }

                e.Handled = cell != null;
            }
        }

        private void OnDataGridCellMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DataGrid dataGrid = sender as DataGrid;
                DataGridCell cell = DataGridCellBeingClicked(dataGrid, e);

                if (cell != null && Keyboard.Modifiers == ModifierKeys.None)
                {
                    dataGrid.UnselectAllCells();
                    cell.IsSelected = true;
                }

                m_DragSource = null;
                m_DragStartPosition = null;
                e.Handled = cell != null;
            }
        }

        private void OnMouseMoveDragDrop(object sender, MouseEventArgs e)
        {
            if (m_DragStartPosition.HasValue && m_DragSource != null)
            {
                Vector dragDistance = e.GetPosition(null) - m_DragStartPosition.Value;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(dragDistance.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(dragDistance.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Using an internal m_DragDropPayload is cheating, but the
                    // the standard (through clipboard?) system is pure evil and
                    // since we don't need drag & drop across application borders,
                    // this will do for now.
                    IList<object> payload = new List<object>();

                    if (m_DragSource == m_SearchResultsView)
                    {
                        IList<object> itemsToAdd = new List<object>();

                        foreach (DataGridCellInfo cell in m_SearchResultsView.SelectedCells)
                        {
                            itemsToAdd.Add(SearchResultCellContent(cell));
                        }

                        ISet<string> artistsToAdd = new SortedSet<string>();
                        ISet<AlbumMetadata> albumsToAdd = new SortedSet<AlbumMetadata>();
                        ISet<SongMetadata> songsToAdd = new SortedSet<SongMetadata>();

                        foreach (object item in itemsToAdd)
                        {
                            if (item is string)
                            {
                                artistsToAdd.Add(item as string);
                            }
                            else if (item is AlbumMetadata)
                            {
                                albumsToAdd.Add(item as AlbumMetadata);
                            }
                            else if (item is SongMetadata)
                            {
                                songsToAdd.Add(item as SongMetadata);
                            }
                        }

                        // Make an effort to not add anything twice, even if it is selected
                        // multiple times directly (as in the same artist on multiple lines)
                        // or indirectly (as in a song and the album to which it belongs).
                        foreach (AlbumMetadata album in new SortedSet<AlbumMetadata>(albumsToAdd))
                        {
                            if (artistsToAdd.Contains(album.Artist))
                            {
                                albumsToAdd.Remove(album);
                            }
                        }

                        foreach (SongMetadata song in new SortedSet<SongMetadata>(songsToAdd))
                        {
                            if (artistsToAdd.Contains(song.Artist) || albumsToAdd.Contains(DataModel.Database.AlbumOfSong(song)))
                            {
                                songsToAdd.Remove(song);
                            }
                        }

                        ISet<string> artistsAlreadyAdded = new SortedSet<string>();
                        ISet<AlbumMetadata> albumsAlreadyAdded = new SortedSet<AlbumMetadata>();

                        foreach (object item in itemsToAdd)
                        {
                            if (item is string)
                            {
                                string artist = item as string;

                                if (artistsToAdd.Contains(artist) && !artistsAlreadyAdded.Contains(artist))
                                {
                                    payload.Add(item);
                                    artistsAlreadyAdded.Add(artist);
                                }
                            }
                            else if (item is AlbumMetadata)
                            {
                                AlbumMetadata album = item as AlbumMetadata;

                                if (albumsToAdd.Contains(album) && !albumsAlreadyAdded.Contains(album))
                                {
                                    payload.Add(item);
                                    albumsAlreadyAdded.Add(album);
                                }
                            }
                            else if (item is SongMetadata)
                            {
                                SongMetadata song = item as SongMetadata;

                                if (songsToAdd.Contains(song))
                                {
                                    payload.Add(item);
                                }
                            }
                        }
                    }
                    else if (m_DragSource is DataGrid)
                    {
                        DataGrid source = m_DragSource as DataGrid;

                        foreach (object o in source.SelectedItems)
                        {
                            payload.Add(o);
                        }
                    }
                    else if (m_DragSource is TreeView)
                    {
                        TreeViewController controller = TreeViewControllerOf(m_DragSource as TreeView);
                        ISet<SongMetadataTreeViewNode> selection = controller.Songs;

                        if (selection != null)
                        {
                            foreach (SongMetadataTreeViewNode node in selection)
                            {
                                payload.Add(node.Song);
                            }
                        }
                    }

                    if (payload.Count > 0)
                    {
                        DragDropEffects mode = sender == m_PlaylistView ? DragDropEffects.Move : DragDropEffects.Copy;
                        m_DragDropPayload = payload;
                        m_DragDropData = GetDragDropDataString(m_DragSource);
                        m_MousePointerHint.Content = DragDropPayloadDescription();
                        DragDrop.DoDragDrop((DependencyObject)sender, m_DragDropData, mode);
                    }

                    m_DragStartPosition = null;
                }
            }
        }

        private string DragDropPayloadDescription()
        {
            const int maxLines = 4;
            int nameLines = m_DragDropPayload.Count <= maxLines ? m_DragDropPayload.Count : maxLines - 1;

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < nameLines; ++i)
            {
                if (i > 0)
                {
                    result.Append("\n");
                }

                object item = m_DragDropPayload[i];

                if (item is string)
                {
                    result.Append(item as string);
                }
                else if (item is AlbumMetadata)
                {
                    result.Append((item as AlbumMetadata).Title);
                }
                else if (item is Playable)
                {
                    result.Append((item as Playable).Title);
                }
            }

            if (m_DragDropPayload.Count > nameLines)
            {
                result.Append("\n+" + (m_DragDropPayload.Count - nameLines) + " more...");
            }

            return result.ToString();
        }

        private void OnPlaylistViewDragOver(object sender, DragEventArgs e)
        {
            if (m_DragDropPayload != null)
            {
                m_MousePointerHint.IsOpen = true;
                m_MousePointerHint.Visibility = Visibility.Visible;

                if (!m_PlaylistView.Items.IsEmpty)
                {
                    if (m_DragDropData != LoadPlaylist)
                    {
                        Point mousePosition = e.GetPosition(m_PlaylistView);

                        m_MousePointerHint.Placement = PlacementMode.Relative;
                        m_MousePointerHint.PlacementTarget = m_PlaylistView;
                        m_MousePointerHint.HorizontalOffset = mousePosition.X + 10;
                        m_MousePointerHint.VerticalOffset = mousePosition.Y - 6;

                        int targetRow = DropTargetRowIndex(e);
                        DataGridRow row = null;

                        if (targetRow >= 0)
                        {
                            row = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(targetRow) as DataGridRow;
                        }

                        if (row == null)
                        {
                            DataGridRow lastItem = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(m_PlaylistView.Items.Count - 1) as DataGridRow;

                            if (lastItem == null)
                            {
                                // TODO: this is null sometimes. No idea why.
                                return;
                            }

                            Rect bounds = VisualTreeHelper.GetDescendantBounds(lastItem);
                            GeneralTransform transform = lastItem.TransformToAncestor(m_PlaylistView);
                            Point bottomOfItem = transform.Transform(bounds.BottomLeft);
                            m_DropPositionIndicator.Y1 = bottomOfItem.Y;
                        }
                        else
                        {
                            Rect bounds = VisualTreeHelper.GetDescendantBounds(row);
                            GeneralTransform transform = row.TransformToAncestor(m_PlaylistView);
                            Point topOfItem = transform.Transform(bounds.TopLeft);
                            m_DropPositionIndicator.Y1 = topOfItem.Y;
                        }

                        m_DropPositionIndicator.X1 = 0;
                        m_DropPositionIndicator.X2 = m_PlaylistView.ActualWidth;
                        m_DropPositionIndicator.Y2 = m_DropPositionIndicator.Y1;
                        m_DropPositionIndicator.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                m_MousePointerHint.IsOpen = false;
                m_MousePointerHint.Visibility = Visibility.Hidden;
                m_DropPositionIndicator.Visibility = Visibility.Hidden;
            }
        }

        private void OnPlaylistViewDragLeave(object sender, DragEventArgs e)
        {
            m_MousePointerHint.IsOpen = false;
            m_MousePointerHint.Visibility = Visibility.Hidden;
            m_DropPositionIndicator.Visibility = Visibility.Hidden;
        }

        private void OnPlaylistViewDrop(object sender, DragEventArgs e)
        {
            if (m_DragDropPayload != null)
            {
                int targetRow = DropTargetRowIndex(e);
                string data = (string)e.Data.GetData(typeof(string));

                if (data == LoadPlaylist)
                {
                    LoadSavedPlaylist(m_DragDropPayload[0] as string);
                }
                else if (data == MovePlaylistItems)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        PlaylistItem item = (PlaylistItem)o;

                        if (item.Position < targetRow)
                        {
                            Protocol.MoveId(DataModel.ServerConnection, item.Id, targetRow - 1);
                        }
                        else
                        {
                            Protocol.MoveId(DataModel.ServerConnection, item.Id, targetRow++);
                        }
                    }
                }
                else if (data == AddGenres)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        targetRow = AddObjectToPlaylist(o, false, targetRow);
                    }
                }
                else if (data == AddSearchResults || data == AddArtists || data == AddAlbums || data == AddSongs || data == AddStreams || data == AddPlaylistItems)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        targetRow = AddObjectToPlaylist(o, true, targetRow);
                    }
                }

                m_DragDropPayload = null;
                m_DragDropData = null;
                Update();
            }

            m_MousePointerHint.IsOpen = false;
            m_MousePointerHint.Visibility = Visibility.Hidden;
            m_DropPositionIndicator.Visibility = Visibility.Hidden;
        }

        #endregion

        #region TreeView handling (browsing, selection, drag & drop)

        private void OnTreeViewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeView tree = sender as TreeView;
            TreeViewItem item = TreeViewItemBeingClicked(tree, e);
            tree.Focus();

            if (item == null)
            {
                if (!TreeViewExpanderBeingClicked(tree, e))
                {
                    (tree.Tag as TreeViewController).ClearMultiSelection();
                }
            }
            else if (item.Header is TreeViewNode)
            {
                TreeViewNode node = item.Header as TreeViewNode;

                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    node.Controller.Current = node;
                    node.Controller.Pivot = node;

                    if (!node.IsMultiSelected)
                    {
                        node.Controller.ClearMultiSelection();
                        node.IsMultiSelected = true;
                    }
                    else if (e.ClickCount == 1)
                    {
                        m_DragSource = sender;
                        m_DragStartPosition = e.GetPosition(null);
                    }
                    else
                    {
                        foreach (SongMetadataTreeViewNode leaf in node.Controller.Songs)
                        {
                            AddSongToPlaylist(leaf.Song);
                        }

                        Update();
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    node.Controller.Current = node;
                    node.IsMultiSelected = !node.IsMultiSelected;
                    node.Controller.Pivot = node.IsMultiSelected ? node : null;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    node.Controller.Current = node;
                    node.Controller.ClearMultiSelection();

                    if (node.Controller.Pivot == null)
                    {
                        node.IsMultiSelected = true;
                        node.Controller.Pivot = node;
                    }
                    else
                    {
                        node.Controller.SelectRange(node);
                    }
                }

                if (e.ClickCount == 1 && node.IsMultiSelected)
                {
                    m_DragSource = sender;
                    m_DragStartPosition = e.GetPosition(null);
                }

                e.Handled = true;
            }
        }

        private void OnTreeViewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.None)
            {
                TreeViewItem item = TreeViewItemBeingClicked(sender as TreeView, e);

                if (item != null && item.Header is TreeViewNode)
                {
                    TreeViewNode node = item.Header as TreeViewNode;
                    node.Controller.ClearMultiSelection();
                    node.IsMultiSelected = true;
                    node.Controller.Pivot = node;
                }
            }
        }

        private void OnTreeViewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                TreeView tree = sender as TreeView;
                TreeViewController controller = tree.Tag as TreeViewController;

                if (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    bool currentChanged = false;

                    if (e.Key == Key.Up && EnsureTreeViewHasCurrentNode(controller))
                    {
                        controller.Current = controller.Previous;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Down && EnsureTreeViewHasCurrentNode(controller))
                    {
                        controller.Current = controller.Next;
                        currentChanged = true;
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Left && EnsureTreeViewHasCurrentNode(controller))
                    {
                        if (controller.Current.Parent != null)
                        {
                            controller.Current = controller.Current.Parent;
                            currentChanged = true;
                        }

                        e.Handled = true;
                    }
                    else if (e.Key == Key.Right && EnsureTreeViewHasCurrentNode(controller))
                    {
                        if (controller.Current.Children.Count > 0)
                        {
                            controller.Current.IsExpanded = true;
                            controller.Current = controller.Current.Children[0];
                            currentChanged = true;
                            e.Handled = true;
                        }
                    }
                    else if (e.Key == Key.Enter)
                    {
                        foreach (SongMetadataTreeViewNode leaf in controller.Songs)
                        {
                            AddSongToPlaylist(leaf.Song);
                        }

                        Update();
                        e.Handled = true;
                    }

                    if (currentChanged)
                    {
                        TreeViewItem item = GetTreeViewItem(tree, controller.Current);

                        if (item != null)
                        {
                            item.BringIntoView();
                        }

                        if (Keyboard.Modifiers == ModifierKeys.None)
                        {
                            controller.ClearMultiSelection();
                            controller.Current.IsMultiSelected = true;
                            controller.Pivot = controller.Current;
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Shift)
                        {
                            if (controller.Pivot == null)
                            {
                                controller.ClearMultiSelection();
                                controller.Current.IsMultiSelected = true;
                                controller.Pivot = controller.Current;
                            }
                            else
                            {
                                controller.ClearMultiSelection();
                                controller.SelectRange(controller.Current);
                            }
                        }
                    }
                }
            }
        }

        private void OnTreeViewSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Cancel the selection. Use the controller multiselection system instead.
            TreeViewNode node = e.NewValue as TreeViewNode;

            if (node != null)
            {
                node.IsSelected = false;
            }
        }

        #endregion

        #region Specialized events for individual controls

        private void OnSearchResultsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            foreach (object o in SearchResultsCellsToObjects())
            {
                AddObjectToPlaylist(o, true);
            }
        }

        private void OnSearchBoxEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (m_SearchBox.IsEnabled)
            {
                m_SearchBox.Focus();
            }
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            DataModel.CollectionSearch.SearchString = m_SearchBox.Text;
        }

        #region Streams collection

        private void OnStreamsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2 && m_StreamsView.SelectedItems.Count == 1)
            {
                OnRenameSelectedStream();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedStreams();
                e.Handled = true;
            }
        }

        private void OnRenameSelectedStreamClicked(object sender, RoutedEventArgs e)
        {
            OnRenameSelectedStream();
        }

        private void OnRenameStreamQueryFinished(bool succeeded, StreamMetadata stream, string newName)
        {
            if (succeeded)
            {
                DataModel.StreamsCollection.Rename(stream, newName);
            }
        }

        private void OnDeleteSelectedStreamsClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedStreams();
        }

        private void OnAddStreamURLClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewStreamQuery();
        }

        private void OnAddNewStreamQueryFinished(bool succeeded, string address, string name)
        {
            if (succeeded)
            {
                StreamMetadata stream = new StreamMetadata(address, name);
                DataModel.StreamsCollection.Add(stream);
            }
        }

        private void OnAddStreamsFromFileClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Add stream files";
            dialog.Multiselect = true;
            dialog.Filter = "Playlist Files|*.pls;*.m3u";

            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                PLSParser plsParser = new PLSParser();
                M3UParser m3uParser = new M3UParser();
                List<StreamMetadata> streamsToAdd = new List<StreamMetadata>();

                foreach (string filename in dialog.FileNames)
                {
                    IEnumerable<StreamMetadata> streams = null;

                    if (filename.ToLowerInvariant().EndsWith(".pls"))
                    {
                        streams = plsParser.ParseFile(filename);
                    }
                    else if (filename.ToLowerInvariant().EndsWith(".m3u"))
                    {
                        streams = m3uParser.ParseFile(filename);
                    }
                    
                    if (streams != null)
                    {
                        streamsToAdd.AddRange(streams);
                    }
                }

                DataModel.StreamsCollection.Add(streamsToAdd);
            }
        }

        private void OnSaveSelectedStreamsToFileClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save streams";
            dialog.Filter = "Playlist Files|*.pls";

            bool? dialogResult = dialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                string filename = dialog.FileName;
                string playlist = PlaylistWriter.Write(Utils.ToTypedList<StreamMetadata>(m_StreamsView.SelectedItems));

                if (playlist != null)
                {
                    File.WriteAllText(filename, playlist);
                }
            }
        }

        private void OnRenameSelectedStream()
        {
            if (m_StreamsView.SelectedItems.Count == 1)
            {
                StartRenameStreamQuery(m_StreamsView.SelectedItem as StreamMetadata);
            }
        }

        private void DeleteSelectedStreams()
        {
            DataModel.StreamsCollection.Delete(Utils.ToTypedList<StreamMetadata>(m_StreamsView.SelectedItems));
        }

        #endregion

        #region Saved playlists collection

        private void OnSavedPlaylistsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadSelectedSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                RenameSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelectedSavedPlaylist();
                e.Handled = true;
            }
        }

        private void OnSavedPlaylistsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                LoadSelectedSavedPlaylist();
            }
        }

        private void OnSendSavedPlaylistToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            LoadSelectedSavedPlaylist();
        }

        private void LoadSelectedSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                LoadSavedPlaylist(selectedPlaylist as string);
            }
        }

        private void LoadSavedPlaylist(string name)
        {
            Protocol.Clear(DataModel.ServerConnection);
            Protocol.Load(DataModel.ServerConnection, name);
            DataModel.SavedPlaylists.CurrentPlaylistName = name;
            Update();
        }

        private void OnRenameSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            RenameSavedPlaylist();
        }

        private void RenameSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                StartRenameSavedPlaylistQuery(selectedPlaylist as string);
            }
        }

        private void OnRenameStreamQueryFinished(bool succeeded, string oldName, string newName)
        {
            if (succeeded)
            {
                Protocol.Rename(DataModel.ServerConnection, oldName, newName);
                DataModel.SavedPlaylists.Refresh(DataModel.ServerConnection);
            }
        }

        private void OnDeleteSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedSavedPlaylist();
        }

        private void DeleteSelectedSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                Protocol.Rm(DataModel.ServerConnection, selectedPlaylist as string);
                DataModel.SavedPlaylists.Refresh(DataModel.ServerConnection);
            }
        }

        #endregion

        #endregion

        #region Playlist

        private void OnPlaylistViewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    if (m_PlaylistView.SelectedItems.Count == 1)
                    {
                        object o = m_PlaylistView.SelectedItems[0];

                        if (o is PlaylistItem)
                        {
                            PlaylistItem item = o as PlaylistItem;
                            Protocol.PlayId(DataModel.ServerConnection, item.Id);
                            Update();
                        }

                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    DeleteSelectedItemsFromPlaylist();
                    e.Handled = true;
                }
            }
        }

        private void OnPlaylistViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridRowBeingClicked(m_PlaylistView, e);

            if (row != null)
            {
                PlaylistItem item = row.Item as PlaylistItem;
                Protocol.PlayId(DataModel.ServerConnection, item.Id);
                Update();
            }
        }

        private void OnClearPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Clear(DataModel.ServerConnection);
            DataModel.SavedPlaylists.CurrentPlaylistName = "";
            Update();
        }

        private void OnRemoveSelectedPlaylistItemsClicked(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItemsFromPlaylist();
        }

        private void OnCropToSelectedPlaylistItemsClicked(object sender, RoutedEventArgs e)
        {
            if (m_PlaylistView.SelectedItems.Count > 0)
            {
                ISet<int> keepItems = new SortedSet<int>();

                foreach (Object o in m_PlaylistView.SelectedItems)
                {
                    PlaylistItem item = o as PlaylistItem;
                    keepItems.Add(item.Id);
                }

                foreach (PlaylistItem item in DataModel.Playlist.Items)
                {
                    if (!keepItems.Contains(item.Id))
                    {
                        Protocol.DeleteId(DataModel.ServerConnection, item.Id);
                    }
                }

                Update();
            }
        }

        private void OnSavePlaylistAsClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewPlaylistAsQuery(DataModel.SavedPlaylists.CurrentPlaylistName);
        }

        private void OnAddNewPlaylistAsQueryFinished(bool succeeded, string playlistName)
        {
            if (succeeded)
            {
                DataModel.SavedPlaylists.CurrentPlaylistName = playlistName;
                Protocol.Rm(DataModel.ServerConnection, DataModel.SavedPlaylists.CurrentPlaylistName);
                Protocol.Save(DataModel.ServerConnection, DataModel.SavedPlaylists.CurrentPlaylistName);
                DataModel.SavedPlaylists.Refresh(DataModel.ServerConnection);
            }
        }
        
        private void OnDedupPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            ISet<string> songPathsOnPlaylist = new SortedSet<string>();
            IList<int> playlistIDsOfDuplicates = new List<int>();

            foreach (PlaylistItem item in DataModel.Playlist.Items)
            {
                if (!songPathsOnPlaylist.Add(item.Playable.Path))
                {
                    playlistIDsOfDuplicates.Add(item.Id);
                }
            }

            foreach (int id in playlistIDsOfDuplicates)
            {
                Protocol.DeleteId(DataModel.ServerConnection, id);
            }
        }

        private void OnShufflePlaylistClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Shuffle(DataModel.ServerConnection);
        }

        private void DeleteSelectedItemsFromPlaylist()
        {
            foreach (object o in m_PlaylistView.SelectedItems)
            {
                if (o is PlaylistItem)
                {
                    PlaylistItem item = o as PlaylistItem;
                    Protocol.DeleteId(DataModel.ServerConnection, item.Id);
                }
            }

            Update();
        }

        #endregion

        #region Autosearch

        private bool AutoSearchInProgrss
        {
            get
            {
                return m_AutoSearchString.Length > 0 && DateTime.Now.Subtract(m_TimeOfLastAutoSearch).TotalMilliseconds <= m_AutoSearchMaxKeystrokeGap;
            }
        }

        private bool CollectionAutoSearch(object sender, char c)
        {
            if (sender != m_LastAutoSearchSender || DateTime.Now.Subtract(m_TimeOfLastAutoSearch).TotalMilliseconds > m_AutoSearchMaxKeystrokeGap)
            {
                m_AutoSearchString = "";
            }

            m_TimeOfLastAutoSearch = DateTime.Now;
            m_LastAutoSearchSender = sender;
            bool searchAgain = false;

            if (c == '\b')
            {
                if (m_AutoSearchString.Length > 0)
                {
                    m_AutoSearchString = m_AutoSearchString.Remove(m_AutoSearchString.Length - 1);
                    searchAgain = m_AutoSearchString.Length > 0;
                }
            }
            else if (!char.IsControl(c) && !char.IsSurrogate(c))
            {
                m_AutoSearchString = (m_AutoSearchString + c).ToLowerInvariant();
                searchAgain = true;
            }
            else
            {
                m_TimeOfLastAutoSearch = DateTime.MinValue;
            }

            if (searchAgain)
            {
                if (sender is DataGrid)
                {
                    DataGrid grid = sender as DataGrid;

                    foreach (object o in grid.Items)
                    {
                        if (o is string && (o as string).ToLowerInvariant().StartsWith(m_AutoSearchString) ||
                           o is AlbumMetadata && (o as AlbumMetadata).Title.ToLowerInvariant().StartsWith(m_AutoSearchString) ||
                           o is Playable && (o as Playable).Title.ToLowerInvariant().StartsWith(m_AutoSearchString))
                        {
                            grid.CurrentItem = o;
                            grid.SelectedItem = o;
                            grid.ScrollIntoView(o);
                            return true;
                        }
                    }
                }
                else if (sender is TreeView)
                {
                    TreeView tree = sender as TreeView;
                    TreeViewNode item = CollectionAutoSearchTreeViewRecursively(Utils.ToTypedList<TreeViewNode>(tree.Items));

                    if (item != null)
                    {
                        item.Controller.ClearMultiSelection();
                        item.IsMultiSelected = true;
                        item.IsSelected = true;
                        item.Controller.Current = item;
                        item.Controller.Pivot = item;

                        return true;
                    }
                }
            }

            return false;
        }

        private TreeViewNode CollectionAutoSearchTreeViewRecursively(IEnumerable<TreeViewNode> nodes)
        {
            foreach (TreeViewNode node in nodes)
            {
                if (node.DisplayString.ToLowerInvariant().StartsWith(m_AutoSearchString))
                {
                    return node;
                }
                else if (node.IsExpanded)
                {
                    TreeViewNode result = CollectionAutoSearchTreeViewRecursively(Utils.ToTypedList<TreeViewNode>(node.Children));

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Helpers for adding items to the playlist

        private void AddObjectToPlaylist(object o, bool stringsAreArtists)
        {
            if (o is string)
            {
                if (stringsAreArtists)
                {
                    AddArtistToPlaylist(o as string);
                }
                else
                {
                    AddGenreToPlaylist(o as string);
                }
            }
            else if (o is AlbumMetadata)
            {
                AddAlbumToPlaylist(o as AlbumMetadata);
            }
            else if (o is SongMetadata)
            {
                AddSongToPlaylist(o as SongMetadata);
            }
            else if (o is StreamMetadata)
            {
                AddStreamToPlaylist(o as StreamMetadata);
            }
        }

        // Template: firstPosition is the position on the playlist to which
        // the first item is pushed. The return value is the position after
        // the last item.
        private int AddObjectToPlaylist(object o, bool stringsAreArtists, int firstPosition)
        {
            if (o is string)
            {
                if (stringsAreArtists)
                {
                    return AddArtistToPlaylist(o as string, firstPosition);
                }
                else
                {
                    return AddGenreToPlaylist(o as string, firstPosition);
                }
            }
            else if (o is AlbumMetadata)
            {
                return AddAlbumToPlaylist(o as AlbumMetadata, firstPosition);
            }
            else if (o is SongMetadata)
            {
                return AddSongToPlaylist(o as SongMetadata, firstPosition);
            }
            else if (o is StreamMetadata)
            {
                return AddStreamToPlaylist(o as StreamMetadata, firstPosition);
            }

            return firstPosition;
        }

        private void AddArtistToPlaylist(string artist)
        {
            foreach (AlbumMetadata album in DataModel.Database.AlbumsByArtist(artist))
            {
                AddAlbumToPlaylist(album);
            }
        }

        private int AddArtistToPlaylist(string artist, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in DataModel.Database.AlbumsByArtist(artist))
            {
                position = AddAlbumToPlaylist(album, position);
            }

            return position;
        }

        private void AddGenreToPlaylist(string genre)
        {
            foreach (AlbumMetadata album in DataModel.Database.AlbumsByGenre(genre))
            {
                AddAlbumToPlaylist(album);
            }
        }

        private int AddGenreToPlaylist(string genre, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in DataModel.Database.AlbumsByGenre(genre))
            {
                position = AddAlbumToPlaylist(album, position);
            }

            return position;
        }

        private void AddAlbumToPlaylist(AlbumMetadata album)
        {
            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                AddSongToPlaylist(song);
            }
        }

        private int AddAlbumToPlaylist(AlbumMetadata album, int firstPosition)
        {
            int position = firstPosition;

            foreach (SongMetadata song in DataModel.Database.SongsByAlbum(album))
            {
                position = AddSongToPlaylist(song, position);
            }

            return position;
        }

        private void AddSongToPlaylist(SongMetadata song)
        {
            Protocol.Add(DataModel.ServerConnection, song.Path);
        }

        private int AddSongToPlaylist(SongMetadata song, int position)
        {
            Protocol.AddId(DataModel.ServerConnection, song.Path, position);
            return position + 1;
        }

        private void AddStreamToPlaylist(StreamMetadata stream)
        {
            Protocol.Add(DataModel.ServerConnection, stream.Path);
        }

        private int AddStreamToPlaylist(StreamMetadata stream, int position)
        {
            Protocol.AddId(DataModel.ServerConnection, stream.Path, position);
            return position + 1;
        }

        #endregion

        #endregion

        #region Seek bar

        private int m_SeekBarPositionFromUser = -1;

        private void OnSeekBarDragStart(object sender, MouseButtonEventArgs e)
        {
            m_SeekBarPositionFromUser = 0;
        }

        private void OnSeekBarDrag(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_SeekBarPositionFromUser >= 0)
            {
                // The user is dragging the position slider, instead of the
                // server just telling us what the current play position is.
                m_SeekBarPositionFromUser = (int)e.NewValue;
                m_PlayPosition.Content = Utils.IntToTimecode(m_SeekBarPositionFromUser);
            }
        }

        private void OnSeekBarDragEnd(object sender, MouseButtonEventArgs e)
        {
            if (m_SeekBarPositionFromUser >= 0)
            {
                Protocol.Seek(DataModel.ServerConnection, DataModel.ServerStatus.CurrentSongIndex, m_SeekBarPositionFromUser);
                m_SeekBarPositionFromUser = -1;
                Update();
            }
        }

        private void OnSeekBarMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int currentPosition = DataModel.ServerStatus.PlayPosition;
            int newPosition = currentPosition;
            int increment = 0;

            if (Settings.Default.MouseWheelAdjustsSongPositionInPercent && Settings.Default.MouseWheelAdjustsSongPositionPercentBy > 0)
            {
                increment = Math.Max(1, Settings.Default.MouseWheelAdjustsSongPositionPercentBy * DataModel.ServerStatus.SongLength / 100);
            }
            else
            {
                increment = Settings.Default.MouseWheelAdjustsSongPositionSecondsBy;
            }

            if (e.Delta < 0)
            {
                newPosition = Math.Max(0, newPosition - increment);
            }
            else if (e.Delta > 0)
            {
                newPosition = Math.Min(DataModel.ServerStatus.SongLength, newPosition + increment);
            }

            if (newPosition != currentPosition)
            {
                Protocol.Seek(DataModel.ServerConnection, DataModel.ServerStatus.CurrentSongIndex, newPosition);
                Update();
            }
        }

        #endregion

        #region Control buttons row

        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void OnPlayButtonClicked(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void OnPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void OnPlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private void OnStopButtonClicked(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void OnSkipButtonClicked(object sender, RoutedEventArgs e)
        {
            Skip();
        }

        bool m_VolumeRestoreInProgress = false;

        private void OnVolumeSliderDragged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!m_PropertyUpdateInProgress && !m_VolumeRestoreInProgress)
            {
                // Volume slider is actually moving because the user is moving it.
                ServerResponse response = Protocol.SetVol(DataModel.ServerConnection, (int)e.NewValue);

                if (response != null && response.IsACK)
                {
                    m_VolumeRestoreInProgress = true;
                    m_VolumeControl.Value = e.OldValue;
                    m_VolumeRestoreInProgress = false;
                }
            }
        }

        private void OnVolumeMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                VolumeDown();
            }
            else if (e.Delta > 0)
            {
                VolumeUp();
            }
        }

        private void OnToggleRandomClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Random(DataModel.ServerConnection, !DataModel.ServerStatus.IsOnRandom);
            Update();
        }

        private void OnToggleRepeatClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Repeat(DataModel.ServerConnection, !DataModel.ServerStatus.IsOnRepeat);
            Update();
        }

        #endregion

        #region Playback control

        private void Back()
        {
            Protocol.Previous(DataModel.ServerConnection);
            Update();
        }

        private void Play()
        {
            Protocol.Play(DataModel.ServerConnection);
            Update();
        }

        private void Pause()
        {
            Protocol.Pause(DataModel.ServerConnection);
            Update();
        }

        private void TogglePlayPause()
        {
            if (DataModel.ServerStatus != null && DataModel.ServerStatus.OK)
            {
                if (DataModel.ServerStatus.IsPlaying)
                {
                    Protocol.Pause(DataModel.ServerConnection);
                }
                else
                {
                    Protocol.Play(DataModel.ServerConnection);
                }
            }

            Update();
        }

        private void Stop()
        {
            Protocol.Stop(DataModel.ServerConnection);
            Update();
        }

        private void Skip()
        {
            Protocol.Next(DataModel.ServerConnection);
            Update();
        }
                
        private void VolumeDown()
        {
            int? currentVolume = DataModel.ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Max(0, currentVolume.Value - Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    Protocol.SetVol(DataModel.ServerConnection, newVolume);
                    Update();
                }
            }
        }
        
        private void VolumeUp()
        {
            int? currentVolume = DataModel.ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Min(100, currentVolume.Value + Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    Protocol.SetVol(DataModel.ServerConnection, newVolume);
                    Update();
                }
            }
        }

        #endregion

        #region Server Tab

        private void OnUpdateCollectionClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Update(DataModel.ServerConnection);
        }
        
        #endregion

        #region Settings and settings tab

        // Called by SettingsWindow when new settings are applied.
        public void SettingsChanged(bool reconnect)
        {
            ApplyTabVisibilitySettings();
            m_VolumeControl.IsEnabled = DataModel.ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            SetTimerInterval(Settings.Default.ViewUpdateInterval);

            if (reconnect)
            {
                Disconnect();

                if (m_OnlineMode)
                {
                    ConnectTo(Settings.Default.Server, Settings.Default.Port);
                }
            }
        }

        private void ApplyInitialSettings()
        {
            ApplyTabVisibilitySettings();
            SetOnlineMode(true);
        }

        private void ApplyTabVisibilitySettings()
        {
            m_SearchTab.Visibility = Settings.Default.SearchTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_ArtistListTab.Visibility = Settings.Default.ArtistListTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_ArtistTreeTab.Visibility = Settings.Default.ArtistTreeTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_GenreListTab.Visibility = Settings.Default.GenreListTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_GenreTreeTab.Visibility = Settings.Default.GenreTreeTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_FilesystemTab.Visibility = Settings.Default.FilesystemTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_StreamsTab.Visibility = Settings.Default.StreamsTabIsVisible ? Visibility.Visible : Visibility.Collapsed;
            m_PlaylistsTab.Visibility = Settings.Default.PlaylistsTabIsVisible ? Visibility.Visible : Visibility.Collapsed;

            if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.SearchTab.ToString())
            {
                m_SearchTab.Visibility = System.Windows.Visibility.Visible;
                m_SearchTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.ArtistListTab.ToString())
            {
                m_ArtistListTab.Visibility = System.Windows.Visibility.Visible;
                m_ArtistListTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.ArtistTreeTab.ToString())
            {
                m_ArtistTreeTab.Visibility = System.Windows.Visibility.Visible;
                m_ArtistTreeTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.GenreListTab.ToString())
            {
                m_GenreListTab.Visibility = System.Windows.Visibility.Visible;
                m_GenreListTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.GenreTreeTab.ToString())
            {
                m_GenreTreeTab.Visibility = System.Windows.Visibility.Visible;
                m_GenreTreeTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.FilesystemTab.ToString())
            {
                m_FilesystemTab.Visibility = System.Windows.Visibility.Visible;
                m_FilesystemTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.StreamsTab.ToString())
            {
                m_StreamsTab.Visibility = System.Windows.Visibility.Visible;
                m_StreamsTab.IsSelected = true;
            }
            else if (Settings.Default.DefaultMusicCollectionTab == MusicCollectionTab.PlaylistsTab.ToString())
            {
                m_PlaylistsTab.Visibility = System.Windows.Visibility.Visible;
                m_PlaylistsTab.IsSelected = true;
            }
        }

        #endregion

        #region String query overlay use cases

        #region Querying for a new stream address and name

        string m_NewStreamAddress;

        private void StartAddNewStreamQuery()
        {
            m_NewStreamAddress = "";
            EnterStringQueryOverlay("Enter the address of the new stream:", "http://", OnAddStreamAddressOverlayReturned);
        }
        
        private void OnAddStreamAddressOverlayReturned(bool okClicked, string streamAddress)
        {
            m_NewStreamAddress = streamAddress.Trim();

            if (okClicked && m_NewStreamAddress != "")
            {
                EnterStringQueryOverlay("Enter a name for this stream:", "", OnAddStreamNameOverlayReturned);
            }
            else
            {
                m_NewStreamAddress = "";
                OnAddNewStreamQueryFinished(false, null, null);
            }
        }

        private void OnAddStreamNameOverlayReturned(bool okClicked, string streamName)
        {
            string cleanedStreamName = streamName.Trim();
            string newStreamAddress = m_NewStreamAddress;
            m_NewStreamAddress = "";
            OnAddNewStreamQueryFinished(okClicked && cleanedStreamName != "", newStreamAddress, cleanedStreamName);
        }

        #endregion

        #region Querying for a new name for a stream

        private StreamMetadata m_RenameStream = null;

        private void StartRenameStreamQuery(StreamMetadata stream)
        {
            m_RenameStream = stream;
            EnterStringQueryOverlay("New stream name:", stream.Title, OnRenameStreamOverlayReturned);
        }

        private void OnRenameStreamOverlayReturned(bool okClicked, string streamName)
        {
            StreamMetadata renameStream = m_RenameStream;
            m_RenameStream = null;
            string trimmedName = streamName.Trim();
            OnRenameStreamQueryFinished(okClicked && trimmedName.Length > 0, renameStream, trimmedName);
        }

        #endregion

        #region Querying for a new playlist name

        private void StartAddNewPlaylistAsQuery(string currentPlaylistName)
        {
            EnterStringQueryOverlay("Save this playlist on the server as:", currentPlaylistName, OnSavePlaylistAsOverlayReturned);
        }

        private void OnSavePlaylistAsOverlayReturned(bool okClicked, string playlistName)
        {
            string trimmedName = playlistName.Trim();
            OnAddNewPlaylistAsQueryFinished(okClicked && trimmedName.Length > 0, trimmedName);
        }

        #endregion

        #region Querying for a new name for a saved playlist

        private string m_OldSavedPlaylistName = null;

        private void StartRenameSavedPlaylistQuery(string oldName)
        {
            m_OldSavedPlaylistName = oldName;
            EnterStringQueryOverlay("New playlist name:", oldName, OnRenameSavedPlaylistOverlayReturned);
        }

        private void OnRenameSavedPlaylistOverlayReturned(bool okClicked, string newName)
        {
            string oldName = m_OldSavedPlaylistName;
            m_OldSavedPlaylistName = null;
            string trimmedNewName = newName.Trim();
            OnRenameStreamQueryFinished(okClicked && trimmedNewName.Length > 0, oldName, trimmedNewName);
        }

        #endregion

        #endregion

        #region String query overlay implementation

        public delegate void StringQueryOverlayExitHandler(bool okClicked, string input);
        StringQueryOverlayExitHandler m_StringQueryOverlayExitHandler = null;

        private void EnterStringQueryOverlay(string caption, string defaultInput, StringQueryOverlayExitHandler handler)
        {
            m_StringQueryOverlayExitHandler = handler;
            m_StringQueryOverlayCaption.Text = caption;
            m_StringQueryOverlayInput.Text = defaultInput == null ? "" : defaultInput;
            m_StringQueryOverlay.Visibility = Visibility.Visible;
            m_StringQueryOverlayInput.CaretIndex = m_StringQueryOverlayInput.Text.Length;
            m_StringQueryOverlayInput.Focus();
        }

        private void ExitStringQueryOverlay()
        {
            m_StringQueryOverlay.Visibility = Visibility.Collapsed;
            m_StringQueryOverlayExitHandler = null;
        }

        private void OnStringQueryOverlayButtonClicked(object sender, RoutedEventArgs e)
        {
            StringQueryOverlayExitHandler currentHandler = m_StringQueryOverlayExitHandler;
            ExitStringQueryOverlay();

            if (currentHandler != null)
            {
                currentHandler(sender == m_StringQueryOverlayOK, m_StringQueryOverlayInput.Text);
            }
        }

        private void m_StringQueryOverlayBackgroundClicked(object sender, MouseButtonEventArgs e)
        {
            StringQueryOverlayExitHandler currentHandler = m_StringQueryOverlayExitHandler;
            ExitStringQueryOverlay();

            if (currentHandler != null)
            {
                currentHandler(false, m_StringQueryOverlayInput.Text);
            }

            e.Handled = true;
        }

        private void m_StringQueryOverlayForegroundClicked(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        #region Child window handling

        private void BringUpSettingsWindow()
        {
            if (m_SettingsWindow == null)
            {
                m_SettingsWindow = new SettingsWindow(this);
            }
            else
            {
                m_SettingsWindow.Visibility = Visibility.Visible;
            }

            m_SettingsWindow.Show();
        }

        private void BringUpLicenseWindow()
        {
            if (m_LicenseWindow == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("Auremo.Text.LICENSE.txt");
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string license = reader.ReadToEnd();

                m_LicenseWindow = new TextWindow("License - Auremo MPD Client", license, this);
            }
            else
            {
                m_LicenseWindow.Visibility = Visibility.Visible;
            }

            m_LicenseWindow.Show();
        }

        private void BringUpAboutWindow()
        {
            if (m_AboutWindow == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream("Auremo.Text.AUTHORS.txt");
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string about = reader.ReadToEnd();
                m_AboutWindow = new AboutWindow(this);
            }
            else
            {
                m_AboutWindow.Visibility = Visibility.Visible;
            }

            m_AboutWindow.Show();
        }

        public void OnChildWindowClosing(Window window)
        {
            if (window == m_AboutWindow)
            {
                m_AboutWindow = null;
            }
            else if (window == m_LicenseWindow)
            {
                m_LicenseWindow = null;
            }
            else if (window == m_SettingsWindow)
            {
                m_SettingsWindow = null;
            }
        }

        #endregion

        #region Miscellaneous helper functions

        private object SearchResultCellContent(DataGridCellInfo cell)
        {
            // Is this really the best way to find which column this is?
            // It seems contrived, but DataGridCellInfo seems to contain
            // very little usable information.
            const int songColumnDisplayIndex = 0;
            const int artistColumnDisplayIndex = 1;
            const int albumColumnDisplayIndex = 2;

            CollectionSearch.SearchResultTuple result = (CollectionSearch.SearchResultTuple)(cell.Item);

            if (result != null)
            {
                if (cell.Column.DisplayIndex == artistColumnDisplayIndex)
                {
                    return result.Artist;
                }
                else if (cell.Column.DisplayIndex == albumColumnDisplayIndex)
                {
                    return result.Album;
                }
                else if (cell.Column.DisplayIndex == songColumnDisplayIndex)
                {
                    return result.Song;
                }
            }

            return null;
        }
        
        private DataGridRow DataGridRowBeingClicked(DataGrid grid, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                while (component != null)
                {
                    if (component is DataGridRow)
                    {
                        return (DataGridRow)component;
                    }
                    else
                    {
                        component = VisualTreeHelper.GetParent(component);
                    }
                }
            }

            return null;
        }

        private DataGridCell DataGridCellBeingClicked(DataGrid grid, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                while (component != null)
                {
                    if (component is DataGridCell)
                    {
                        return (DataGridCell)component;
                    }
                    else
                    {
                        component = VisualTreeHelper.GetParent(component);
                    }
                }
            }

            return null;
        }

        private TreeViewItem TreeViewItemBeingClicked(TreeView tree, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(tree, e.GetPosition(tree));

            if (hit != null)
            {
                DependencyObject component = (DependencyObject)hit.VisualHit;

                if (component is TextBlock) // Don't return hits to the expander arrow.
                {
                    while (component != null)
                    {
                        if (component is TreeViewItem)
                        {
                            return (TreeViewItem)component;
                        }
                        else
                        {
                            component = VisualTreeHelper.GetParent(component);
                        }
                    }
                }
            }

            return null;
        }

        private bool TreeViewExpanderBeingClicked(TreeView tree, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(tree, e.GetPosition(tree));
            return hit != null && hit.VisualHit is System.Windows.Shapes.Path;
        }

        private IList<object> SearchResultsCellsToObjects()
        {
            // Is this really the best way to find which column this is?
            // It seems contrived, but DataGridCellInfo seems to contain
            // very little usable information.
            const int songColumnDisplayIndex = 0;
            const int artistColumnDisplayIndex = 1;
            const int albumColumnDisplayIndex = 2;

            IList<object> result = new List<object>();

            foreach (DataGridCellInfo cell in m_SearchResultsView.SelectedCells)
            {
                CollectionSearch.SearchResultTuple searchResult = (CollectionSearch.SearchResultTuple)(cell.Item);

                if (searchResult != null)
                {
                    if (cell.Column.DisplayIndex == artistColumnDisplayIndex)
                    {
                        result.Add(searchResult.Artist);
                    }
                    else if (cell.Column.DisplayIndex == albumColumnDisplayIndex)
                    {
                        result.Add(searchResult.Album);
                    }
                    else if (cell.Column.DisplayIndex == songColumnDisplayIndex)
                    {
                        result.Add(searchResult.Song);
                    }
                }
            }

            return result;
        }

        private TreeViewController TreeViewControllerOf(TreeView tree)
        {
            if (tree == m_ArtistTree)
            {
                return DataModel.DatabaseView.ArtistTreeController;
            }
            else if (tree == m_GenreTree)
            {
                return DataModel.DatabaseView.GenreTreeController;
            }
            else if (tree == m_DirectoryTree)
            {
                return DataModel.DatabaseView.DirectoryTreeController;
            }

            throw new Exception("Tried to find the controller of an unknown TreeView.");
        }

        private int DropTargetRowIndex(DragEventArgs e)
        {
            for (int i = 0; i < m_PlaylistView.Items.Count; ++i)
            {
                DataGridRow row = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;

                if (row != null)
                {
                    Point pt = e.GetPosition(row);
                    double yCoord = row.TranslatePoint(pt, row).Y;
                    double halfHeight = row.ActualHeight / 2;

                    if (yCoord < halfHeight)
                    {
                        return i;
                    }
                }
            }

            return m_PlaylistView.Items.Count;
        }

        private string GetDragDropDataString(object dragSource)
        {
            if (dragSource == m_SearchResultsView)
            {
                return AddSearchResults;
            }
            else if (dragSource == m_ArtistsView)
            {
                return AddArtists;
            }
            else if (dragSource == m_GenresView)
            {
                return AddGenres;
            }
            else if (dragSource == m_AlbumsBySelectedArtistsView || dragSource == m_AlbumsOfSelectedGenresView)
            {
                return AddAlbums;
            }
            else if (dragSource == m_SongsOnSelectedAlbumsView || dragSource == m_SongsOnSelectedGenreAlbumsView)
            {
                return AddSongs;
            }
            else if (dragSource == m_SavedPlaylistsView)
            {
                return LoadPlaylist;
            }
            else if (dragSource is TreeView)
            {
                return AddSongs;
            }
            else if (dragSource == m_StreamsView)
            {
                return AddStreams;
            }
            else if (dragSource == m_ItemsOnSelectedSavedPlaylistView)
            {
                return AddPlaylistItems;
            }
            else if (dragSource == m_PlaylistView)
            {
                return MovePlaylistItems;
            }

            throw new Exception("GetDragDropDataString: unknown drag source.");
        }

        private bool EnsureTreeViewHasCurrentNode(TreeViewController controller)
        {
            if (controller.Current == null)
            {
                controller.Current = controller.FirstNode;
                return controller.Current != null;
            }

            return true;
        }

        // nodeContainer must be either a TreeView or a TreeViewItem.
        private TreeViewItem GetTreeViewItem(ItemsControl nodeContainer, TreeViewNode node)
        {
            if (nodeContainer == null || node == null)
            {
                return null;
            }
            else
            {
                TreeViewItem nodeWithHighestLowerID = null;
                TreeViewItem item = null;
                int i = 0;

                do
                {
                    nodeWithHighestLowerID = item;
                    item = nodeContainer.ItemContainerGenerator.ContainerFromIndex(i++) as TreeViewItem;
                } while (item != null && ((TreeViewNode)item.Header).ID < node.ID);

                if (item != null && ((TreeViewNode)item.Header).ID == node.ID)
                {
                    return item;
                }
                else
                {
                    return GetTreeViewItem(nodeWithHighestLowerID, node);
                }
            }
        }

        #endregion
    }
}
