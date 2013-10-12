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
    public class SpotifySearch
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

        private DataModel m_DataModel = null;
        private DateNormalizer m_DateNormalizer = null;

        public SpotifySearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            string[] dateFormat = { "YYYY" };
            m_DateNormalizer = new DateNormalizer(dateFormat);
            SearchResults = new ObservableCollection<Playable>();
        }

        public void Search(string what)
        {
            SearchResults.Clear();
            m_DataModel.ServerSession.Search("any", what);
        }

        public void OnSearchResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            foreach (MPDSongResponseBlock item in response)
            {
                SongMetadata song = new SongMetadata(item, m_DateNormalizer);

                if (song.IsSpotify)
                {
                    SearchResults.Add(song);
                }
            }
        }

        public IList<Playable> SearchResults
        {
            get;
            private set;
        }
    }
}
