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
    public class SavedPlaylists : INotifyPropertyChanged
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

        private string m_CurrentPlaylistName = "";

        public SavedPlaylists()
        {
            Playlists = new ObservableCollection<string>();
        }

        public void Refresh(ServerConnection connection)
        {
            Playlists.Clear();

            if (connection.Status == ServerConnection.State.Connected)
            {
                ServerResponse lsInfoResponse = Protocol.LsInfo(connection);

                if (lsInfoResponse.IsOK)
                {
                    foreach (ServerResponseLine line in lsInfoResponse.ResponseLines)
                    {
                        if (line.Name == "playlist")
                        {
                            Playlists.Add(line.Value);
                        }
                    }
                }
            }
        }

        public IList<string> Playlists
        {
            get;
            private set;
        }

        public string CurrentPlaylistName
        {
            get
            {
                return m_CurrentPlaylistName;
            }
            set
            {
                if (value != m_CurrentPlaylistName)
                {
                    m_CurrentPlaylistName = value;
                    NotifyPropertyChanged("CurrentPlaylistName");
                    NotifyPropertyChanged("CurrentPlaylistNameEmpty");
                    NotifyPropertyChanged("CurrentPlaylistNameNonempty");
                }
            }
        }

        public bool CurrentPlaylistNameEmpty
        {
            get
            {
                return CurrentPlaylistName.Trim().Length == 0;
            }
        }

        public bool CurrentPlaylistNameNonempty
        {
            get
            {
                return !CurrentPlaylistNameEmpty;
            }
        }
    }
}
