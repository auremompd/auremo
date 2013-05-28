using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class CollectionSearchThread : INotifyPropertyChanged
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

        public class SearchResultTuple
        {
            public SongMetadata Song { get; set; }
            public string Artist { get; set; }
            public AlbumMetadata Album { get; set; }
        };

        private Database m_Database = null;
        IList<SongMetadata> m_RunningSearchResults = new ObservableCollection<SongMetadata>();

        public CollectionSearchThread(Database database)
        {
            m_Database = database;
        }

        private string m_SearchString = "";
        
        public string SearchString
        {
            get
            {
                return m_SearchString;
            }
            set
            {
                m_SearchString = value.ToLower();
                Search();
            }
        }

        public IList<SearchResultTuple> SearchResults
        {
            get;
            private set;
        }

        private void Search()
        {
            string searchString = SearchString;
            IList<SearchResultTuple> results = new ObservableCollection<SearchResultTuple>();

            if (searchString != "")
            {
                IEnumerable<SongMetadata> allSongs = m_Database.Songs;
                
                foreach (SongMetadata song in allSongs)
                {
                    if (song.Artist.ToLower().Contains(searchString) || song.Album.ToLower().Contains(searchString) || song.Title.ToLower().Contains(searchString))
                    {
                        SearchResultTuple result = new SearchResultTuple();
                        result.Song = song;
                        result.Album = m_Database.AlbumOfSong(song);
                        result.Artist = song.Artist;
                        results.Add(result);
                    }
                }
            }

            SearchResults = results;
            NotifyPropertyChanged("SearchResults");
        }
    }
}
