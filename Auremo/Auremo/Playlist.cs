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

        ServerConnection m_Connection = null;
        ServerStatus m_ServerStatus = null;
        Database m_Database = null;
        StreamsCollection m_StreamsCollection = null;
        PlaylistItem m_ItemMarkedAsCurrent = null;

        public Playlist(ServerConnection connection, ServerStatus serverStatus, Database database, StreamsCollection streamsCollection)
        {
            m_Connection = connection;
            m_ServerStatus = serverStatus;
            m_Database = database;
            m_StreamsCollection = streamsCollection;

            Items = new ObservableCollection<PlaylistItem>();

            m_ServerStatus.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnServerStatusPropertyChanged);
        }

        public IList<PlaylistItem> Items
        {
            get;
            private set;
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
            }
            else if (e.PropertyName == "CurrentSongIndex" || e.PropertyName == "State")
            {
                UpdateCurrentSong();
            }
        }

        private void UpdateItems()
        {
            Items.Clear();
            m_ItemMarkedAsCurrent = null;

            if (!m_ServerStatus.OK)
                return;

            ServerResponse response = Protocol.PlaylistInfo(m_Connection);

            if (response == null || !response.IsOK)
                return;

            PlaylistItem item = new PlaylistItem();

            foreach (ServerResponseLine line in response.ResponseLines)
            {
                if (line.Name == "file")
                {
                    if (item.IsValid)
                    {
                        Items.Add(item);
                    }

                    item = new PlaylistItem();
                    item.Playable = PlayableByPath(line.Value);
                }
                else if (line.Name == "Id")
                {
                    int? id = Utils.StringToInt(line.Value);
                    item.Id = id.HasValue ? id.Value : -1;
                }
                else if (line.Name == "Pos")
                {
                    int? position = Utils.StringToInt(line.Value);
                    item.Position = position.HasValue ? position.Value : -1;
                }
            }

            if (item.IsValid)
            {
                Items.Add(item);
            }

            UpdateCurrentSong();
        }

        private void UpdateCurrentSong()
        {
            PlaylistItem itemToMarkCurrent = null;

            if (m_ServerStatus.OK && (m_ServerStatus.IsPaused.Value || m_ServerStatus.IsPlaying.Value) &&
                m_ServerStatus.CurrentSongIndex >= 0 && m_ServerStatus.CurrentSongIndex < Items.Count)
            {
                itemToMarkCurrent = Items[m_ServerStatus.CurrentSongIndex];
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

            if (!m_ServerStatus.OK)
            {
                PlayStatusDescription = "";
            }
            else if (m_ItemMarkedAsCurrent == null || m_ServerStatus.IsStopped.Value)
            {
                PlayStatusDescription = "Stopped.";
            }
            else
            {
                string status = m_ServerStatus.IsPlaying.Value ? "Playing " : "Paused - ";

                if (m_ItemMarkedAsCurrent.Playable is SongMetadata)
                {
                    status += 
                          m_ItemMarkedAsCurrent.Playable.Artist + ": " +
                          m_ItemMarkedAsCurrent.Playable.Title + " (" +
                          m_ItemMarkedAsCurrent.Playable.Album;

                    if (m_ItemMarkedAsCurrent.Playable.Year.HasValue)
                    {
                        status += ", " + m_ItemMarkedAsCurrent.Playable.Year.Value;
                    }

                    status += ").";
                }
                else if (m_ItemMarkedAsCurrent.Playable is StreamMetadata)
                {
                    status =
                         (m_ServerStatus.IsPlaying.Value ? "Playing " : "Paused - ") +
                          m_ItemMarkedAsCurrent.Playable.Title + ".";
                }

                PlayStatusDescription = status;
            }

            NotifyPropertyChanged("PlayStatusDescription");
        }

        private Playable PlayableByPath(string path)
        {
            Playable result = m_Database.SongByPath(path);

            if (result == null)
            {
                result = m_StreamsCollection.StreamByPath(path);
            }

            return result;
        }
    }
}
