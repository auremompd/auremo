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

        private IDictionary<string, StreamMetadata> m_Streams = null;

        public StreamsCollection()
        {
            Load();
        }

        public void Load()
        {
            m_Streams = new SortedDictionary<string, StreamMetadata>();

            if (Settings.Default.KnownStreams.Length > 0)
            {
                PLSParser parser = new PLSParser();
                IEnumerable<StreamMetadata> streams = parser.ParseString(Settings.Default.KnownStreams);

                if (streams != null)
                {
                    foreach (StreamMetadata stream in streams)
                    {
                        Add(stream);
                    }
                }
            }

            NotifyPropertyChanged("Streams");
        }

        public void Save()
        {
            string playlist = PlaylistWriter.Write(m_Streams.Values);
            Settings.Default.KnownStreams = playlist == null ? "" : playlist;
            Settings.Default.Save();
        }

        public bool Add(StreamMetadata stream)
        {
            if (AddWithoutNotification(stream))
            {
                Save();
                NotifyPropertyChanged("Streams");
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
            NotifyPropertyChanged("Streams");
            return allSucceeded;
        }
        
        public bool Delete(StreamMetadata stream)
        {
            if (DeleteWithoutNotification(stream))
            {
                Save();
                NotifyPropertyChanged("Streams");
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
            NotifyPropertyChanged("Streams");
            return allSucceeded;
        }
        
        public void Rename(StreamMetadata stream, string newName)
        {
            m_Streams.Remove(stream.Path);
            stream.Title = newName;
            m_Streams.Add(stream.Path, stream);
            Save();
            NotifyPropertyChanged("Streams");
        }

        public IEnumerable<StreamMetadata> Streams
        {
            get
            {
                return m_Streams.Values;
            }
        }

        public StreamMetadata StreamByPath(string path)
        {
            StreamMetadata result;

            if (m_Streams.TryGetValue(path, out result))
            {
                return result;
            }

            return null;
        }

        private bool AddWithoutNotification(StreamMetadata stream)
        {
            if (m_Streams.ContainsKey(stream.Path))
            {
                return false;
            }

            m_Streams.Add(stream.Path, stream);
            return true;
        }

        private bool DeleteWithoutNotification(StreamMetadata stream)
        {
            if (!m_Streams.ContainsKey(stream.Path))
            {
                return false;
            }

            m_Streams.Remove(stream.Path);
            return true;
        }
    }
}
