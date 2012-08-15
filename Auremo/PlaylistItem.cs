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
    public class PlaylistItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        // At present only the IsPlaying property sends notifications.
        // All other properties should be set to their final state
        // before the playlist item is inserted into the ListView.
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        public PlaylistItem()
        {
        }

        private SongMetadata m_Song = null;
        public SongMetadata Song
        {
            get
            {
                return m_Song;
            }
            set
            {
                m_Song = value;
            }
        }

        private int m_Id = -1;
        public int Id
        {
            get
            {
                return m_Id;
            }
            set
            {
                m_Id = value;
            }
        }

        private bool m_IsPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            set
            {
                if (m_IsPlaying != value)
                {
                    m_IsPlaying = value;
                    NotifyPropertyChanged("IsPlaying");
                }
            }
        }

        public bool IsValid
        {
            get
            {
                return m_Song != null && m_Id >= 0;
            }
        }

        public override string ToString()
        {
            return Id + " - " + Song.ToString();
        }
    }
}
