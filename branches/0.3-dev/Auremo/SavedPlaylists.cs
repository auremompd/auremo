using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class SavedPlaylists : INotifyPropertyChanged
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

        private string m_CurrentPlaylistName = "";

        public SavedPlaylists()
        {
            Playlists = new ObservableCollection<string>();
        }

        public void Refresh(ServerConnection connection)
        {
            Playlists.Clear();

            if (connection.Status == ServerConnection.State.Connected)
            {
                ServerResponse lsInfoResponse = Protocol.LsInfo(connection);

                if (lsInfoResponse.IsOK)
                {
                    foreach (ServerResponseLine line in lsInfoResponse.ResponseLines)
                    {
                        if (line.Name == "playlist")
                        {
                            Playlists.Add(line.Value);
                        }
                    }
                }
            }
        }

        public IList<string> Playlists
        {
            get;
            private set;
        }

        public string CurrentPlaylistName
        {
            get
            {
                return m_CurrentPlaylistName;
            }
            set
            {
                if (value != m_CurrentPlaylistName)
                {
                    m_CurrentPlaylistName = value;
                    NotifyPropertyChanged("CurrentPlaylistName");
                    NotifyPropertyChanged("CurrentPlaylistNameEmpty");
                    NotifyPropertyChanged("CurrentPlaylistNameNonempty");
                }
            }
        }

        public bool CurrentPlaylistNameEmpty
        {
            get
            {
                return CurrentPlaylistName.Trim().Length == 0;
            }
        }

        public bool CurrentPlaylistNameNonempty
        {
            get
            {
                return !CurrentPlaylistNameEmpty;
            }
        }
    }
}
