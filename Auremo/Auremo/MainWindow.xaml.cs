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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using Auremo.Properties;

namespace Auremo
{
    public partial class MainWindow : Window
    {
        private ServerConnection m_Connection = new ServerConnection();
        private ServerStatus m_ServerStatus = new ServerStatus();
        private Database m_Database = null;
        private DatabaseView m_DatabaseView = null;
        private SavedPlaylists m_SavedPlaylists = new SavedPlaylists();
        private Playlist m_Playlist = null;
        private DispatcherTimer m_Timer = null;
        private Object m_DragSource = null;
        private IList<object> m_DragDropPayload = null;
        private string m_DragDropData = null;
        private Nullable<Point> m_DragStartPosition = null;
        private bool m_PropertyUpdateInProgress = false;
        private bool m_OnlineMode = true;

        private const string AddArtists = "add_artists";
        private const string AddGenres = "add_genres";
        private const string AddAlbums = "add_albums";
        private const string AddSongs = "add_songs";
        private const string LoadPlaylist = "load_playlist";
        private const string MovePlaylistItems = "move_playlist_items";

        #region Start-up, construction and destruction

        public MainWindow()
        {
            InitializeComponent();
            InitializeAboutTab();
            InitializeComplexObjects();
            SetUpDataBindings();
            SetUpTreeViewControllers();
            LoadSettings();
            CreateTimer(Settings.Default.ViewUpdateInterval);
            ApplyInitialSettings();
            Update();
        }

        private void InitializeAboutTab()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Stream stream = assembly.GetManifestResourceStream("Auremo.Text.AUTHORS.txt");
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            string authors = reader.ReadToEnd();

            stream = assembly.GetManifestResourceStream("Auremo.Text.LICENSE.txt");
            reader = new StreamReader(stream, Encoding.UTF8);
            string license = reader.ReadToEnd();

            m_AboutBox.Text = authors + "\n\n\n" + license;
        }

        private void InitializeComplexObjects()
        {
            m_Database = new Database(m_Connection, m_ServerStatus);
            m_DatabaseView = new DatabaseView(m_Database);
            m_Playlist = new Playlist(m_Connection, m_ServerStatus, m_Database);
        }

        private void SetUpDataBindings()
        {
            m_CollectionBrowsingModes.DataContext = m_DatabaseView;
            m_SavedPlaylistsView.DataContext = m_SavedPlaylists;
            m_PlaylistView.DataContext = m_Playlist;
            m_PlaylistControls.DataContext = m_SavedPlaylists;
            m_PlaybackControls.DataContext = m_ServerStatus;
            m_PlayStatusMessage.DataContext = m_Playlist;
            m_ConnectionStatusDescription.DataContext = m_Connection;
            m_ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        private void SetUpTreeViewControllers()
        {
            m_DirectoryTree.Tag = m_DatabaseView.DirectoryTreeController;
            m_ArtistTree.Tag = m_DatabaseView.ArtistTreeController;
            m_GenreTree.Tag = m_DatabaseView.GenreTreeController;
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
                m_ToggleConnectionButton.Content = "Online mode";
                ConnectTo(Settings.Default.Server, Settings.Default.Port);
            }
            else
            {
                m_ToggleConnectionButton.Content = "Offline mode";
                Disconnect();
            }
        }

        private void ConnectTo(string host, int port)
        {
            m_Connection.SetHost(host, port);
            m_Connection.StartConnecting();
            SetTimerInterval(100); // Run with tight frequency until connected.
        }

        private void DoPostConnectInit()
        {
            string password = Crypto.DecryptPassword(Settings.Default.Password);

            if (password.Length > 0)
            {
                Protocol.Password(m_Connection, password);
            }
            
            m_Database.Refresh();
            m_DatabaseView.Refresh();
            m_SavedPlaylists.Refresh(m_Connection);
            SetTimerInterval(Settings.Default.ViewUpdateInterval); // Normal operation.
        }

