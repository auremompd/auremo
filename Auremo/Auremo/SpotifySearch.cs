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
        IDictionary<string, IDictionary<string, IDictionary<int, SongMetadata>>> m_ResultSorter = new SortedDictionary<string, IDictionary<string, IDictionary<int, SongMetadata>>>();

        public SpotifySearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            string[] dateFormat = { "YYYY" };
            m_DateNormalizer = new DateNormalizer(dateFormat);
            SearchResults = new ObservableCollection<MusicCollectionItem>();
        }

        public void Search(string what)
        {
            SearchResults.Clear();
            m_DataModel.ServerSession.Search("any", what);
        }

        public void OnSearchResponseReceived(IEnumerable<MPDSongResponseBlock> response)
        {
            m_ResultSorter.Clear();

            foreach (MPDSongResponseBlock item in response)
            {
                Playable playable = item.ToPlayable(m_DateNormalizer);

                if (playable != null && playable is SongMetadata)
                {
                    SongMetadata song = playable as SongMetadata;

                    if (song.IsSpotify)
                    {
                        PlaceSongInSorter(song);
                    }
                }
            }

            foreach (IDictionary<string, IDictionary<int, SongMetadata>> discography in m_ResultSorter.Values)
            {
                foreach (IDictionary<int, SongMetadata> album in discography.Values)
                {
                    foreach (SongMetadata song in album.Values)
                    {
                        SearchResults.Add(new MusicCollectionItem(song, SearchResults.Count));
                    }
                }
            }
        }

        public IList<MusicCollectionItem> SearchResults
        {
            get;
            private set;
        }

        private void PlaceSongInSorter(SongMetadata song)
        {
            // Sort first by artist, then by album and finally by track#.
            if (!m_ResultSorter.ContainsKey(song.Artist))
            {
                m_ResultSorter[song.Artist] = new SortedDictionary<string, IDictionary<int, SongMetadata>>();
            }

            IDictionary<string, IDictionary<int, SongMetadata>> discography = m_ResultSorter[song.Artist];

            if (!discography.ContainsKey(song.Album))
            {
                discography[song.Album] = new SortedDictionary<int, SongMetadata>();
            }

            IDictionary<int, SongMetadata> album = discography[song.Album];
            int track = song.Track.HasValue ? song.Track.Value : 0;
            album[track] = song;
        }
    }
}
