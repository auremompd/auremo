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
        private TextWindow m_AboutWindow = null;
        private ServerConnection m_Connection = new ServerConnection();
        private ServerStatus m_ServerStatus = new ServerStatus();
        private Database m_Database = null;
        private StreamsCollection m_StreamsCollection = new StreamsCollection();
        private DatabaseView m_DatabaseView = null;
        private CollectionSearch m_CollectionSearchThread = null;
        private SavedPlaylists m_SavedPlaylists = new SavedPlaylists();
        private Playlist m_Playlist = null;
        private OutputCollection m_Outputs = new OutputCollection();
        private DispatcherTimer m_Timer = null;
        private Object m_DragSource = null;
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

        private void InitializeComplexObjects()
        {
            m_Database = new Database(m_Connection, m_ServerStatus);
            m_CollectionSearchThread = new CollectionSearch(m_Database);
            m_DatabaseView = new DatabaseView(m_Database, m_StreamsCollection, m_CollectionSearchThread);
            m_Playlist = new Playlist(m_Connection, m_ServerStatus, m_Database, m_StreamsCollection);
            m_DatabaseView.RefreshStreams();
        }

        private void SetUpDataBindings()
        {
            m_FileMenuSavePlaylistAsItem.DataContext = m_PlaylistView;
            m_ConnectionMenuItem.DataContext = m_Connection;
            m_OutputsMenu.DataContext = m_Outputs;
            
            m_CollectionBrowsingModes.DataContext = m_DatabaseView;
            m_SearchResultsViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            m_SearchBox.DataContext = m_Connection;

            m_ArtistsViewContextMenu.DataContext = m_ArtistsView.SelectedItems;
            m_ArtistsViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            m_AlbumsBySelectedArtistsViewContextMenu.DataContext = m_AlbumsBySelectedArtistsView.SelectedItems;
            m_AlbumsBySelectedArtistsViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            m_SongsOnSelectedAlbumsViewContextMenu.DataContext = m_SongsOnSelectedAlbumsView.SelectedItems;
            m_SongsOnSelectedAlbumsViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            
            m_ArtistTreeContextMenu.DataContext = m_DatabaseView.ArtistTreeController;
            m_ArtistTreeRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            
            m_GenresViewContextMenu.DataContext = m_GenresView.SelectedItems;
            m_GenresViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            m_AlbumsOfSelectedGenresViewContextMenu.DataContext = m_AlbumsOfSelectedGenresView.SelectedItems;
            m_AlbumsOfSelectedGenresViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            m_SongsOnSelectedGenreAlbumsViewContextMenu.DataContext = m_SongsOnSelectedGenreAlbumsView.SelectedItems;
            m_SongsOnSelectedGenreAlbumsViewRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            
            m_GenreTreeContextMenu.DataContext = m_DatabaseView.GenreTreeController;
            m_GenreTreeRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            
            m_DirectoryTreeContextMenu.DataContext = m_DatabaseView.DirectoryTreeController;
            m_DirectoryTreeRescanMusicCollectionContextMenuItem.DataContext = m_Connection;
            
            m_SavedPlaylistsView.DataContext = m_SavedPlaylists;
            m_SavedPlaylistsViewContextMenu.DataContext = m_SavedPlaylistsView.SelectedItems;

            m_StreamsViewContextMenu.DataContext = m_StreamsView.SelectedItems;

            m_PlaylistView.DataContext = m_Playlist;
            m_PlaylistViewContextMenu.DataContext = m_PlaylistView;
            
            m_SeekPanel.DataContext = m_ServerStatus;
            m_PlaybackControlPanel.DataContext = m_ServerStatus;
            m_PlayButton.DataContext = m_ServerStatus.IsPlaying;
            m_PauseButton.DataContext = m_ServerStatus.IsPaused;
            m_StopButton.DataContext = m_ServerStatus.IsStopped;
            m_RandomButton.DataContext = m_ServerStatus.IsOnRandom;
            m_RepeatButton.DataContext = m_ServerStatus.IsOnRepeat;

            m_PlayStatusMessage.DataContext = m_Playlist;
            m_ConnectionStatusDescription.DataContext = m_Connection;

            m_SearchResultsHint.DataContext = m_SearchResultsView.Items;
            m_SearchBoxHint.DataContext = m_SearchBox;
            m_ArtistsHint.DataContext = m_DatabaseView.Artists;
            m_AlbumsBySelectedArtistsHint.DataContext = m_DatabaseView.AlbumsBySelectedArtists;
            m_SongsOnSelectedAlbumsHint.DataContext = m_DatabaseView.SongsOnSelectedAlbumsBySelectedArtists;
            m_ArtistsTreeHint.DataContext = m_DatabaseView.ArtistTree;
            m_GenresHint.DataContext = m_DatabaseView.Genres;
            m_AlbumsOfSelectedGenresHint.DataContext = m_DatabaseView.AlbumsOfSelectedGenres;
            m_SongsOnSelectedGenreAlbumsHint.DataContext = m_DatabaseView.SongsOnSelectedAlbumsOfSelectedGenres;
            m_GenresTreeHint.DataContext = m_DatabaseView.GenreTree;
            m_DirectoryTreeHint.DataContext = m_DatabaseView.DirectoryTree;
            m_StreamsHint.DataContext = m_StreamsView.Items;
            m_SavedPlaylistsHint.DataContext = m_SavedPlaylists.Playlists;

            m_PlaylistViewHint.DataContext = m_Playlist.Items;

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
                ConnectTo(Settings.Default.Server, Settings.Default.Port);
            }
            else
            {
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
            
            m_Database.RefreshCollection();
            m_DatabaseView.RefreshCollection();
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
            m_Database.RefreshCollection();
            m_DatabaseView.RefreshCollection();
            m_SavedPlaylists.Refresh(m_Connection);
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
            m_Outputs.Update(m_Connection);
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            m_PropertyUpdateInProgress = true;

            if (e.PropertyName == "OK")
            {
                if (!m_ServerStatus.OK)
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
                m_Database.RefreshCollection();
                m_DatabaseView.RefreshCollection();
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
            if (e.Key == Key.MediaPreviousTrack)
            {
                Back();
            }
            else if (e.Key == Key.Space && !m_SearchBox.IsFocused && m_StringQueryOverlay.Visibility != Visibility.Visible && !AutoSearchInProgrss)
            {
                TogglePlayPause();
            }
            else if (e.Key == Key.MediaPlayPause)
            {
                TogglePlayPause();
            }
            else if (e.Key == Key.MediaStop)
            {
                Stop();
            }
            else if (e.Key == Key.MediaNextTrack)
            {
                Skip();
            }
            else if (e.Key == Key.VolumeDown)
            {
                VolumeDown();
            }
            else if (e.Key == Key.VolumeUp)
            {
                VolumeUp();
            }
        }

        private void OnExit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_CollectionSearchThread.Terminate();
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
                Protocol.DisableOutput(m_Connection, output.Index);
            }
            else
            {
                Protocol.EnableOutput(m_Connection, output.Index);
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

        #endregion

        #region Music collection

        #region Simple (non-drag-drop) data grid operations

        private void OnSearchBoxEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (m_SearchBox.Focusable)
            {
                m_SearchBox.Focus();
            }
        }

        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            m_CollectionSearchThread.SearchString = m_SearchBox.Text;
        }

        private void OnSearchResultsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            // Is this really the best way to find which column this is?
            // It seems contrived, but DataGridCellInfo seems to contain
            // very little usable information.
            const int songColumnDisplayIndex = 0;
            const int artistColumnDisplayIndex = 1;
            const int albumColumnDisplayIndex = 2;

            foreach (DataGridCellInfo cell in m_SearchResultsView.SelectedCells)
            {
                CollectionSearch.SearchResultTuple result = (CollectionSearch.SearchResultTuple)(cell.Item);

                if (result != null)
                {
                    if (cell.Column.DisplayIndex == artistColumnDisplayIndex)
                    {
                        AddArtistToPlaylist(result.Artist);
                    }
                    else if (cell.Column.DisplayIndex == albumColumnDisplayIndex)
                    {
                        AddAlbumToPlaylist(result.Album);
                    }
                    else if (cell.Column.DisplayIndex == songColumnDisplayIndex)
                    {
                        AddSongToPlaylist(result.Song);
                    }
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
                        Protocol.Add(m_Connection, node.Song.Path);
                    }
                }
            }
        }

        public void OnRescanMusicCollectionClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Update(m_Connection);
        }

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

        private void OnCollectionTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Handled && e.Text != null && e.Text.Length == 1)
            {
                CollectionAutoSearch(sender, e.Text[0]);
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
                foreach (object song in m_SongsOnSelectedAlbumsView.SelectedItems)
                {
                    AddObjectToPlaylist(song, false);
                }

                e.Handled = true;
            }
        }

        private void OnStreamsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (object stream in m_StreamsView.SelectedItems)
                {
                    AddObjectToPlaylist(stream, false);
                }

                e.Handled = true;
            }
            else if (e.Key == Key.F2 && m_StreamsView.SelectedItems.Count == 1)
            {
                OnRenameSelectedStream();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                OnDeleteSelectedStreams();
                e.Handled = true;
            }
        }

        private void OnStreamsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            foreach (object stream in m_StreamsView.SelectedItems)
            {
                AddObjectToPlaylist(stream, false);
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
                m_StreamsCollection.Rename(stream, newName);
            }
        }

        private void OnDeleteSelectedStreamsClicked(object sender, RoutedEventArgs e)
        {
            OnDeleteSelectedStreams();
        }

        private void OnAddStreamURLClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewStreamQuery();
        }

        private void OnAddNewStreamQueryFinished(bool succeeded, string address, string name)
        {
            if (succeeded)
            {
                StreamMetadata stream = new StreamMetadata();
                stream.Path = address;
                stream.Title = name;
                m_StreamsCollection.Add(stream);
                m_DatabaseView.RefreshStreams();
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

                m_StreamsCollection.Add(streamsToAdd);
                m_DatabaseView.RefreshStreams();
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

        private void OnDeleteSelectedStreams()
        {
            m_StreamsCollection.Delete(Utils.ToTypedList<StreamMetadata>(m_StreamsView.SelectedItems));
        }
        
        private void OnSavedPlaylistsViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnLoadSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                RenameSavedPlaylist();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                OnDeleteSavedPlaylist();
                e.Handled = true;
            }
        }

        private void OnSavedPlaylistsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                OnLoadSavedPlaylist();
            }
        }

        private void OnSendSavedPlaylistToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            OnLoadSavedPlaylist();
        }

        private void OnLoadSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                Protocol.Load(m_Connection, selectedPlaylist as string);
            }
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
                Protocol.Rename(m_Connection, oldName, newName);
                m_SavedPlaylists.Refresh(m_Connection);
            }
        }

        private void OnDeleteSavedPlaylistClicked(object sender, RoutedEventArgs e)
        {
            OnDeleteSavedPlaylist();
        }

        private void OnDeleteSavedPlaylist()
        {
            object selectedPlaylist = m_SavedPlaylistsView.SelectedItem;

            if (selectedPlaylist != null)
            {
                Protocol.Rm(m_Connection, selectedPlaylist as string);
                m_SavedPlaylists.Refresh(m_Connection);
            }
        }

        private void OnDeleteFromPlaylist()
        {
            foreach (object o in m_PlaylistView.SelectedItems)
            {
                if (o is PlaylistItem)
                {
                    PlaylistItem item = o as PlaylistItem;
                    Protocol.DeleteId(m_Connection, item.Id);
                }
            }

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
            else if (e.Key == Key.Delete)
            {
                OnDeleteFromPlaylist();
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
            if (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers == ModifierKeys.None)
            {
                DataGrid grid = sender as DataGrid;

                if (grid == m_SearchResultsView)
                {
                    DataGridCell cell = DataGridCellBeingClicked(grid, e);

                    if (cell != null)
                    {
                        grid.SelectedCells.Clear();
                        cell.IsSelected = true;
                    }
                }
                else
                {
                    DataGridRow row = DataGridRowBeingClicked(grid, e);

                    if (row != null)
                    {
                        grid.SelectedIndex = -1;
                        row.IsSelected = true;
                    }
                }
            }
        }

        private void OnDedupPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            ISet<string> songPathsOnPlaylist = new SortedSet<string>();
            IList<int> playlistIDsOfDuplicates = new List<int>();

            foreach (PlaylistItem item in m_Playlist.Items)
            {
                if (!songPathsOnPlaylist.Add(item.Playable.Path))
                {
                    playlistIDsOfDuplicates.Add(item.Id);
                }
            }

            foreach (int id in playlistIDsOfDuplicates)
            {
                Protocol.DeleteId(m_Connection, id);
            }
        }

        private void OnShufflePlaylistClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Shuffle(m_Connection);
        }

        private void OnClearPlaylistViewClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Clear(m_Connection);
            m_SavedPlaylists.CurrentPlaylistName = "";
            Update();
        }


        private void OnRemoveSelectedPlaylistItemsClicked(object sender, RoutedEventArgs e)
        {
            OnDeleteFromPlaylist();
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

                foreach (PlaylistItem item in m_Playlist.Items)
                {
                    if (!keepItems.Contains(item.Id))
                    {
                        Protocol.DeleteId(m_Connection, item.Id);
                    }
                }

                Update();
            }
        
        }

        private void OnSavePlaylistAsClicked(object sender, RoutedEventArgs e)
        {
            StartAddNewPlaylistAsQuery(m_SavedPlaylists.CurrentPlaylistName);
        }

        private void OnAddNewPlaylistAsQueryFinished(bool succeeded, string playlistName)
        {
            if (succeeded)
            {
                m_SavedPlaylists.CurrentPlaylistName = playlistName;
                Protocol.Rm(m_Connection, m_SavedPlaylists.CurrentPlaylistName);
                Protocol.Save(m_Connection, m_SavedPlaylists.CurrentPlaylistName);
                m_SavedPlaylists.Refresh(m_Connection);
            }
        }

        private void LoadSavedPlaylist(string name)
        {
            Protocol.Clear(m_Connection);
            Protocol.Load(m_Connection, name);
            m_SavedPlaylists.CurrentPlaylistName = name;
            Update();
        }

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
            if (e.ChangedButton != MouseButton.Left ||
                Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ||
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || 
                e.ClickCount > 1)
            {
                // Don't mess up multi-select or multiple-click.
                return;
            }

            DataGrid grid = sender as DataGrid;
            bool dragStarting = false;

            if (sender == m_SearchResultsView)
            {
                DataGridCell cell = DataGridCellBeingClicked(grid, e);
                dragStarting = cell != null && cell.IsSelected;
            }
            else
            {
                DataGridRow row = DataGridRowBeingClicked(grid, e);
                dragStarting = row != null && row.IsSelected;
            }

            if (dragStarting)
            {
                m_DragSource = grid;
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
                            if (artistsToAdd.Contains(song.Artist) || albumsToAdd.Contains(m_Database.AlbumOfSong(song)))
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
            else if (m_DragSource == m_SearchResultsView)
            {
                if (m_DragDropPayload.Count == 1)
                {
                    object theItem = m_DragDropPayload[0];

                    if (theItem is string)
                        return "Adding " + (string)theItem;
                    else if (theItem is AlbumMetadata)
                        return "Adding " + ((AlbumMetadata)theItem).Title;
                    else if (theItem is SongMetadata)
                        return "Adding " + ((SongMetadata)theItem).Title;
                }
                else
                {
                    return "Adding " + m_DragDropPayload.Count + " items";
                }
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
                else if (firstItem is StreamMetadata)
                {
                    if (m_DragDropPayload.Count == 1)
                        return "Adding " + ((StreamMetadata)firstItem).Title;
                    else
                        return "Adding " + count + " streams";
                }
                else if (firstItem is PlaylistItem)
                {
                    if (m_DragDropPayload.Count == 1)
                        return "Moving " + ((PlaylistItem)firstItem).Playable.Title;
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

                if (data == AddSearchResults)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        targetRow = AddObjectToPlaylist(o, true, targetRow);
                    }
                }
                else if (data == AddArtists)
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
                else if (data == AddSongs || data == AddStreams)
                {
                    foreach (object o in m_DragDropPayload)
                    {
                        Playable playable = (Playable)o;
                        Protocol.AddId(m_Connection, playable.Path, targetRow++);
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
            int currentPosition = m_ServerStatus.PlayPosition;
            int newPosition = currentPosition;
            int increment = 0;

            if (Settings.Default.MouseWheelAdjustsSongPositionInPercent && Settings.Default.MouseWheelAdjustsSongPositionPercentBy > 0)
            {
                increment = Math.Max(1, Settings.Default.MouseWheelAdjustsSongPositionPercentBy * m_ServerStatus.SongLength / 100);
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
                newPosition = Math.Min(m_ServerStatus.SongLength, newPosition + increment);
            }

            if (newPosition != currentPosition)
            {
                Protocol.Seek(m_Connection, m_ServerStatus.CurrentSongIndex, newPosition);
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
            Protocol.Random(m_Connection, !m_ServerStatus.IsOnRandom.Value);
        }

        private void OnToggleRepeatClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Repeat(m_Connection, !m_ServerStatus.IsOnRepeat.Value);
        }

        #endregion

        #region Playback control

        private void Back()
        {
            Protocol.Previous(m_Connection);
            Update();
        }

        private void Play()
        {
            Protocol.Play(m_Connection);
            Update();
        }

        private void Pause()
        {
            Protocol.Pause(m_Connection);
            Update();
        }

        private void TogglePlayPause()
        {
            if (m_ServerStatus != null && m_ServerStatus.OK)
            {
                if (m_ServerStatus.IsPlaying.Value)
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

        private void Stop()
        {
            Protocol.Stop(m_Connection);
            Update();
        }

        private void Skip()
        {
            Protocol.Next(m_Connection);
            Update();
        }
                
        private void VolumeDown()
        {
            int? currentVolume = m_ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Max(0, currentVolume.Value - Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    Protocol.SetVol(m_Connection, newVolume);
                    Update();
                }
            }
        }
        
        private void VolumeUp()
        {
            int? currentVolume = m_ServerStatus.Volume;

            if (currentVolume != null && Settings.Default.EnableVolumeControl)
            {
                int newVolume = Math.Min(100, currentVolume.Value + Settings.Default.VolumeAdjustmentStep);

                if (newVolume != currentVolume)
                {
                    Protocol.SetVol(m_Connection, newVolume);
                    Update();
                }
            }
        }

        #endregion

        #region Server Tab

        private void OnUpdateCollectionClicked(object sender, RoutedEventArgs e)
        {
            Protocol.Update(m_Connection);
        }
        
        #endregion

        #region Settings and settings tab

        // Called by SettingsWindow when new settings are applied.
        public void SettingsChanged(bool reconnect)
        {
            m_VolumeControl.IsEnabled = m_ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
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
            SetOnlineMode(true);
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

                m_AboutWindow = new TextWindow("About - Auremo MPD Client", about, this);
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
            foreach (AlbumMetadata album in m_Database.AlbumsByArtist(artist))
            {
                AddAlbumToPlaylist(album);
            }
        }

        private int AddArtistToPlaylist(string artist, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in m_Database.AlbumsByArtist(artist))
            {
                position = AddAlbumToPlaylist(album, position);
            }

            return position;
        }

        private void AddGenreToPlaylist(string genre)
        {
            foreach (AlbumMetadata album in m_Database.AlbumsByGenre(genre))
            {
                AddAlbumToPlaylist(album);
            }
        }

        private int AddGenreToPlaylist(string genre, int firstPosition)
        {
            int position = firstPosition;

            foreach (AlbumMetadata album in m_Database.AlbumsByGenre(genre))
            {
                position = AddAlbumToPlaylist(album, position);
            }

            return position;
        }

        private void AddAlbumToPlaylist(AlbumMetadata album)
        {
            foreach (SongMetadata song in m_Database.SongsByAlbum(album))
            {
                AddSongToPlaylist(song);
            }
        }

        private int AddAlbumToPlaylist(AlbumMetadata album, int firstPosition)
        {
            int position = firstPosition;

            foreach (SongMetadata song in m_Database.SongsByAlbum(album))
            {
                position = AddSongToPlaylist(song, position);
            }

            return position;
        }

        private void AddSongToPlaylist(SongMetadata song)
        {
            Protocol.Add(m_Connection, song.Path);
        }

        private int AddSongToPlaylist(SongMetadata song, int position)
        {
            Protocol.AddId(m_Connection, song.Path, position);
            return position + 1;
        }

        private void AddStreamToPlaylist(StreamMetadata stream)
        {
            Protocol.Add(m_Connection, stream.Path);
        }

        private int AddStreamToPlaylist(StreamMetadata stream, int position)
        {
            Protocol.AddId(m_Connection, stream.Path, position);
            return position + 1;
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

        private TreeViewController TreeViewControllerOf(TreeView tree)
        {
            if (tree == m_ArtistTree)
            {
                return m_DatabaseView.ArtistTreeController;
            }
            else if (tree == m_GenreTree)
            {
                return m_DatabaseView.GenreTreeController;
            }
            else if (tree == m_DirectoryTree)
            {
                return m_DatabaseView.DirectoryTreeController;
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
