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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Playlist : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        DataModel m_DataModel = null;
        PlaylistItem m_ItemMarkedAsCurrent = null;

        public Playlist(DataModel dataModel)
        {
            m_DataModel = dataModel;
            Items = new ObservableCollection<PlaylistItem>();
            m_DataModel.ServerStatus.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        public IList<PlaylistItem> Items
        {
            get;
            private set;
        }

        private IList<PlaylistItem> m_SelectedItems = new ObservableCollection<PlaylistItem>();
        public IList<PlaylistItem> SelectedItems
        {
            get
            {
                return m_SelectedItems;
            }
            set
            {
                m_SelectedItems.Clear();

                foreach (PlaylistItem item in value)
                {
                    m_SelectedItems.Add(item);
                }
            }
        }

        public string PlayStatusDescription
        {
            get; 
            private set;
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlaylistVersion")
            {
                UpdateItems();
                UpdateCurrentSong();
            }
            else if (e.PropertyName == "CurrentSongIndex" || e.PropertyName == "State")
            {
                UpdateCurrentSong();
            }
        }

        private void UpdateItems()
        {
            m_DataModel.ServerSession.PlaylistInfo();
        }

        public void OnPlaylistInfoResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            Items.Clear();
            m_ItemMarkedAsCurrent = null;

            foreach (MPDSongResponseBlock block in response)
            {
                SongMetadata song = new SongMetadata();
                song.Path = block.File;
                song.Album = block.Album;
                song.Artist = block.Artist;
                song.Date = m_DataModel.Database.DateNormalizer.Normalize(block.Date);
                song.Genre = block.Genre;
                song.Length = block.Time;
                song.Title = block.Title;
                song.Track = block.Track;
                PlaylistItem item = new PlaylistItem();
                item.Id = block.Id;
                item.Position = block.Pos;
                item.Playable = song;

                Items.Add(item);
            }

            UpdateCurrentSong();
        }

        private void UpdateCurrentSong()
        {
            PlaylistItem itemToMarkCurrent = null;

            if (m_DataModel.ServerStatus.OK && 
                (m_DataModel.ServerStatus.IsPaused || m_DataModel.ServerStatus.IsPlaying) &&
                m_DataModel.ServerStatus.CurrentSongIndex >= 0 &&
                m_DataModel.ServerStatus.CurrentSongIndex < Items.Count)
            {
                itemToMarkCurrent = Items[m_DataModel.ServerStatus.CurrentSongIndex];
            }

            if (itemToMarkCurrent != m_ItemMarkedAsCurrent)
            {
                if (m_ItemMarkedAsCurrent != null)
                {
                    m_ItemMarkedAsCurrent.IsPlaying = false;
                }

                m_ItemMarkedAsCurrent = itemToMarkCurrent;

                if (m_ItemMarkedAsCurrent != null)
                {
                    m_ItemMarkedAsCurrent.IsPlaying = true;
                }
            }

            if (!m_DataModel.ServerStatus.OK)
            {
                PlayStatusDescription = "";
            }
            else if (m_ItemMarkedAsCurrent == null || m_DataModel.ServerStatus.IsStopped)
            {
                PlayStatusDescription = "Stopped.";
            }
            else
            {
                string status = m_DataModel.ServerStatus.IsPlaying ? "Playing " : "Paused - ";

                if (m_ItemMarkedAsCurrent.Playable is SongMetadata)
                {
                    status += 
                          m_ItemMarkedAsCurrent.Playable.Artist + ": " +
                          m_ItemMarkedAsCurrent.Playable.Title + " (" +
                          m_ItemMarkedAsCurrent.Playable.Album;

                    if (m_ItemMarkedAsCurrent.Playable is SongMetadata)
                    {
                        SongMetadata song = (SongMetadata)m_ItemMarkedAsCurrent.Playable;

                        if (song.Year != null)
                        {
                            status += ", " + song.Year;
                        }
                    }

                    status += ").";
                }
                else
                {
                    status =
                         (m_DataModel.ServerStatus.IsPlaying ? "Playing " : "Paused - ") +
                          m_ItemMarkedAsCurrent.Playable.Title + ".";
                }

                PlayStatusDescription = status;
            }
            
            NotifyPropertyChanged("PlayStatusDescription");
        }

        private Playable PlayableByPath(string path)
        {
            Playable result = m_DataModel.Database.SongByPath(path);

            if (result == null)
            {
                result = m_DataModel.StreamsCollection.StreamByPath(path);
            }

            if (result == null)
            {
                result = new UnknownPlayable(path);
            }

            return result;
        }
    }
}
