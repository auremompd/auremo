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
using System.Threading;

namespace Auremo
{
    public class CollectionSearch : INotifyPropertyChanged
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

        private DataModel m_DataModel = null;
        private CollectionSearchThread m_Searcher = null;
        private Thread m_Thread = null;

        object m_ThreadInputLock = new object();
        string m_SearchString = "";
        private ManualResetEvent m_ThreadEvent = new ManualResetEvent(false);
        bool m_ThreadShouldTerminate = false;

        object m_ThreadOutputLock = new object();
        IEnumerable<SearchResultTuple> m_SearchResults = new List<SearchResultTuple>();

        public CollectionSearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            m_Searcher = new CollectionSearchThread(this, m_DataModel.Database);
            m_Thread = new Thread(new ThreadStart(m_Searcher.Start));
            m_Thread.Start();
        }

        public string SearchString
        {
            private get
            {
                m_ThreadEvent.WaitOne();

                lock (m_ThreadInputLock)
                {
                    m_ThreadEvent.Reset();
                    return m_SearchString;
                }
            }
            set
            {
                lock (m_ThreadInputLock)
                {
                    m_ThreadEvent.Set();
                    m_SearchString = value.ToLower();
                }
            }
        }

        public IEnumerable<SearchResultTuple> SearchResults
        {
            get
            {
                lock (m_ThreadOutputLock)
                {
                    return m_SearchResults;
                }
            }
            private set
            {
                lock (m_ThreadOutputLock)
                {
                    m_SearchResults = new List<SearchResultTuple>(value);
                }

                NotifyPropertyChanged("SearchResults");
            }
        }

        public void Terminate()
        {
            ThreadShouldTerminate = true;
            m_Thread.Join();
        }

        private string SearchStringWithoutSignals
        {
            get
            {
                lock (m_ThreadInputLock)
                {
                    return m_SearchString;
                }
            }
        }

        private bool ThreadShouldTerminate
        {
            get
            {
                lock (m_ThreadInputLock)
                {
                    return m_ThreadShouldTerminate;
                }
            }
            set
            {
                lock (m_ThreadInputLock)
                {
                    m_ThreadShouldTerminate = value;
                    m_ThreadEvent.Set();
                }
            }
        }

        private class CollectionSearchThread
        {
            CollectionSearch m_Owner = null;
            Database m_Database = null;
            bool m_Terminating = false;

            public CollectionSearchThread(CollectionSearch owner, Database database)
            {
                m_Owner = owner;
                m_Database = database;
            }

            public void Start()
            {
                char[] delimiters = { ' ', '\t' };
                string[] previousFragments = new string[0];

                while (!m_Terminating)
                {
                    string searchString = m_Owner.SearchString;
                    m_Terminating = m_Owner.ThreadShouldTerminate;
                    DateTime lastUpdate = DateTime.MinValue;
                    int lastElementCount = 0;

                    // Ignore if only spaces were added. This doesn't affect
                    // the results and decreases flickering (and unnecessary
                    // work).
                    string[] fragments = searchString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    bool allFragmentsAreEqual = previousFragments.Count() == fragments.Count();

                    for (int i = 0; allFragmentsAreEqual && i < fragments.Count(); ++i)
                    {
                        allFragmentsAreEqual = fragments[i] == previousFragments[i];
                    }
                    
                    if (allFragmentsAreEqual)
                    {
                        continue;
                    }

                    previousFragments = fragments;
                    IList<SearchResultTuple> results = new ObservableCollection<SearchResultTuple>();
                    m_Owner.SearchResults = results;

                    if (fragments.Count() > 0)
                    {
                        IEnumerable<SongMetadata> allSongs = m_Database.Songs;

                        foreach (SongMetadata song in allSongs)
                        {
                            bool allFragmentsMatch = true;

                            for (int i = 0; i < fragments.Count() && allFragmentsMatch; ++i)
                            {
                                string fragment = fragments[i];
                                allFragmentsMatch = song.Artist.ToLower().Contains(fragment) || song.Album.ToLower().Contains(fragment) || song.Title.ToLower().Contains(fragment);
                            }

                            if (allFragmentsMatch)
                            {
                                SearchResultTuple result = new SearchResultTuple();
                                result.Song = song;
                                result.Album = m_Database.AlbumOfSong(song);
                                result.Artist = song.Artist;
                                results.Add(result);

                                if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds > 250 && results.Count > lastElementCount)
                                {
                                    m_Owner.SearchResults = results;
                                    lastElementCount = results.Count;
                                    lastUpdate = DateTime.Now;
                                    m_Terminating = m_Owner.ThreadShouldTerminate;

                                    if (m_Terminating || searchString != m_Owner.SearchStringWithoutSignals)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (results.Count > lastElementCount)
                        {
                            m_Owner.SearchResults = results;
                        }
                    }
                }

                m_Terminating = true;
            }
        }
    }
}
