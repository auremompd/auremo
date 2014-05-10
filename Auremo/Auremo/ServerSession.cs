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
using System.Threading;
using Auremo.Properties;

namespace Auremo
{
    public class ServerSession : INotifyPropertyChanged
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

        private DataModel m_DataModel = null;
        private ServerSessionThread m_SessionThread = null;
        private SessionState m_State = SessionState.Disconnected;
        private string m_StateDescription = "";
        private string m_ProtocolError = "";

        private delegate void ThreadMessage(string message);
        private delegate void ThreadState(SessionState state);
        
        public enum SessionState
        {
            Disconnected,            // Not connected and idle
            Connecting,              // (Re-)establishing connection
            Connected,               // Connection OK
            Disconnecting            // Disconnecting and not reconnecting when done
        }

        public ServerSession(DataModel dataModel)
        {
            m_DataModel = dataModel;
        }

        public bool Connect(string host, int port)
        {
            if (m_SessionThread != null)
            {
                return false;
            }

            m_SessionThread = new ServerSessionThread(this, m_DataModel, host, port, 1000 * Settings.Default.NetworkTimeout, Settings.Default.ReconnectInterval);
            m_SessionThread.Start();
            return true;
        }

        public void Disconnect()
        {
            if (m_SessionThread != null)
            {
                m_SessionThread.Terminating = true;
            }
        }

        public void DoCleanup()
        {
            if (m_State == SessionState.Disconnecting && m_SessionThread.Join())
            {
                m_SessionThread = null;
                m_State = SessionState.Disconnected;
            }
        }
         
        public SessionState State
        {
            get
            {
                return m_State;
            }
            set
            {
                if (value != m_State)
                {
                    m_State = value;
                    NotifyPropertyChanged("State");
                    NotifyPropertyChanged("IsConnected");
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return State == SessionState.Connected;
            }
        }


        public string StateDescription
        {
            get
            {
                return m_StateDescription;
            }
            set
            {
                if (value != m_StateDescription)
                {
                    m_StateDescription = value;
                    NotifyPropertyChanged("StateDescription");
                }
            }
        }

        public string ProtocolError
        {
            get
            {
                return m_ProtocolError;
            }
            set
            {
                if (value != m_ProtocolError)
                {
                    m_ProtocolError = value;
                    NotifyPropertyChanged("ProtocolError");
                }
            }
        }

        public void OnThreadStateChanged(SessionState state)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadState((SessionState s) => { State = s; }), state);
        }

        public void OnThreadMessage(string message)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage((string m) => { StateDescription = m; }), message);
        }

        public void OnThreadError(string error)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage((string e) => { ProtocolError = e; }), error);
        }

        #region Protocol commands

        // The commands are in the order in which they appear in the
        // protocol spec.

        #region Admin commands

        public void Update()
        {
            Send(new MPDCommand("update"));
        }

        #endregion

        #region Informational commands

        public void Stats()
        {
            Send(new MPDCommand("stats"));
        }

        public void Status()
        {
            Send(new MPDCommand("status"));
        }

        #endregion

        #region Database commands

        public void ListAllInfo()
        {
            //Send(new MPDCommand("listallinfo"));
            // TODO: this is a kludge. Mopidy now seems to implement
            // listallinfo, but it is too slow to be usable. Therefore
            // we must pre-emptively fall back to search "any" ""
            // for now.
            Send(new MPDCommand());
        }

        #endregion

        #region Playlist commands

        public void Add(string path)
        {
            Send(new MPDCommand("add", path));
        }

        public void AddId(string path, int position)
        {
            Send(new MPDCommand("addid", path, position));
        }

        public void Clear()
        {
            Send(new MPDCommand("clear"));
        }

        public void CurrentSong()
        {
            Send(new MPDCommand("currentsong"));
        }

        public void DeleteId(int id)
        {
            Send(new MPDCommand("deleteid", id));
        }

        public void ListPlaylistInfo(string playlist)
        {
            Send(new MPDCommand("listplaylistinfo", playlist));
        }

        public void Load(string name)
        {
            Send(new MPDCommand("load", name));
        }

        public void LsInfo()
        {
            Send(new MPDCommand("lsinfo"));
        }

        public void Search(string type, string what)
        {
            Send(new MPDCommand("search", type, what));
        }

        public void MoveId(int id, int position)
        {
            Send(new MPDCommand("moveid", id, position));
        }

        public void PlaylistInfo()
        {
            Send(new MPDCommand("playlistinfo"));
        }

        public void Rename(string oldName, string newName)
        {
            Send(new MPDCommand("rename", oldName, newName));
        }

        public void Rm(string name)
        {
            Send(new MPDCommand("rm", name));
        }

        public void Save(string name)
        {
            Send(new MPDCommand("save", name));
        }

        public void Shuffle()
        {
            Send(new MPDCommand("shuffle"));
        }

        #endregion
        
        #region Playback commands

        public void Next()
        {
            Send(new MPDCommand("next"));
        }

        public void Pause()
        {
            Send(new MPDCommand("pause"));
        }
        
        public void Play()
        {
            Send(new MPDCommand("play"));
        }

        public void PlayId(int id)
        {
            Send(new MPDCommand("playid", id));
        }

        public void Previous()
        {
            Send(new MPDCommand("previous"));
        }

        public void Random(bool to)
        {
            Send(new MPDCommand("random", to));
        }

        public void Repeat(bool to)
        {
            Send(new MPDCommand("repeat", to));
        }

        public void Seek(int songIndex, int position)
        {
            Send(new MPDCommand("seek", songIndex, position));
        }

        public void SetVol(int vol)
        {
            Send(new MPDCommand("setvol", vol));
        }

        public void Stop()
        {
            Send(new MPDCommand("stop"));
        }

        #endregion

        #region Outputs

        public void Outputs()
        {
            Send(new MPDCommand("outputs"));
        }

        public void EnableOutput(int index)
        {
            Send(new MPDCommand("enableoutput", index));
        }

        public void DisableOutput(int index)
        {
            Send(new MPDCommand("disableoutput", index));
        }

        #endregion

        #region Miscellaneous commands

        public void Password(string password)
        {
            Send(new MPDCommand("password", password));
        }

        #endregion

        #region Helpers

        private void Send(MPDCommand command)
        {
            if (m_SessionThread != null || m_State == SessionState.Connected)
            {
                m_SessionThread.Send(command);
            }
        }

        #endregion

        #endregion
    }
}
