using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class SavedPlaylists
    {
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
    }
}
