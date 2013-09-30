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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class CurrentSong : INotifyPropertyChanged
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
        private string m_Album = null;
        private string m_Song = null;
        private string m_Date = null;
        private string m_DisplayString = "";

        public CurrentSong(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
            Update();
            BuildDisplayString();
        }

        public void Update()
        {
            if (m_DataModel.ServerConnection.Status == ServerConnection.State.Connected)
            {
                ServerResponse response = Protocol.CurrentSong(m_DataModel.ServerConnection);
                string path = null;
                m_Song = null;
                m_Album = null;
                m_Date = null;

                if (response != null && response.IsOK)
                {
                    foreach (ServerResponseLine line in response.ResponseLines)
                    {
                        if (line.Name == "file")
                        {
                            path = line.Value;
                        }
                        else if (line.Name == "Name" || line.Name == "Title")
                        {
                            m_Song = line.Value;
                        }
                        else if (line.Name == "Album")
                        {
                            m_Album = line.Value;
                        }
                        else if (line.Name == "Date")
                        {
                            m_Date = line.Value;
                        }
                    }
                }

                if (path != null)
                {
                    SongMetadata song = m_DataModel.Database.SongByPath(path);
                    
                    if (song != null)
                    {
                        m_Date = song.Year;
                        AlbumMetadata album = m_DataModel.Database.AlbumOfSong(song);

                        if (album != null)
                        {
                            m_Album = album.Title;
                        }
                    }
                }
            }
        }

        public string DisplayString
        {
            get
            {
                return m_DisplayString;
            }
            private set
            {
                if (value != m_DisplayString)
                {
                    m_DisplayString = value;
                    NotifyPropertyChanged("DisplayString");
                }
            }
        }

        private void OnServerStatusPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSongIndex" || e.PropertyName == "PlaylistVersion")
            {
                Update();
                BuildDisplayString();
            }
            else if (e.PropertyName == "State")
            {
                BuildDisplayString();
            }
        }

        private void BuildDisplayString()
        {
            StringBuilder state = new StringBuilder();

            if (!m_DataModel.ServerStatus.IsPlaying && !m_DataModel.ServerStatus.IsPaused)
            {
                state.Append("Stopped.");
            }
            else
            {
                if (m_Song == null)
                {
                    if (m_DataModel.ServerStatus.IsPlaying)
                    {
                        state.Append("Playing.");
                    }
                    else if (m_DataModel.ServerStatus.IsPaused)
                    {
                        state.Append("Paused.");
                    }
                }
                else
                {
                    if (m_DataModel.ServerStatus.IsPlaying)
                    {
                        state.Append("Playing ");
                    }
                    else if (m_DataModel.ServerStatus.IsPaused)
                    {
                        state.Append("Paused - ");
                    }

                    state.Append(m_Song);

                    if (m_Album != null || m_Date != null)
                    {
                        state.Append(" (");

                        if (m_Album != null)
                        {
                            state.Append(m_Album);

                            if (m_Date != null)
                            {
                                state.Append(", ");
                            }
                        }

                        if (m_Date != null)
                        {
                            state.Append(m_Date);
                        }

                        state.Append(")");
                    }

                    state.Append(".");
                }

                DisplayString = state.ToString();
            }
        }
    }
}
