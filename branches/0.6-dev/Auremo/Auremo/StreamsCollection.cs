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
using Auremo.Properties;

namespace Auremo
{
    public class StreamsCollection : INotifyPropertyChanged
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

        private IDictionary<string, StreamMetadata> m_StreamsByTitle = new SortedDictionary<string, StreamMetadata>(StringComparer.CurrentCultureIgnoreCase);

        public StreamsCollection()
        {
            Streams = new ObservableCollection<StreamMetadata>();
            Load();
        }

        public IList<StreamMetadata> Streams
        {
            get;
            private set;
        }

        public void Load()
        {
            m_StreamsByTitle.Clear();

            if (Settings.Default.KnownStreams.Length > 0)
            {
                PLSParser parser = new PLSParser();
                IEnumerable<StreamMetadata> streams = parser.ParseString(Settings.Default.KnownStreams);

                if (streams != null)
                {
                    Add(streams);
                }
            }
        }

        public void Save()
        {
            string playlist = PlaylistWriter.Write(m_StreamsByTitle.Values);
            Settings.Default.KnownStreams = playlist == null ? "" : playlist;
            Settings.Default.Save();
        }

        public bool Add(StreamMetadata stream)
        {
            if (AddWithoutNotification(stream))
            {
                Save();
                UpdateStreamsView();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Add(IEnumerable<StreamMetadata> streams)
        {
            bool allSucceeded = true;

            foreach (StreamMetadata stream in streams)
            {
                bool succeeded = AddWithoutNotification(stream);
                allSucceeded &= succeeded;
            }

            Save();
            UpdateStreamsView();
            return allSucceeded;
        }
        
        public bool Delete(StreamMetadata stream)
        {
            if (DeleteWithoutNotification(stream))
            {
                Save();
                UpdateStreamsView();
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool Delete(IEnumerable<StreamMetadata> streams)
        {
            bool allSucceeded = true;

            foreach (StreamMetadata stream in streams)
            {
                bool succeeded = DeleteWithoutNotification(stream);
                allSucceeded &= succeeded;
            }

            Save();
            UpdateStreamsView();
            return allSucceeded;
        }
        
        public bool Rename(StreamMetadata stream, string newName)
        {
            if (m_StreamsByTitle.ContainsKey(newName))
            {
                return false;
            }
            else
            {
                m_StreamsByTitle.Remove(stream.Title);
                stream.Title = newName;
                m_StreamsByTitle.Add(stream.Title, stream);
                Save();
                UpdateStreamsView();
                return true;
            }
        }

        public StreamMetadata StreamByPath(string path)
        {
            foreach (StreamMetadata stream in m_StreamsByTitle.Values)
            {
                if (stream.Path == path)
                {
                    return stream;
                }
            }

            return null;
        }

        private bool AddWithoutNotification(StreamMetadata stream)
        {
            if (m_StreamsByTitle.ContainsKey(stream.Title))
            {
                return false;
            }

            m_StreamsByTitle.Add(stream.Title, stream);
            return true;
        }

        private bool DeleteWithoutNotification(StreamMetadata stream)
        {
            if (m_StreamsByTitle.ContainsKey(stream.Title))
            {
                m_StreamsByTitle.Remove(stream.Title);
                return true;    
            }

            return false;
        }

        private void UpdateStreamsView()
        {
            Streams.Clear();

            foreach (StreamMetadata stream in m_StreamsByTitle.Values)
            {
                Streams.Add(stream);
            }
        }
    }
}
