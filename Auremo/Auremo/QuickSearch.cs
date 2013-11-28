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
    public class QuickSearch : INotifyPropertyChanged
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
        private QuickSearchThread m_Searcher = null;
        private Thread m_Thread = null;

        object m_Lock = new object();
        string m_SearchString = "";
        string[] m_SearchStringFragments = new string[0];
        IList<SongMetadata> m_NewResults = new List<SongMetadata>();

        public QuickSearch(DataModel dataModel)
        {
            m_DataModel = dataModel;
            SearchResults = new ObservableCollection<MusicCollectionItem>();
            SelectedSearchResults = new ObservableCollection<MusicCollectionItem>();
            m_Searcher = new QuickSearchThread(this, m_DataModel.Database);
            m_Thread = new Thread(new ThreadStart(m_Searcher.Start));
            m_Thread.Name = "QuickSearch thread";
            m_Thread.Start();
        }

        public string SearchString
        {
            get
            {
                return m_SearchString;
            }
            set
            {
                if (value != m_SearchString)
                {
                    m_SearchString = value;
                    NotifyPropertyChanged("SearchString");
                    bool searchChanged = UpdateSearchStringFragments(value);

                    if (searchChanged && m_Searcher != null)
                    {
                        lock (m_Lock)
                        {
                            m_Searcher.OnSearchStringChanged(m_SearchStringFragments);
                            m_NewResults.Clear();
                            SearchResults.Clear();
                            
                        }
                    }
                }
            }
        }

        public ObservableCollection<MusicCollectionItem> SearchResults
        {
            get;
            private set;
        }

        public IList<MusicCollectionItem> SelectedSearchResults
        {
            get;
            private set;
        }

        public void UpdateSelectedSearchResults()
        {
            SelectedSearchResults.Clear();

            foreach (MusicCollectionItem item in SearchResults)
            {
                if (item.IsSelected)
                {
                    SelectedSearchResults.Add(item);
                }
            }
        }

        public void Terminate()
        {
            if (m_Searcher != null)
            {
                m_Searcher.Terminate();
                m_Thread.Join();
                m_Searcher = null;
                m_Thread = null;
            }
        }

        public void AddSearchResults(IEnumerable<SongMetadata> results)
        {
            lock (m_Lock)
            {
                foreach (SongMetadata result in results)
                {
                    m_NewResults.Add(result);
                }
            }

            m_DataModel.MainWindow.Dispatcher.BeginInvoke((Action)OnNewSearchResultsReceived, null);
        }

        private void OnNewSearchResultsReceived()
        {
            lock (m_Lock)
            {
                foreach (SongMetadata result in m_NewResults)
                {
                    SearchResults.Add(new MusicCollectionItem(result, SearchResults.Count));
                }

                m_NewResults.Clear();
            }
        }

        private bool UpdateSearchStringFragments(string search)
        {
            char[] delimiters = { ' ', '\t' };
            string[] fragments = search.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (!Utils.CollectionsAreEqual(m_SearchStringFragments, fragments))
            {
                m_SearchStringFragments = fragments;
                return true;
            }

            return false;
        }
    }
}