        private void Disconnect()
        {
            if (m_Connection.Status == ServerConnection.State.Connected)
            {
                Protocol.Close(m_Connection);
            }

            m_Connection.Disconnect();
            m_Database.Refresh();
            m_DatabaseView.Refresh();
            m_SavedPlaylists.Refresh(m_Connection);
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
            if (m_Connection.Status == ServerConnection.State.Connecting)
            {
                if (m_Connection.IsReadyToConnect)
                {
                    ServerResponse banner = m_Connection.FinishConnecting();

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
            else if (m_Connection.Status == ServerConnection.State.Disconnected)
            {
                if (m_OnlineMode &&
                    Settings.Default.ReconnectInterval > 0 &&
                    m_Connection.TimeSinceDisconnect.TotalSeconds >= Settings.Default.ReconnectInterval)
                {
                    ConnectTo(Settings.Default.Server, Settings.Default.Port);
                }
            }

            m_ServerStatus.Update(m_Connection);
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            m_PropertyUpdateInProgress = true;

            if (e.PropertyName == "PlayPosition")
            {
                OnPlayPositionChanged();
            }
            else if (e.PropertyName == "Volume")
            {
                OnVolumeChanged();
            }
            else if (e.PropertyName == "DatabaseUpdateTime")
            {
                m_Database.Refresh();
                m_DatabaseView.Refresh();
            }

            m_PropertyUpdateInProgress = false;
        }

        private void OnPlayPositionChanged()
        {
            if (m_SeekBarPositionFromUser < 0)
            {
                // The user is not dragging the slider, so set the
                // values freely.
                m_SeekBar.Value = m_ServerStatus.PlayPosition;
                m_PlayPosition.Content = Utils.IntToTimecode(m_ServerStatus.PlayPosition);
            }
        }

        private void OnVolumeChanged()
        {
            m_VolumeControl.IsEnabled = m_ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            m_VolumeControl.Value = m_ServerStatus.Volume.HasValue ? m_ServerStatus.Volume.Value : 0;
        }

        #endregion

        #region Whole window operations

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (m_MusicTab.IsSelected)
            {
                if (e.Key == Key.Delete)
                {
                    OnDeleteFromPlaylist();
                }
            }

            if (e.Key == Key.Space)
            {
                if (m_ServerStatus != null && m_ServerStatus.OK)
                {
                    if (m_ServerStatus.IsPlaying)
                    {
                        Protocol.Pause(m_Connection);
                    }
                    else
                    {
                        Protocol.Play(m_Connection);
                    }
                }

                Update();
            }
            /*else if (e.Key == Key.L)
            {
                if (m_LogActive)
                {
                    LogMessage("Switching logging off.");
                    m_LogActive = false;
                }
                else
                {
                    m_LogActive = true;
                    LogMessage("Switched logging on.");
                }
            }*/
        }

        private void OnExit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }

        #endregion

        #region Music tab

        #region Simple (non-drag-drop) data grid operations

        private void OnArtistViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (AlbumMetadata album in m_DatabaseView.AlbumsBySelectedArtists)
                {
                    foreach (SongMetadata song in m_Database.SongsByAlbum(album))
                    {
                        Protocol.Add(m_Connection, song.Path);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnGenreViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (AlbumMetadata album in m_DatabaseView.AlbumsOfSelectedGenres)
                {
                    foreach (SongMetadata song in m_Database.SongsByAlbum(album))
                    {
                        Protocol.Add(m_Connection, song.Path);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnAlbumViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (SongMetadata song in m_DatabaseView.SongsOnSelectedAlbumsBySelectedArtists)
                {
                    Protocol.Add(m_Connection, song.Path);
                }

                e.Handled = true;
            }
        }

        private void OnGenreAlbumsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (SongMetadata song in m_DatabaseView.SongsOnSelectedAlbumsOfSelectedGenres)
                {
                    Protocol.Add(m_Connection, song.Path);
                }

                e.Handled = true;
            }
        }

