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
    public class ServerStatus : INotifyPropertyChanged
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

        private bool m_OK = false;
        private int? m_Volume = null;
        private int m_PlaylistVersion = -1;
        private int m_CurrentSongIndex = -1;
        private int m_PlayPosition = 0;
        private int m_SongLength = 0;
        private string m_State = "";
        private int m_DatabaseUpdateTime = 0;

        public ServerStatus()
        {
            Reset();
        }

        public void Update(ServerConnection connection)
        {
            ReadStatus(connection);
            ReadStats(connection);
        }

        private void ReadStatus(ServerConnection connection)
        {
            ServerResponse response = null;
            bool ok = false;

            if (connection != null && connection.Status == ServerConnection.State.Connected)
            {
                response = Protocol.Status(connection);
                ok = response != null && response.IsOK;
            }

            if (!ok && m_OK)
            {
                // The server disappeared.
                Reset();
            }

            m_OK = ok;

            if (m_OK)
            {
                // Not all the values checked for below are always
                // present. Handle them specially so that defaults
                // can be provided.
                int currentSongIndex = -1;
                int playPosition = 0;
                int songLength = 0;

                foreach (ServerResponseLine line in response.ResponseLines)
                {
                    if (line.Name == "state")
                    {
                        if (m_State != line.Value)
                        {
                            State = line.Value;
                        }
                    }
                    else if (line.Name == "volume")
                    {
                        int? volume = Utils.StringToInt(line.Value);

                        if (volume == null || volume.Value < 0 || volume > 100)
                        {
                            Volume = null;
                        }
                        else
                        {
                            Volume = volume;
                        }
                    }
                    else if (line.Name == "playlist")
                    {
                        int? version = Utils.StringToInt(line.Value);
                        PlaylistVersion = version.HasValue ? version.Value : -1;
                    }
                    else if (line.Name == "song")
                    {
                        int? index = Utils.StringToInt(line.Value);
                        currentSongIndex = index.HasValue ? index.Value : -1;
                    }
                    else if (line.Name == "time")
                    {
                        string[] pieces = line.Value.Split(':');

                        if (pieces.Length == 2)
                        {
                            int? position = Utils.StringToInt(pieces[0]);
                            int? length = Utils.StringToInt(pieces[1]);

                            if (position.HasValue && length.HasValue)
                            {
                                playPosition = position.Value;
                                songLength = length.Value;
                            }
                        }
                    }
                    else if (line.Name == "random")
                    {
                        IsOnRandom = line.Value == "1";
                    }
                    else if (line.Name == "repeat")
                    {
                        IsOnRepeat = line.Value == "1";
                    }
                }

                CurrentSongIndex = currentSongIndex;
                PlayPosition = playPosition;
                SongLength = songLength;
            }
        }

        private void ReadStats(ServerConnection connection)
        {
            ServerResponse response = null;
            bool ok = false;

            if (connection != null && connection.Status == ServerConnection.State.Connected)
            {
                response = Protocol.Stats(connection);
                ok = response != null && response.IsOK;
            }

            if (!ok && m_OK)
            {
                // The server disappeared.
                Reset();
            }

            m_OK = ok;

            if (m_OK)
            {
                foreach (ServerResponseLine line in response.ResponseLines)
                {
                    if (line.Name == "db_update")
                    {
                        int? time = Utils.StringToInt(line.Value);
                        DatabaseUpdateTime = time.HasValue ? time.Value : -1;
                        return; // We're not interested in anything else right now.
                    }
                }
            }
        }

        public bool OK
        {
            get
            {
                return m_OK;
            }
            private set
            {
                if (m_OK != value)
                {
                    m_OK = value;
                    NotifyPropertyChanged("OK");
                }
            }
        }

        public string State
        {
            get
            {
                return m_State;
            }
            private set
            {
                if (m_State != value)
                {
                    m_State = value;
                    IsPlaying = m_State == "play";
                    IsPaused = m_State == "pause";
                    IsStopped = m_State == "stop";
                    NotifyPropertyChanged("State");
                }
            }
        }

        bool m_IsPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            private set
            {
                if (value != m_IsPlaying)
                {
                    m_IsPlaying = value;
                    NotifyPropertyChanged("IsPlaying");
                }
            }
        }

        bool m_IsPaused = false;
        public bool IsPaused
        {
            get
            {
                return m_IsPaused;
            }
            private set
            {
                if (value != m_IsPaused)
                {
                    m_IsPaused = value;
                    NotifyPropertyChanged("IsPaused");
                }
            }
        }

        bool m_IsStopped = false;
        public bool IsStopped
        {
            get
            {
                return m_IsStopped;
            }
            private set
            {
                if (value != m_IsStopped)
                {
                    m_IsStopped = value;
                    NotifyPropertyChanged("IsStopped");
                }
            }
        }

        bool m_IsOnRepeat = false;
        public bool IsOnRepeat
        {
            get
            {
                return m_IsOnRepeat;
            }
            private set
            {
                if (value != m_IsOnRepeat)
                {
                    m_IsOnRepeat = value;
                    NotifyPropertyChanged("IsOnRepeat");
                }
            }
        }

        bool m_IsOnRandom = false;
        public bool IsOnRandom
        {
            get
            {
                return m_IsOnRandom;
            }
            private set
            {
                if (value != m_IsOnRandom)
                {
                    m_IsOnRandom = value;
                    NotifyPropertyChanged("IsOnRandom");
                }
            }
        }

        public int? Volume
        {
            get
            {
                return m_Volume;
            }
            private set
            {
                if (m_Volume != value)
                {
                    m_Volume = value;
                    NotifyPropertyChanged("Volume");
                }
            }
        }

        public int PlaylistVersion
        {
            get
            {
                return m_PlaylistVersion;
            }
            private set
            {
                if (m_PlaylistVersion != value)
                {
                    m_PlaylistVersion = value;
                    NotifyPropertyChanged("PlaylistVersion");
                }
            }
        }

        public int CurrentSongIndex
        {
            get
            {
                return m_CurrentSongIndex;
            }
            private set
            {
                if (m_CurrentSongIndex != value)
                {
                    m_CurrentSongIndex = value;
                    NotifyPropertyChanged("CurrentSongIndex");
                }
            }
        }

        public int PlayPosition
        {
            get
            {
                return m_PlayPosition;
            }
            private set
            {
                if (m_PlayPosition != value)
                {
                    m_PlayPosition = value;
                    NotifyPropertyChanged("PlayPosition");
                }
            }
        }

        public int SongLength
        {
            get
            {
                return m_SongLength;
            }
            private set
            {
                if (m_SongLength != value)
                {
                    m_SongLength = value;
                    NotifyPropertyChanged("SongLength");
                }
            }
        }

        public int DatabaseUpdateTime
        {
            get
            {
                return m_DatabaseUpdateTime;
            }
            private set
            {
                if (value != m_DatabaseUpdateTime)
                {
                    m_DatabaseUpdateTime = value;
                    NotifyPropertyChanged("DatabaseUpdateTime");
                }
            }
        }

        private void Reset()
        {
            OK = false;
            Volume = null;
            PlaylistVersion = -1;
            CurrentSongIndex = -1;
            PlayPosition = 0;
            SongLength = 0;
            IsPlaying = false;
            IsPaused = false;
            IsStopped = false;
            IsOnRandom = false;
            IsOnRepeat = false;
            State = "";
            DatabaseUpdateTime = 0;
        }
    }
}
