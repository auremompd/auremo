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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
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
        private Database m_Database = new Database();
        private Playlist m_Playlist = null;
        private DispatcherTimer m_Timer = null;
        private IList<object> m_DragDropPayload = null;
        private Nullable<Point> m_DragStartPosition = null;
        private bool m_PropertyUpdateInProgress = false;
        private bool m_OnlineMode = true;

        private const string AddArtists = "add_artists";
        private const string AddAlbums = "add_albums";
        private const string AddSongs = "add_songs";
        private const string MovePlaylistItems = "move_playlist_items";

        #region Start-up, construction and destruction

        public MainWindow()
        {
            InitializeComponent();
            InitializeAboutTab();
            InitializeComplexObjects();
            SetUpDataBindings();
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
            m_Playlist = new Playlist(m_Connection, m_ServerStatus, m_Database);
        }

        private void SetUpDataBindings()
        {
            m_ArtistsView.DataContext = m_Database;
            m_AlbumsBySelectedArtistsView.DataContext = m_Database;
            m_SongsOnSelectedAlbumsView.DataContext = m_Database;
            m_PlaylistView.DataContext = m_Playlist;
            m_PlaybackControls.DataContext = m_ServerStatus;
            m_PlayStatusMessage.DataContext = m_Playlist;
            m_ConnectionStatusDescription.DataContext = m_Connection;
            m_ServerStatus.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnServerStatusPropertyChanged);
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
            m_Database.Refresh(m_Connection);
            UpdateTopLevelSelection();
            SetTimerInterval(Settings.Default.ViewUpdateInterval); // Normal operation.
        }

        private void Disconnect()
        {
            if (m_Connection.Status == ServerConnection.State.Connected)
            {
                Protocol.Close(m_Connection);
            }

            m_Connection.Disconnect();
            m_Database.Refresh(m_Connection);
        }

        private void UpdateTopLevelSelection()
        {
            m_Database.OnSelectedArtistsChanged(m_ArtistsView.SelectedItems);
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

            LogMessage("property changed: " + e.PropertyName);

            if (e.PropertyName == "CurrentSongIndex")
            {
                OnCurrentSongIndexChanged();
            }
            else if (e.PropertyName == "PlayPosition")
            {
                OnPlayPositionChanged();
            }
            else if (e.PropertyName == "State")
            {
                OnPlayStateChanged();
            }
            else if (e.PropertyName == "Volume")
            {
                OnVolumeChanged();
            }

            m_PropertyUpdateInProgress = false;
        }

        //int m_SongIndexMarkedAsPlaying = -1;

        private void OnCurrentSongIndexChanged()
        {
            /*
            if (m_SongIndexMarkedAsPlaying >= 0 && m_SongIndexMarkedAsPlaying < m_PlaylistView.Items.Count)
            {
                PlaylistItem item = m_PlaylistView.Items[m_SongIndexMarkedAsPlaying] as PlaylistItem;
                item.IsPlaying = false;
                m_SongIndexMarkedAsPlaying = -1;
            }

            if (m_ServerStatus.OK && m_ServerStatus.CurrentSongIndex >= 0 && m_ServerStatus.CurrentSongIndex < m_PlaylistView.Items.Count)
            {
                m_SongIndexMarkedAsPlaying = m_ServerStatus.CurrentSongIndex;
                PlaylistItem item = m_PlaylistView.Items[m_SongIndexMarkedAsPlaying] as PlaylistItem;
                item.IsPlaying = true;
            }
             */

            UpdateSongOnStatusBar();
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

        private void OnPlayStateChanged()
        {
            UpdateSongOnStatusBar();
        }

        private void OnVolumeChanged()
        {
            m_VolumeControl.IsEnabled = m_ServerStatus.Volume.HasValue && Settings.Default.EnableVolumeControl;
            m_VolumeControl.Value = m_ServerStatus.Volume.HasValue ? m_ServerStatus.Volume.Value : 0;
        }

        private void UpdateSongOnStatusBar()
        {
            if (m_Connection.Status == ServerConnection.State.Connected)
            {
                if ((m_ServerStatus.IsPlaying || m_ServerStatus.IsPaused) &&
                    m_ServerStatus.CurrentSongIndex >= 0 &&
                    m_ServerStatus.CurrentSongIndex < m_Playlist.Items.Count)
                {
                    /*
                    string state = m_ServerStatus.IsPlaying ? "Playing: " : "Paused - ";
                    SongMetadata currentSong = m_Playlist.Items[m_ServerStatus.CurrentSongIndex].Song;
                    string message = state + currentSong.Title + " by " + currentSong.Artist + " (" + currentSong.Album;

                    if (currentSong.Year.HasValue)
                    {
                        message += ", " + currentSong.Year.Value;
                    }

                    message += ").";
                    m_PlayStatusMessage.Content = message;
                     */
                }
                /*
            else if (m_ServerStatus.IsStopped)
            {
                m_PlayStatusMessage.Content = "Stopped.";
            }
            else
            {
                m_PlayStatusMessage.Content = "";
            }*/
            }
            else
            {
                //m_PlayStatusMessage.Content = "";
            }
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
            else if (e.Key == Key.Space)
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

        #region Simple (non-drag-drop) list view operations

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
            m_Database.OnSelectedArtistsChanged(m_ArtistsView.SelectedItems);
        }

        private void OnSelectedAlbumsChanged(object sender, SelectionChangedEventArgs e)
        {
            m_Database.OnSelectedAlbumsChanged(m_AlbumsBySelectedArtistsView.SelectedItems);
        }

        private void OnSongsOnAlbumsViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ListViewItem row = ListViewItemBeingClicked(m_SongsOnSelectedAlbumsView, e);

            if (row != null)
            {
                SongMetadata song = row.Content as SongMetadata;
                Protocol.Add(m_Connection, song.Path);
                Update();
            }
        }

        private void OnPlaylistViewDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ListViewItem row = ListViewItemBeingClicked(m_PlaylistView, e);

            if (row != null)
            {
                PlaylistItem item = row.Content as PlaylistItem;
                Protocol.PlayId(m_Connection, item.Id);
                Update();
            }
        }

        #endregion

        #region Drag & Drop

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 || (Keyboard.Modifiers & ModifierKeys.Shift) != 0 || e.ClickCount > 1)
            {
                // Don't mess up multi-select or multiple-click.
                return;
            }

            ListView dragSource = (ListView)sender;
            ListViewItem row = ListViewItemBeingClicked(dragSource, e);

            if (row != null && row.IsSelected)
            {
                m_DragStartPosition = e.GetPosition(null);

                // Using an internal m_DragDropPayload is cheating, but the
                // the standard (through clipboard?) system is pure evil and
                // since we don't need drag & drop across application borders,
                // this will do for now.
                m_DragDropPayload = new List<object>();

                foreach (object o in dragSource.SelectedItems)
                {
                    m_DragDropPayload.Add(o);

                    if (o is PlaylistItem)
                    {
                        PlaylistItem item = (PlaylistItem)o;
                        int index = m_PlaylistView.Items.IndexOf(item);
                    }
                }

                // Again, don't mess up multi-select.
                e.Handled = true;
            }
            else
            {
                m_DragStartPosition = null;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (m_DragStartPosition.HasValue)
            {
                Vector dragDistance = e.GetPosition(null) - m_DragStartPosition.Value;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(dragDistance.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragDistance.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    DragDropEffects mode = sender == m_PlaylistView ? DragDropEffects.Move : DragDropEffects.Copy;
                    string data = GetDragDropDataString(sender);
                    DragDrop.DoDragDrop((DependencyObject)sender, data, mode);
                    m_DragStartPosition = null;
                }
            }
        }

        private void OnPlaylistViewDragOver(object sender, DragEventArgs e)
        {
            if (m_DragDropPayload != null && !m_PlaylistView.Items.IsEmpty)
            {
                int targetRow = DropTargetRowIndex(e);
                ListViewItem item = null;

                if (targetRow >= 0)
                {
                    item = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(targetRow) as ListViewItem;
                }

                if (item == null)
                {
                    ListViewItem lastItem = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(m_PlaylistView.Items.Count - 1) as ListViewItem;

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
                    Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
                    GeneralTransform transform = item.TransformToAncestor(m_PlaylistView);
                    Point topOfItem = transform.Transform(bounds.TopLeft);
                    m_DropPositionIndicator.Y1 = topOfItem.Y;
                }

                m_DropPositionIndicator.X1 = +10;
                m_DropPositionIndicator.X2 = m_PlaylistView.ActualWidth - 20;
                m_DropPositionIndicator.Y1 += 2;
                m_DropPositionIndicator.Y2 = m_DropPositionIndicator.Y1;
                m_DropPositionIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                m_DropPositionIndicator.Visibility = Visibility.Hidden;
            }
        }

        private void OnPlaylistViewDragLeave(object sender, DragEventArgs e)
        {
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
                        ISet<AlbumMetadata> albums = m_Database.Albums(artist);

                        foreach (AlbumMetadata album in albums)
                        {
                            ISet<SongMetadata> songs = m_Database.Songs(album);

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
                        ISet<SongMetadata> songs = m_Database.Songs(album);

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
                else if (data == MovePlaylistItems)
                {
                    // Plan the move. For this we need the old positions of
                    // the moved items.
                    IDictionary<int, int> itemLookup = new SortedDictionary<int, int>();

                    foreach (object o in m_DragDropPayload)
                    {
                        PlaylistItem item = (PlaylistItem)o;
                        itemLookup[item.Id] = -1;
                    }

                    int positionOnPlaylist = 0;

                    foreach (PlaylistItem item in m_Playlist.Items)
                    {
                        if (itemLookup.ContainsKey(item.Id))
                        {
                            itemLookup[item.Id] = positionOnPlaylist;
                        }

                        ++positionOnPlaylist;
                    }

                    // That's all we need.
                    foreach (object o in m_DragDropPayload)
                    {
                        PlaylistItem item = (PlaylistItem)o;
                        int oldPosition = itemLookup[item.Id];

                        if (oldPosition < targetRow)
                        {
                            Protocol.MoveId(m_Connection, item.Id, targetRow - 1);
                        }
                        else if (oldPosition > targetRow)
                        {
                            Protocol.MoveId(m_Connection, item.Id, targetRow++);
                        }
                    }
                }

                m_DragDropPayload = null;
                Update();
            }

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
                if (Protocol.SetVol(m_Connection, (int)e.NewValue).IsACK)
                {
                    m_VolumeRestoreInProgress = true;
                    m_VolumeControl.Value = e.OldValue;
                    m_VolumeRestoreInProgress = false;
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

        #region Status bar

        #endregion

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

            Settings.Default.Server = m_ServerEntry.Text;
            Settings.Default.Port = Utils.StringToInt(m_PortEntry.Text, 6600);
            Settings.Default.ViewUpdateInterval = Utils.StringToInt(m_UpdateIntervalEntry.Text, 500);
            Settings.Default.EnableVolumeControl = m_EnableVolumeControl.IsChecked == null || m_EnableVolumeControl.IsChecked.Value;
            Settings.Default.DisplaySeparatePlayAndPauseButtons = m_DisplaySeparatePlayAndPauseButtons.IsChecked == null || m_DisplaySeparatePlayAndPauseButtons.IsChecked.Value;
            Settings.Default.Save();

            SetTimerInterval(Settings.Default.ViewUpdateInterval);

            if (Settings.Default.Server != oldServer || Settings.Default.Port != oldPort)
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
            m_UpdateIntervalEntry.Text = Settings.Default.ViewUpdateInterval.ToString();
            m_EnableVolumeControl.IsChecked = Settings.Default.EnableVolumeControl;
            m_DisplaySeparatePlayAndPauseButtons.IsChecked = Settings.Default.DisplaySeparatePlayAndPauseButtons;
        }

        private void OnNumericOptionPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void OnNumericOptionLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox source = sender as TextBox;
            int min, max;

            if (source == m_PortEntry)
            {
                min = 1;
                max = 65535;
            }
            else if (source == m_UpdateIntervalEntry)
            {
                min = 100;
                max = 5000;
            }
            else // if (source == m_ReconnectIntervalEntry)
            {
                min = 0;
                max = 3600;
            }

            if (source.Text == "")
            {
                source.Text = min.ToString();
            }
            else
            {
                int value;

                if (!int.TryParse(source.Text, out value))
                {
                    value = max + 1;
                }

                if (value < min)
                    source.Text = min.ToString();
                else if (value > max)
                    source.Text = max.ToString();
            }
        }

        #endregion

        #region Miscellaneous helper functions

        private ListViewItem ListViewItemBeingClicked(ListView list, MouseButtonEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(list, e.GetPosition(list));

            if (hit == null)
            {
                return null;
            }

            DependencyObject component = (DependencyObject)hit.VisualHit;

            while (component != null)
            {
                if (component is ListViewItem)
                {
                    return (ListViewItem)component;
                }
                else
                {
                    component = VisualTreeHelper.GetParent(component);
                }
            }

            return null;
        }

        private int DropTargetRowIndex(DragEventArgs e)
        {
            for (int i = 0; i < m_PlaylistView.Items.Count; ++i)
            {
                ListViewItem item = m_PlaylistView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

                if (item != null)
                {
                    Point pt = e.GetPosition(item);
                    double yCoord = item.TranslatePoint(pt, item).Y;
                    double halfHeight = item.ActualHeight / 2;

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
                return AddArtists;
            else if (dragSource == m_AlbumsBySelectedArtistsView)
                return AddAlbums;
            else if (dragSource == m_SongsOnSelectedAlbumsView)
                return AddSongs;
            else if (dragSource == m_PlaylistView)
                return MovePlaylistItems;

            throw new Exception("GetDragDropDataString: unknown drag source.");
        }

        bool m_LogActive = true;

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
                foreach (ServerResponseLine line in response.Lines)
                {
                    m_LogLines.Items.Add(line.Full);
                }

                m_LogLines.Items.Add(response.Status.Full);
            }
        }

        #endregion
    }
}