        private void OnSongViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (object o in m_SongsOnSelectedAlbumsView.SelectedItems)
                {
                    if (o is SongMetadata)
                    {
                        SongMetadata song = o as SongMetadata;
                        Protocol.Add(m_Connection, song.Path);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnSavedPlaylistsViewKeyDown(object sender, KeyEventArgs e)
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                string playlistName = selectedPlaylist as string;
            
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    LoadSavedPlaylist(playlistName);
                }
                else if (e.Key == Key.Delete)
                {
                    e.Handled = true;
                    Protocol.Rm(m_Connection, playlistName);
                    m_SavedPlaylists.Refresh(m_Connection);
                }
            }
        }

        private void OnDeleteFromPlaylist()
        {
            foreach (object obj in m_PlaylistView.SelectedItems)
            {
                if (obj is PlaylistItem)
                {
                    PlaylistItem item = (PlaylistItem)obj;
                    Protocol.DeleteId(m_Connection, item.Id);
                }
            }

            m_PlaylistView.SelectedItems.Clear();
            Update();
        }

        private void OnSelectedArtistsChanged(object sender, SelectionChangedEventArgs e)
        {
            m_DatabaseView.OnSelectedArtistsChanged(m_ArtistsView.SelectedItems);
        }

        private void OnSelectedGenresChanged(object sender, SelectionChangedEventArgs e)
        {
            m_DatabaseView.OnSelectedGenresChanged(m_GenresView.SelectedItems);
        }

        private void OnSelectedAlbumsChanged(object sender, SelectionChangedEventArgs e)
        {
            m_DatabaseView.OnSelectedAlbumsBySelectedArtistsChanged(m_AlbumsBySelectedArtistsView.SelectedItems);
        }

        private void OnSelectedGenreAlbumsChanged(object sender, SelectionChangedEventArgs e)
        {
            m_DatabaseView.OnSelectedAlbumsOfSelectedGenresChanged(m_AlbumsOfSelectedGenresView.SelectedItems);
        }
        
        private void OnSongsOnAlbumsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridRowBeingClicked(m_SongsOnSelectedAlbumsView, e);

            if (row != null)
            {
                SongMetadata song = row.Item as SongMetadata;
                Protocol.Add(m_Connection, song.Path);
                Update();
            }
        }

        private void OnSongsOnSelectedAlbumsOfSelectedGenresViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridRowBeingClicked(sender as DataGrid, e);

            if (row != null)
            {
                SongMetadata song = row.Item as SongMetadata;
                Protocol.Add(m_Connection, song.Path);
                Update();
            }
        }

        private void OnSavedPlaylistsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

                if (selectedPlaylist != null)
                {
                    LoadSavedPlaylist(selectedPlaylist as string);
                }
            }
        }

        private void OnPlaylistViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (m_PlaylistView.SelectedItems.Count == 1)
                {
                    object o = m_PlaylistView.SelectedItems[0];
                    
                    if (o is PlaylistItem)
                    {
                        PlaylistItem item = o as PlaylistItem;
                        Protocol.PlayId(m_Connection, item.Id);
                        Update();
                    }
                }

                e.Handled = true;
            }
        }

        private void OnPlaylistViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = DataGridRowBeingClicked(m_PlaylistView, e);

            if (row != null)
            {
                PlaylistItem item = row.Item as PlaylistItem;
                Protocol.PlayId(m_Connection, item.Id);
                Update();
            }
        }

        private void OnDataGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                DataGrid grid = sender as DataGrid;
                DataGridRow row = DataGridRowBeingClicked(grid, e);

                if (row != null)
                {
                    grid.SelectedIndex = -1;
                    row.IsSelected = true;
                }
            }
        }

        private void OnSavePlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            if (m_SavedPlaylists.CurrentPlaylistNameNonempty)
            {
                string name = m_SavedPlaylists.CurrentPlaylistName.Trim();
                Protocol.Rm(m_Connection, name);
                Protocol.Save(m_Connection, name);
                m_SavedPlaylists.Refresh(m_Connection);
            }
        }

        private void OnDedupPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            ISet<string> songPathsOnPlaylist = new SortedSet<string>();
            IList<int> playlistIDsOfDuplicates = new List<int>();

            foreach (PlaylistItem item in m_Playlist.Items)
            {
                if (!songPathsOnPlaylist.Add(item.Song.Path))
                {
                    playlistIDsOfDuplicates.Add(item.Id);
                }
            }

            foreach (int id in playlistIDsOfDuplicates)
            {
                Protocol.DeleteId(m_Connection, id);
            }
        }

        private void OnClearPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Clear(m_Connection);
            m_SavedPlaylists.CurrentPlaylistName = "";
            Update();
        }

        private void LoadSavedPlaylist(string name)
        {
            Protocol.Clear(m_Connection);
            Protocol.Load(m_Connection, name);
            m_SavedPlaylists.CurrentPlaylistName = name;
            Update();
        }

        #endregion

        #region TreeView handling (browsing, drag & drop)

        private void OnTreeViewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = TreeViewItemBeingClicked(sender as TreeView, e);

            if (item != null && item.Header is TreeViewNode)
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
                    else if (e.ClickCount == 2)
                    {
                        if (node is DirectoryTreeViewNode)
                        {
                            node.IsExpanded = !node.IsExpanded;
                        }
                        else if (node is SongMetadataTreeViewNode)
                        {
                            SongMetadataTreeViewNode songNode = node as SongMetadataTreeViewNode;
                            Protocol.Add(m_Connection, songNode.Song.Path);
                        }
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

                e.Handled = true;
            }
        }

        private void OnTreeViewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
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
            TreeView tree = sender as TreeView;
            TreeViewController controller = tree.Tag as TreeViewController;

            e.Handled = true;
            
            if (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift)
            {
                bool currentChanged = false;

                if (e.Key == Key.Up && EnsureTreeViewHasCurrentNode(controller))
                {
                    controller.Current = controller.Previous;
                    currentChanged = true;
                }
                else if (e.Key == Key.Down && EnsureTreeViewHasCurrentNode(controller))
                {
                    controller.Current = controller.Next;
                    currentChanged = true;
                }
                else if (e.Key == Key.Enter)
                {
                    if (controller.MultiSelection.Count > 1)
                    {
                        foreach (SongMetadataTreeViewNode leaf in controller.Songs)
                        {
                            Protocol.Add(m_Connection, leaf.Song.Path);
                        }

                        Update();
                    }
                    else if (controller.Current != null)
                    {
                        if (controller.Current is SongMetadataTreeViewNode)
                        {
                            Protocol.Add(m_Connection, ((SongMetadataTreeViewNode)controller.Current).Song.Path);
                        }
                        else
                        {
                            controller.Current.IsExpanded = !controller.Current.IsExpanded;
                        }

                        Update();
                    }
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

        #region List drag & drop

        private void OnDataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 || (Keyboard.Modifiers & ModifierKeys.Control) != 0 || e.ClickCount > 1)
            {
                // Don't mess up multi-select or multiple-click.
                return;
            }

            DataGridRow row = DataGridRowBeingClicked((DataGrid)sender, e);

            if (row != null && row.IsSelected)
            {
                m_DragSource = sender;
                m_DragStartPosition = e.GetPosition(null);
                // Again, don't mess up multi-select.
                e.Handled = true;
            }
            else
            {
                m_DragSource = null;
                m_DragStartPosition = null;
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

                    if (m_DragSource is DataGrid)
                    {
                        DataGrid source = m_DragSource as DataGrid;

                        foreach (object o in source.SelectedItems)
                        {
                            payload.Add(o);
                        }
                    }
                    else if (m_DragSource is TreeView)
                    {
                        ISet<SongMetadataTreeViewNode> selection = null;

                        if (m_DragSource == m_DirectoryTree)
                        {
                            selection = m_DatabaseView.DirectoryTreeSelectedSongs;
                        }
                        else if (m_DragSource == m_ArtistTree)
                        {
                            selection = m_DatabaseView.ArtistTreeSelectedSongs;
                        }
                        else if (m_DragSource == m_GenreTree)
                        {
                            selection = m_DatabaseView.GenreTreeSelectedSongs;
                        }

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
            if (m_DragSource == null || m_DragDropPayload == null || m_DragDropPayload.Count == 0)
            {
                return "";
            }
            else
            {
                int count = m_DragDropPayload.Count;
                object firstItem = m_DragDropPayload[0];

                if (firstItem is string)
                {
                    if (m_DragSource == m_ArtistsView)
                    {
                        if (count == 1)
                            return "Adding " + (string)firstItem;
                        else
                            return "Adding " + count + " artists";
                    }
                    else if (m_DragSource == m_GenresView)
                    {
                        if (count == 1)
                            return "Adding " + (string)firstItem;
                        else
                            return "Adding " + count + " genres";
                    }
                    else if (m_DragSource == m_SavedPlaylistsView)
                    {
                        return (string)firstItem;
                    }
                }
                else if (firstItem is AlbumMetadata)
                {
                    if (m_DragDropPayload.Count == 1)
                        return "Adding " + ((AlbumMetadata)firstItem).Title;
                    else
                        return "Adding " + count + " albums";
                }
                else if (firstItem is SongMetadata)
                {
                    if (m_DragDropPayload.Count == 1)
                        return "Adding " + ((SongMetadata)firstItem).Title;
                    else
                        return "Adding " + count + " songs";
                }
                else if (firstItem is PlaylistItem)
                {
                    if (m_DragDropPayload.Count == 1)
                        return "Moving " + ((PlaylistItem)firstItem).Song.Title;
                    else
                        return "Moving " + count + " songs";
                }
            }

            return "";
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

                        m_DropPositionIndicator.X1 = 11;
                        m_DropPositionIndicator.X2 = m_PlaylistView.ActualWidth + 8;
                        m_DropPositionIndicator.Y1 += 5;
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

                if (data == AddArtists)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        string artist = (string)o;
                        ISet<AlbumMetadata> albums = m_Database.AlbumsByArtist(artist);

                        foreach (AlbumMetadata album in albums)
                        {
                            ISet<SongMetadata> songs = m_Database.SongsByAlbum(album);

                            foreach (SongMetadata song in songs)
                            {
                                Protocol.AddId(m_Connection, song.Path, targetRow++);
                            }
                        }
                    }
                }
                else if (data == AddGenres)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        string genre = (string)o;
                        ISet<AlbumMetadata> albums = m_Database.AlbumsByGenre(genre);

                        foreach (AlbumMetadata album in albums)
                        {
                            ISet<SongMetadata> songs = m_Database.SongsByAlbum(album);

                            foreach (SongMetadata song in songs)
                            {
                                Protocol.AddId(m_Connection, song.Path, targetRow++);
                            }
                        }
                    }
                }
                else if (data == AddAlbums)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        AlbumMetadata album = (AlbumMetadata)o;
                        ISet<SongMetadata> songs = m_Database.SongsByAlbum(album);

                        foreach (SongMetadata song in songs)
                        {
                            Protocol.AddId(m_Connection, song.Path, targetRow++);
                        }
                    }
                }
                else if (data == AddSongs)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        SongMetadata song = (SongMetadata)o;
                        Protocol.AddId(m_Connection, song.Path, targetRow++);
                    }
                }
                else if (data == LoadPlaylist)
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
                            Protocol.MoveId(m_Connection, item.Id, targetRow - 1);
                        }
                        else
                        {
                            Protocol.MoveId(m_Connection, item.Id, targetRow++);
                        }
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
                Protocol.Seek(m_Connection, m_ServerStatus.CurrentSongIndex, m_SeekBarPositionFromUser);
                m_SeekBarPositionFromUser = -1;
                Update();
            }
        }

        private void OnSeekBarMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Settings.Default.MouseWheelAdjustsSongPositionBy > 0)
            {
                int currentPosition = m_ServerStatus.PlayPosition;
                int newPosition = currentPosition;
                int increment;

                if (Settings.Default.MouseWheelSongPositionUnitIsSecond)
                {
                    increment = Settings.Default.MouseWheelAdjustsSongPositionBy;
                }
                else
                {
                    increment = Math.Max(1, Settings.Default.MouseWheelAdjustsSongPositionBy * m_ServerStatus.SongLength / 100);
                }

                if (e.Delta < 0)
                {
                    newPosition = Math.Max(0, newPosition - increment);
                }
                else if (e.Delta > 0)
                {
                    newPosition = Math.Min(m_ServerStatus.SongLength, newPosition + increment);
                }

                if (newPosition != currentPosition)
                {
                    Protocol.Seek(m_Connection, m_ServerStatus.CurrentSongIndex, newPosition);
                    Update();
                }
            }
        }

        #endregion

        #region Control buttons row

        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Previous(m_Connection);
            Update();
        }

        private void OnPlayButtonClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Play(m_Connection);
            Update();
        }

        private void OnPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Pause(m_Connection);
            Update();
        }

        private void OnPlayPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (m_ServerStatus != null && m_ServerStatus.OK)
            {
                if (m_ServerStatus.IsPlaying)
                {
                    Protocol.Pause(m_Connection);
                }
                else
                {
                    Protocol.Play(m_Connection);
                }
            }

            Update();
        }
        private void OnStopButtonClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Stop(m_Connection);
            Update();
        }

        private void OnSkipButtonClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Next(m_Connection);
            Update();
        }

        bool m_VolumeRestoreInProgress = false;

        private void OnVolumeSliderDragged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!m_PropertyUpdateInProgress && !m_VolumeRestoreInProgress)
            {
                // Volume slider is actually moving because the user is moving it.
                ServerResponse response = Protocol.SetVol(m_Connection, (int)e.NewValue);

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
            int? currentVolume = m_ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = currentVolume.Value;

                if (e.Delta < 0)
                {
                    newVolume = Math.Max(0, newVolume - Settings.Default.MouseWheelAdjustsVolumeBy);
                }
                else if (e.Delta > 0)
                {
                    newVolume = Math.Min(100, newVolume + Settings.Default.MouseWheelAdjustsVolumeBy);
                }

                if (newVolume != currentVolume)
                {
                    Protocol.SetVol(m_Connection, newVolume);
                    Update();
                }
            }
        }

        private void OnToggleRandomClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Random(m_Connection, !m_ServerStatus.Random);
        }

        private void OnToggleRepeatClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Repeat(m_Connection, !m_ServerStatus.Repeat);
        }

        private void OnToggleOnlineOfflineClicked(object sender, RoutedEventArgs e)
        {
            SetOnlineMode(!m_OnlineMode);
        }

        #endregion

        #endregion

        #region Server Tab

        private void OnUpdateCollectionClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Update(m_Connection);
        }
        
        #endregion

        #region Settings and settings tab

        private void ApplyInitialSettings()
        {
            SetOnlineMode(true);
            m_PlayButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Visible : Visibility.Collapsed;
            m_PauseButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Visible : Visibility.Collapsed;
            m_PlayPauseButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnSaveSettingsClicked(object sender, RoutedEventArgs e)
        {
            string oldServer = Settings.Default.Server;
            int oldPort = Settings.Default.Port;
            string oldPassword = Settings.Default.Password;

            Settings.Default.Server = m_ServerEntry.Text;
            Settings.Default.Port = Utils.StringToInt(m_PortEntry.Text, 6600);
            Settings.Default.Password = Crypto.EncryptPassword(m_PasswordEntry.Password);
            Settings.Default.ViewUpdateInterval = Utils.StringToInt(m_UpdateIntervalEntry.Text, 500);
            Settings.Default.EnableVolumeControl = m_EnableVolumeControl.IsChecked == null || m_EnableVolumeControl.IsChecked.Value;
            Settings.Default.DisplaySeparatePlayAndPauseButtons = m_DisplaySeparatePlayAndPauseButtons.IsChecked == null || m_DisplaySeparatePlayAndPauseButtons.IsChecked.Value;
            Settings.Default.MouseWheelAdjustsVolumeBy = Utils.StringToInt(m_WheelVolumeStepEntry.Text, 5);
            Settings.Default.MouseWheelAdjustsSongPositionBy = Utils.StringToInt(m_WheelSongPositionStepEntry.Text, 5);
            Settings.Default.MouseWheelSongPositionUnitIsSecond = m_WheelSongPositionUnitIsSecond.IsSelected;
            Settings.Default.Save();

            SetTimerInterval(Settings.Default.ViewUpdateInterval);

            if (Settings.Default.Server != oldServer || Settings.Default.Port != oldPort || Settings.Default.Password != oldPassword)
            {
                Disconnect();

                if (m_OnlineMode)
                {
                    ConnectTo(Settings.Default.Server, Settings.Default.Port);
                }
            }

            m_PlayButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Visible : Visibility.Collapsed;
            m_PauseButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Visible : Visibility.Collapsed;
            m_PlayPauseButton.Visibility = Settings.Default.DisplaySeparatePlayAndPauseButtons ? Visibility.Collapsed : Visibility.Visible;
            m_VolumeControl.IsEnabled = m_ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
        }

        private void OnLoadSettingsClicked(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            m_ServerEntry.Text = Settings.Default.Server;
            m_PortEntry.Text = Settings.Default.Port.ToString();
            m_PasswordEntry.Password = Crypto.DecryptPassword(Settings.Default.Password);
            m_UpdateIntervalEntry.Text = Settings.Default.ViewUpdateInterval.ToString();
            m_EnableVolumeControl.IsChecked = Settings.Default.EnableVolumeControl;
            m_DisplaySeparatePlayAndPauseButtons.IsChecked = Settings.Default.DisplaySeparatePlayAndPauseButtons;
            m_WheelVolumeStepEntry.Text = Settings.Default.MouseWheelAdjustsVolumeBy.ToString();
            m_WheelSongPositionStepEntry.Text = Settings.Default.MouseWheelAdjustsSongPositionBy.ToString();
            m_WheelSongPositionUnitIsSecond.IsSelected = Settings.Default.MouseWheelSongPositionUnitIsSecond;
        }

        private void OnNumericOptionPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void ValidateOptions(object sender, RoutedEventArgs e)
        {
            ClampTextBoxContent(m_PortEntry, 1, 6600, 65536);
            ClampTextBoxContent(m_UpdateIntervalEntry, 100, 500, 5000);
            ClampTextBoxContent(m_ReconnectIntervalEntry, 0, 10, 3600);
            ClampTextBoxContent(m_WheelVolumeStepEntry, 0, 5, 100);

            if (m_WheelSongPositionUnitIsPercent.IsSelected)
            {
                ClampTextBoxContent(m_WheelSongPositionStepEntry, 0, 5, 100);
            }
            else if (m_WheelSongPositionUnitIsSecond.IsSelected)
            {
                ClampTextBoxContent(m_WheelSongPositionStepEntry, 0, 5, 1800);
            }
        }

        private void ClampTextBoxContent(object control, int min, int dfault, int max)
        {
            TextBox textBox = control as TextBox;
            int value = Utils.StringToInt(textBox.Text, dfault);
            textBox.Text = Utils.Clamp(min, value, max).ToString();
        }

        #endregion

        #region Miscellaneous helper functions

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
            if (dragSource == m_ArtistsView)
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

        bool m_LogActive = false;

        private void LogMessage(string message)
        {
            if (m_LogActive)
            {
                m_LogLines.Items.Add(DateTime.Now.TimeOfDay.ToString() + ": " + message);
            }
        }

        private void LogResponse(ServerResponse response)
        {
            if (m_LogActive)
            {
                foreach (ServerResponseLine line in response.ResponseLines)
                {
                    m_LogLines.Items.Add(line.Full);
                }

                m_LogLines.Items.Add(response.Status.Full);
            }
        }

        #endregion
    }
}
