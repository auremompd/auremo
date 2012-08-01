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
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    class ServerStatus : INotifyPropertyChanged
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

        public ServerStatus()
        {
            Reset();
        }

        public void Update(ServerConnection connection)
        {
            ServerResponse statusResponse = null;
            bool ok = false;

            if (connection != null && connection.Status == ServerConnection.State.Connected)
            {
                statusResponse = Protocol.Status(connection);
                ok = statusResponse != null && statusResponse.IsOK;
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

                foreach (ServerResponseLine line in statusResponse.Lines)
                {
                    if (line.Name == "state")
                    {
                        if (m_State != line.Value)
                        {
                            StateInternal = line.Value;
                        }
                    }
                    else if (line.Name == "volume")
                    {
                        int? volume = Utils.StringToInt(line.Value);

                        if (volume == null || volume.Value < 0 || volume > 100)
                        {
                            VolumeInternal = null;
                        }
                        else
                        {
                            VolumeInternal = volume;
                        }
                    }
                    else if (line.Name == "playlist")
                    {
                        int? version = Utils.StringToInt(line.Value);
                        PlaylistVersionInternal = version.HasValue ? version.Value : -1;
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
                        RandomInternal = line.Value == "1";
                    }
                    else if (line.Name == "repeat")
                    {
                        RepeatInternal = line.Value == "1";
                    }
                }

                CurrentSongIndexInternal = currentSongIndex;
                PlayPositionInternal = playPosition;
                SongLengthlInternal = songLength;
            }
        }

        private bool m_OK = false;
        public bool OK
        {
            get { return m_OK; }
        }

        public bool IsPlaying
        {
            get { return m_State == "play"; }
        }

        public bool IsPaused
        {
            get { return m_State == "pause"; }
        }

        public bool IsStopped
        {
            get { return m_State == "stop"; }
        }

        private int? m_Volume = null;
        public int? Volume
        {
            get { return m_Volume; }
        }

        private int m_PlaylistVersion = -1;
        public int PlaylistVersion
        {
            get { return m_PlaylistVersion; }
        }

        private int m_CurrentSongIndex = -1;
        public int CurrentSongIndex
        {
            get { return m_CurrentSongIndex; }
        }

        private int m_PlayPosition = 0;
        public int PlayPosition
        {
            get { return m_PlayPosition; }
        }

        private int m_SongLength = 0;
        public int SongLength
        {
            get { return m_SongLength; }
        }

        private bool m_Random = false;
        public bool Random
        {
            get { return m_Random; }
        }

        private bool m_Repeat = false;
        public bool Repeat
        {
            get { return m_Repeat; }
        }

        private string m_State = "";
        public string State
        {
            get { return m_State; }
        }

        private void Reset()
        {
            OKInternal = false;
            VolumeInternal = null;
            PlaylistVersionInternal = -1;
            CurrentSongIndexInternal = -1;
            PlayPositionInternal = 0;
            SongLengthlInternal = 1;
            RandomInternal = false;
            RepeatInternal = false;
            StateInternal = "";
        }

        #region XInternal properties, for keeping setters private.

        private bool OKInternal
        {
            set
            {
                if (m_OK != value)
                {
                    m_OK = value;
                    NotifyPropertyChanged("OK");
                }
            }
        }

        private int? VolumeInternal
        {
            set
            {
                if (m_Volume != value)
                {
                    m_Volume = value;
                    NotifyPropertyChanged("Volume");
                }
            }
        }

        private int PlaylistVersionInternal
        {
            set
            {
                if (m_PlaylistVersion != value)
                {
                    m_PlaylistVersion = value;
                    NotifyPropertyChanged("PlaylistVersion");
                }
            }
        }

        private int CurrentSongIndexInternal
        {
            set
            {
                if (m_CurrentSongIndex != value)
                {
                    m_CurrentSongIndex = value;
                    NotifyPropertyChanged("CurrentSongIndex");
                }
            }
        }

        private int PlayPositionInternal
        {
            set
            {
                if (m_PlayPosition != value)
                {
                    m_PlayPosition = value;
                    NotifyPropertyChanged("PlayPosition");
                }
            }
        }

        private int SongLengthlInternal
        {
            set
            {
                if (m_SongLength != value)
                {
                    m_SongLength = value;
                    NotifyPropertyChanged("SongLength");
                }
            }
        }

        private bool RandomInternal
        {
            set
            {
                if (m_Random != value)
                {
                    m_Random = value;
                    NotifyPropertyChanged("Random");
                }
            }
        }

        private bool RepeatInternal
        {
            set
            {
                if (m_Repeat != value)
                {
                    m_Repeat = value;
                    NotifyPropertyChanged("Repeat");
                }
            }
        }

        private string StateInternal
        {
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    NotifyPropertyChanged("State");
                    NotifyPropertyChanged("IsPlaying");
                    NotifyPropertyChanged("IsPaused");
                    NotifyPropertyChanged("IsStopped");
                }
            }
        }

        #endregion
    }
}
