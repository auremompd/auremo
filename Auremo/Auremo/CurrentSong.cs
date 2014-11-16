/*
 * Copyright 2014 Mikko Teräs and Niilo Säämänen.
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
using System.Windows.Media;

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
        private string m_DisplayString = "";
        private Playable m_CurrentlyPlaying = null;
        private AlbumMetadata m_CurrentAlbum = null;
        private AlbumMetadata m_UnknownAlbum = null;

        public CurrentSong(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_DataModel.ServerStatus.PropertyChanged += new PropertyChangedEventHandler(OnServerStatusPropertyChanged);
            
            m_UnknownAlbum = new AlbumMetadata(SongMetadata.UnknownArtist, SongMetadata.UnknownAlbum, null);
            m_DataModel.CoverArtRepository.SetCoverOfAlbum(m_UnknownAlbum);

            Update();
            BuildDisplayString();
        }

        public void Update()
        {
            m_DataModel.ServerSession.CurrentSong();
        }

        public void OnCurrentSongResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            if (response.Count() > 0)
            {
                CurrentlyPlaying = response.First().ToPlayable(m_DataModel);

                if (CurrentlyPlaying is SongMetadata)
                {
                    AlbumMetadata album = m_DataModel.Database.AlbumOfSong(CurrentlyPlaying as SongMetadata);
                    CurrentAlbum = album == null ? m_UnknownAlbum : album;
                    m_DataModel.CoverArtRepository.SetCoverOfAlbum(album);
                }
                else
                {
                    CurrentAlbum = m_UnknownAlbum;
                }

                BuildDisplayString();
            }
        }

        public Playable CurrentlyPlaying
        {
            get
            {
                return m_CurrentlyPlaying;
            }
            private set
            {
                if (value != m_CurrentlyPlaying)
                {
                    m_CurrentlyPlaying = value;
                    NotifyPropertyChanged("CurrentlyPlaying");
                }
            }
        }

        public AlbumMetadata CurrentAlbum
        {
            get
            {
                return m_CurrentAlbum;
            }
            private set
            {
                if (value != m_CurrentAlbum)
                {
                    m_CurrentAlbum = value;
                    NotifyPropertyChanged("CurrentAlbum");
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
            }
            else if (e.PropertyName == "State")
            {
                BuildDisplayString();
            }
        }

        private void BuildDisplayString()
        {
            if (m_DataModel.ServerStatus.IsPlaying)
            {
                if (m_CurrentlyPlaying == null)
                {
                    DisplayString = "Playing. " + m_DataModel.ServerStatus.AudioQuality;
                }
                else
                {
                    DisplayString = "Playing " + CurrentPlayableToString() + ". " + m_DataModel.ServerStatus.AudioQuality;
                }
            }
            else if (m_DataModel.ServerStatus.IsPaused)
            {
                if (m_CurrentlyPlaying == null)
                {
                    DisplayString = "Paused. " + m_DataModel.ServerStatus.AudioQuality;
                }
                else
                {
                    DisplayString = "Paused " + CurrentPlayableToString() + ". " + m_DataModel.ServerStatus.AudioQuality;
                }
            }
            else if (m_DataModel.ServerStatus.IsStopped)
            {
                DisplayString = "Stopped.";
            }
        }

        private string CurrentPlayableToString()
        {
            if (m_CurrentlyPlaying is SongMetadata)
            {
                return CurrentSongToString();
            }
            else if (m_CurrentlyPlaying is StreamMetadata)
            {
                return CurrentStreamToString();
            }
            else
            {
                return m_CurrentlyPlaying.Path;
            }
        }

        private string CurrentSongToString()
        {
            SongMetadata song = m_CurrentlyPlaying as SongMetadata;
            StringBuilder result = new StringBuilder(); 

            result.Append(song.Artist);
            result.Append(": ");
            result.Append(song.Title);
            result.Append(" (");
            result.Append(song.Album);

            if (song.Year != null)
            {
                result.Append(", ");
                result.Append(song.Year);
            }

            result.Append(")");

            return result.ToString();
        }

        private string CurrentStreamToString()
        {
            StreamMetadata stream = m_CurrentlyPlaying as StreamMetadata;
            StringBuilder result = new StringBuilder();

            if (stream.Title != null)
            {
                result.Append(stream.Title);
                {
                    if (stream.Name != null)
                    {
                        result.Append(" - ");
                        result.Append(stream.Name);
                    }
                }

                result.Append(" (");
                result.Append(stream.Path);
                result.Append(")");
            }
            else if (stream.Name != null)
            {
                result.Append(stream.Name);
                result.Append(" (");
                result.Append(stream.Path);
                result.Append(")");
            }
            else if (stream.Label != null)
            {
                result.Append(stream.Label);
                result.Append(" (");
                result.Append(stream.Path);
                result.Append(")");
            }
            else
            {
                result.Append(stream.Path);
            }

            return result.ToString();
        }
    }
}
