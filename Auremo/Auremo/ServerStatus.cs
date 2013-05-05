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
        private NestedProperty<bool> m_IsPlaying = new NestedProperty<bool>(false);
        private NestedProperty<bool> m_IsPaused = new NestedProperty<bool>(false);
        private NestedProperty<bool> m_IsStopped = new NestedProperty<bool>(false);
        private NestedProperty<bool> m_IsOnRandom = new NestedProperty<bool>(false);
        private NestedProperty<bool> m_IsOnRepeat = new NestedProperty<bool>(false);

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
                int songLength = 1;

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
                        m_IsOnRandom.Value = line.Value == "1";
                    }
                    else if (line.Name == "repeat")
                    {
                        m_IsOnRepeat.Value = line.Value == "1";
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
                    NotifyPropertyChanged("State");
                    IsPlaying.Value = m_State == "play";
                    IsPaused.Value = m_State == "pause";
                    IsStopped.Value = m_State == "stop";
                }
            }
        }

        public NestedProperty<bool> IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
        }

        public NestedProperty<bool> IsPaused
        {
            get
            {
                return m_IsPaused;
            }
        }

        public NestedProperty<bool> IsStopped
        {
            get
            {
                return m_IsStopped;
            }
        }

        public NestedProperty<bool> IsOnRepeat
        {
            get
            {
                return m_IsOnRepeat;
            }
        }

        public NestedProperty<bool> IsOnRandom
        {
            get
            {
                return m_IsOnRandom;
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
            SongLength = 1;
            IsPlaying.Value = false;
            IsPaused.Value = false;
            IsStopped.Value = false;
            IsOnRandom.Value = false;
            IsOnRepeat.Value = false;
            State = "";
            DatabaseUpdateTime = 0;
        }
    }
}
