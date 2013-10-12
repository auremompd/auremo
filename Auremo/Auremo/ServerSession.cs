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
        private string m_Host = "";
        private int m_Port = 6600;
        private ServerSessionThread m_SessionThread = null;
        private Thread m_Thread = null;
        private bool m_ConnectionDesired = false;
        private SessionState m_State = SessionState.Disconnected;
        private string m_StateDescription = "";
        private string m_ProtocolError = "";

        private delegate void ThreadEvent();
        private delegate void ThreadMessage(string message);
        
        public enum SessionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        public ServerSession(DataModel dataModel)
        {
            m_DataModel = dataModel;
        }

        public void Connect(string host, int port)
        {
            m_ConnectionDesired = true;

            if (m_SessionThread != null)
            {
                m_SessionThread.Terminating = true;
                m_Thread.Join();
            }

            m_SessionThread = new ServerSessionThread(this, m_DataModel, host, port, Settings.Default.NetworkTimeout, Settings.Default.ReconnectInterval);
            m_Thread = new Thread(new ThreadStart(m_SessionThread.Start));
            m_Thread.Start();
        }

        public void Disconnect()
        {
            m_ConnectionDesired = false;

            if (m_SessionThread != null)
            {
                m_SessionThread.Terminating = true;
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
                }
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

        public void OnThreadConnected()
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadEvent(OnConnected), null);
        }

        public void OnThreadDisconnected()
        {
            m_DataModel.MainWindow.Dispatcher.BeginInvoke(new ThreadEvent(OnDisconnected), null);
        }

        public void OnThreadMessage(string message)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage(OnMessage), new object[] { message });
        }

        public void OnThreadError(string message)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(new ThreadMessage(OnError), new object[] { message });
        }

        private void OnConnected()
        {
            State = SessionState.Connected;
        }

        private void OnDisconnected()
        {
            State = SessionState.Disconnected;
            StateDescription = "Disconnected from " + m_Host + ":" + m_Port + ".";

            m_Thread.Join();
            m_Thread = null;
            m_SessionThread = null;

            if (m_ConnectionDesired)
            {
                Connect(m_Host, m_Port);
            }
        }

        private void OnMessage(string message)
        {
            StateDescription = message;
        }

        private void OnError(string error)
        {
            ProtocolError = error;
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
            Send(new MPDCommand("listallinfo"));
        }

        // TODO: this is a workaround for Mopidy's missing listallinfo
        // commands. Remove it if/when Mopidy adds support.
        public void ListAllInfoMopidyWordaround()
        {
            Send(new MPDCommand("search", "any", ""));
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

        public void ListPlaylist(string playlist)
        {
            Send(new MPDCommand("listplaylist", playlist));
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
            if (m_SessionThread != null)
            {
                m_SessionThread.Send(command);
            }
        }

        #endregion

        #endregion
    }
}
