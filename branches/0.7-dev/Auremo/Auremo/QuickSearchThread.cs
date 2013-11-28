using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Auremo
{
    public class QuickSearchThread
    {
        QuickSearch m_Owner = null;
        Database m_Database = null;
        object m_Lock = new object();
        string[] m_Fragments = new string[0];
        bool m_SearchChanged = false;
        bool m_Terminating = false;
        private ManualResetEvent m_Event = new ManualResetEvent(false);

        public QuickSearchThread(QuickSearch owner, Database database)
        {
            m_Owner = owner;
            m_Database = database;
        }

        public void Start()
        {
            IEnumerable<SongMetadata> allSongs = m_Database.Songs;
            IList<SongMetadata> newResults = new List<SongMetadata>();

            string[] fragments = new string[0];
            bool searchChanged = false;
            bool terminating = false;

            while (!terminating)
            {
                lock (m_Lock)
                {
                    fragments = m_Fragments;
                    searchChanged = m_SearchChanged;
                    terminating = m_Terminating;
                    m_SearchChanged = false;
                    m_Event.Reset();
                }

                if (!terminating)
                {
                    if (searchChanged && fragments.Count() > 0)
                    {
                        DateTime lastUpdate = DateTime.MinValue;
                        newResults.Clear();

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
                                newResults.Add(song);

                                if (DateTime.Now.Subtract(lastUpdate).TotalMilliseconds > 250 && newResults.Count > 0)
                                {
                                    lock (m_Lock)
                                    {
                                        searchChanged = m_SearchChanged;
                                        terminating = m_Terminating;
                                        m_Event.Reset();
                                    }

                                    if (terminating || searchChanged)
                                    {
                                        break;
                                    }

                                    m_Owner.AddSearchResults(newResults);
                                    newResults.Clear();
                                    lastUpdate = DateTime.Now;
                                }
                            }
                        }
                    }

                    if (!terminating && !searchChanged)
                    {
                        if (newResults.Count > 0)
                        {
                            m_Owner.AddSearchResults(newResults);
                            newResults.Clear();
                        }

                        m_Event.WaitOne();
                    }
                }
            }
        }

        public void OnSearchStringChanged(string[] fragments)
        {
            lock (m_Lock)
            {
                m_Fragments = fragments;
                m_SearchChanged = true;
                m_Event.Set();
            }
        }

        public void Terminate()
        {
            lock (m_Lock)
            {
                m_Terminating = true;
                m_Event.Set();
            }
        }
    }
}
