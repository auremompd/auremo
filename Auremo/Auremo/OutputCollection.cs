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
    public class OutputCollection : INotifyPropertyChanged
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

        public OutputCollection()
        {
        }

        private IList<Output> m_Items = new ObservableCollection<Output>();

        public IList<Output> Items
        {
            get
            {
                return m_Items;
            }
            private set
            {
                if (m_Items != value)
                {
                    m_Items = value;
                    NotifyPropertyChanged("Items");
                }
            }
        }

        public void Update(ServerConnection connection)
        {
            ServerResponse response = null;
            bool ok = false;

            if (connection.IsConnected)
            {
                response = Protocol.Outputs(connection);
                ok = response != null && response.IsOK;
            }

            if (ok)
            {
                IList<Output> outputs = Parse(response);

                if (outputs == null)
                {
                    Items = new ObservableCollection<Output>();
                }
                else
                {
                    bool listIsStillValid = outputs.Count == Items.Count;

                    for (int i = 0; listIsStillValid && i < Items.Count; ++i)
                    {
                        listIsStillValid = outputs[i].Name == Items[i].Name;
                    }

                    if (listIsStillValid)
                    {
                        for (int i = 0; i < Items.Count; ++i)
                        {
                            Items[i].IsEnabled = outputs[i].IsEnabled;
                        }
                    }
                    else
                    {
                        Items = new ObservableCollection<Output>(outputs);
                    }
                }
            }
            else
            {
                Items = new ObservableCollection<Output>();
            }
        }

        private IList<Output> Parse(ServerResponse response)
        {
            if (!response.IsOK)
            {
                return null;
            }

            IList<Output> result = new List<Output>();

            int index = -1;
            string name = "";
            bool enabled = false;

            foreach (ServerResponseLine line in response.ResponseLines)
            {
                if (line.Name == "outputid")
                {
                    int? parsed = Utils.StringToInt(line.Value);

                    if (parsed == null || parsed.Value != result.Count)
                    {
                        return null;
                    }
                    else
                    {
                        index = parsed.Value;
                    }
                }
                else if (line.Name == "outputname")
                {
                    name = line.Value;
                }
                else if (line.Name == "outputenabled")
                {
                    enabled = line.Value == "1";
                    result.Add(new Output(index, name, enabled));
                }
                else
                {
                    return null;
                }
            }

            return result;
        }
    }
}
