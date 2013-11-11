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
    public class AdvancedSearch
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
        private bool m_IncludeLocal = true;
        private bool m_IncludeSpotify = true;
        private bool m_IncludeSoundCloud = true;
        private SearchType m_SearchType = SearchType.Any;
        IDictionary<string, IDictionary<string, IDictionary<int, SongMetadata>>> m_ResultSorter = new SortedDictionary<string, IDictionary<string, IDictionary<int, SongMetadata>>>();

        public AdvancedSearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            string[] dateFormat = { "YYYY" };
            m_DateNormalizer = new DateNormalizer(dateFormat);
            SearchResults = new ObservableCollection<MusicCollectionItem>();
        }

        public void Search(string what)
        {
            SearchResults.Clear();
            string type = m_SearchType.ToString().ToLowerInvariant();
            m_DataModel.ServerSession.Search(type, what);
        }

        public IList<MusicCollectionItem> SearchResults
        {
            get;
            private set;
        }

        public bool IncludeLocal
        {
            get
            {
                return m_IncludeLocal;
            }
            set
            {
                if (m_IncludeLocal != value)
                {
                    m_IncludeLocal = value;
                    NotifyPropertyChanged("IncludeLocal");
                }
            }
        }

        public bool IncludeSpotify
        {
            get
            {
                return m_IncludeSpotify;
            }
            set
            {
                if (m_IncludeSpotify != value)
                {
                    m_IncludeSpotify = value;
                    NotifyPropertyChanged("IncludeSpotify");
                }
            }
        }

        public bool IncludeSoundCloud
        {
            get
            {
                return m_IncludeSoundCloud;
            }
            set
            {
                if (m_IncludeSoundCloud != value)
                {
                    m_IncludeSoundCloud = value;
                    NotifyPropertyChanged("IncludeSoundCloud");
                }
            }
        }

        public bool SearchByAny
        {
            get
            {
                return m_SearchType == SearchType.Any;
            }
            set
            {
                if (value && m_SearchType != SearchType.Any)
                {
                    m_SearchType = SearchType.Any;
                    NotifyPropertyChanged("SearchByAny");
                }
            }
        }

        public bool SearchByArtist
        {
            get
            {
                return m_SearchType == SearchType.Artist;
            }
            set
            {
                if (value && m_SearchType != SearchType.Artist)
                {
                    m_SearchType = SearchType.Artist;
                    NotifyPropertyChanged("SearchByArtist");
                }
            }
        }

        public bool SearchByAlbum
        {
            get
            {
                return m_SearchType == SearchType.Album;
            }
            set
            {
                if (value && m_SearchType != SearchType.Album)
                {
                    m_SearchType = SearchType.Album;
                    NotifyPropertyChanged("SearchByAlbum");
                }
            }
        }

        public bool SearchByTitle
        {
            get
            {
                return m_SearchType == SearchType.Title;
            }
            set
            {
                if (value && m_SearchType != SearchType.Title)
                {
                    m_SearchType = SearchType.Title;
                    NotifyPropertyChanged("SearchByTitle");
                }
            }
        }


        public void OnSearchResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            m_ResultSorter.Clear();

            foreach (MPDSongResponseBlock item in response)
            {
                Playable playable = item.ToPlayable(m_DateNormalizer);

                if (playable != null && !(playable is UnknownPlayable))
                {
                    SearchResults.Add(new MusicCollectionItem(playable, SearchResults.Count));
                }
            }
        }

        private bool IncludeSongInResults(SongMetadata song)
        {
            return song.IsLocal && IncludeLocal ||
                song.IsSpotify && IncludeSpotify ||
                song.IsSoundCloud && IncludeSpotify;
        }
    }
}
