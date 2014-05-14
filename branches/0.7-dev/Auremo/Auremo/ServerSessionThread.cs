﻿/*
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

using Auremo.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Auremo
{
    public class ServerSessionThread
    {
        public delegate void GenericResponseReceivedCallback(IEnumerable<MPDResponseLine> response);
        public delegate void GenericSingleArgumentResponseReceivedCallback(IEnumerable<MPDResponseLine> response, string argument);
        public delegate void SongListResponseReceivedCallback(IEnumerable<MPDSongResponseBlock> response);
        public delegate void NamedSongListResponseReceivedCallback(string name, IEnumerable<MPDSongResponseBlock> response);
        private ServerSession m_Parent = null;
        private DataModel m_DataModel = null;
        private Thread m_Thread = null;
        private object m_Lock = new object();
        private bool m_Terminating = false;
        private ManualResetEvent m_ThreadEvent = new ManualResetEvent(false);
        private Queue<MPDCommand> m_CommandQueue = new Queue<MPDCommand>();
        private bool m_StatusUpdateEnqueued = false;
        private bool m_StatsUpdateEnqueued = false;
        private int m_ReconnectInterval = 0;
        private int m_Timeout = 0;
        private TcpClient m_Connection = null;
        private NetworkStream m_Stream = null;
        private const int m_ReceiveBufferSize = 16384;
        private byte[] m_ReceiveBuffer = null;
        private int m_ReceiveBufferPosition = 0;
        private int m_BytesInBuffer = 0;
        private Queue<MPDResponseLine> m_ResponseLines = new Queue<MPDResponseLine>();
        private IList<MPDResponseLine> m_CurrentResponse = new List<MPDResponseLine>();
        private IList<MPDSongResponseBlock> m_CurrentSongList = new List<MPDSongResponseBlock>();
        private string m_CharsLeftFromLastBuffer = "";
        private UTF8Encoding m_UTF8 = new UTF8Encoding();

        public ServerSessionThread(ServerSession parent, DataModel dataModel, string host, int port, int timeout, int reconnectInterval)
        {
            m_Parent = parent;
            m_DataModel = dataModel;
            m_ReceiveBuffer = new byte[m_ReceiveBufferSize];
            Host = host;
            Port = port;
            m_Timeout = timeout;
            m_ReconnectInterval = reconnectInterval;

            m_Thread = new Thread(Run);
            m_Thread.Name = "MPD connection thread";
        }

        public void Start()
        {
            m_Thread.Start();
        }

        public bool Join()
        {
            return m_Thread.Join(0);
        }

        private void Run()
        {
            try
            {
                while (!Terminating)
                {
                    SetInitialState();
                    Connect();
                    ExecuteCommands();
                    Close();
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Uncaught exception in Auremo.ServerSessionThread.\n" +
                                               "Please take a screenshot of this message and send it to the developer.\n\n" +
                                               e.ToString(),
                                               "Auremo has crashed!");
                throw e;
            }
        }

        private string Host
        {
            get;
            set;
        }

        private int Port
        {
            get;
            set;
        }

        public bool Terminating
        {
            get
            {
                lock (m_Lock)
                {
                    return m_Terminating;
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_Terminating = value;

                    if (m_Terminating)
                    {
                        m_ThreadEvent.Set();
                    }
                }
            }
        }

        public void Send(MPDCommand command)
        {
            lock (m_Lock)
            {
                m_CommandQueue.Enqueue(command);
                m_ThreadEvent.Set();
            }
        }

        private void SetInitialState()
        {
            m_CommandQueue.Clear();
            m_StatusUpdateEnqueued = false;
            m_StatsUpdateEnqueued = false;
            m_ResponseLines.Clear();
            m_CurrentResponse.Clear();
            m_CurrentSongList.Clear();
            m_CharsLeftFromLastBuffer = "";
        }

        private void Connect()
        {
            while (!Terminating && m_Connection == null)
            {
                m_Connection = new TcpClient();
                m_Connection.SendTimeout = m_Timeout;
                m_Connection.ReceiveTimeout = m_Timeout;
                bool fatal = false;

                try
                {
                    m_Parent.OnConnectionStateChanged(ServerSession.SessionState.Connecting);
                    m_Parent.OnActivityChanged("");
                    IAsyncResult connectResult = m_Connection.BeginConnect(Host, Port, null, null);

                    while (!connectResult.IsCompleted && !Terminating)
                    {
                        Thread.Sleep(100);
                    }

                    if (Terminating)
                    {
                        m_Connection = null;
                    }
                    else
                    {
                        m_Connection.EndConnect(connectResult);
                        m_Stream = m_Connection.GetStream();
                    }
                }
                catch (Exception e)
                {
                    fatal = !(e is SocketException);
                    m_Stream = null;
                    m_Connection = null;
                }

                if (m_Connection != null)
                {
                    if (ParseBanner())
                    {
                        // Send possible password before any other commands.
                        SendPassword();
                        m_Parent.OnConnectionStateChanged(ServerSession.SessionState.Connected);
                        m_Parent.OnActivityChanged("");
                    }
                    else
                    {
                        m_Stream = null;
                        m_Connection = null;
                    }
                }

                if (m_Connection == null)
                {
                    m_Parent.OnActivityChanged("Connecting to " + Host + ":" + Port + " failed.");

                    if (fatal)
                    {
                        Terminating = true;
                    }
                    else
                    {
                        DateTime coolOffStart = DateTime.Now;

                        do
                        {
                            Thread.Sleep(100);
                        } while (!Terminating && DateTime.Now.Subtract(coolOffStart).TotalSeconds < m_ReconnectInterval);
                    }
                }
            }
        }

        private void ExecuteCommands()
        {
            bool terminating = Terminating;

            while (!terminating)
            {
                MPDCommand command = null;

                lock (m_Lock)
                {
                    // Avoid using the property as it might break
                    // threading. TODO: this could be neatly wrapped into
                    // a method that returns a command or null if
                    // terminating, but it's just cosmetics.
                    terminating = m_Terminating;

                    if (m_CommandQueue.Count > 0)
                    {
                        command = m_CommandQueue.Dequeue();
                        m_ThreadEvent.Reset();
                    }
                }

                if (!terminating)
                {
                    if (command == null)
                    {
                        m_ThreadEvent.WaitOne();
                    }
                    else
                    {
                        UpdateThreadMessage(command);

                        bool optimizeOut = false;
                        optimizeOut = optimizeOut || (command.Op == "status" && m_StatusUpdateEnqueued);
                        optimizeOut = optimizeOut || (command.Op == "stats" && m_StatsUpdateEnqueued);

                        if (!optimizeOut)
                        {
                            SendCommand(command.FullSyntax);
                            ReceiveResponse(command);
                        }
                    }
                }
            }
        }

        private void Close()
        {
            if (Terminating)
            {
                m_Parent.OnConnectionStateChanged(ServerSession.SessionState.Disconnecting);
            }
            else
            {
                m_Parent.OnConnectionStateChanged(ServerSession.SessionState.Connecting);
            }

            m_Parent.OnActivityChanged("Disconnected from " + Host + ":" + Port + ".");

            if (m_Connection != null)
            {
                if (m_Connection.Connected)
                {
                    SendCommand("close");
                }

                m_Stream.Close();
                m_Stream = null;
                m_Connection.Close();
                m_Connection = null;
            }
        }

        private bool ParseBanner()
        {
            MPDResponseLine banner = GetResponseLine();

            if (banner == null)
            {
                m_Parent.OnErrorMessageChanged("The server banner is not valid; check settings.");
                return false;
            }
            else if (banner.Key == MPDResponseLine.Keyword.OK && banner.Value.StartsWith("MPD"))
            {
                return true;
            }
            else
            {
                m_Parent.OnErrorMessageChanged("Error from server: " + banner.Literal);
                return false;
            }
        }

        private bool SendCommand(string command)
        {
            try
            {
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(command + "\n");
                m_Stream.Write(messageBytes, 0, messageBytes.Length);
                return true;
            }
            catch (Exception)
            {
                m_Connection.Close();
                Terminating = true;
            }

            return false;
        }

        private void ReceiveResponse(MPDCommand command)
        {
            MPDResponseLine statusLine = GetResponseLine();

            while (statusLine != null && !statusLine.IsStatus)
            {
                if (statusLine.Key != MPDResponseLine.Keyword.Unknown)
                {
                    m_CurrentResponse.Add(statusLine);
                }

                statusLine = GetResponseLine();
            }

            if (statusLine != null)
            {
                if (statusLine.Key == MPDResponseLine.Keyword.ACK)
                {
                    if (command.Op == "listallinfo" && statusLine.Value.Contains("Not implemented"))
                    {
                        // TODO: this is a workaround for Mopidy not implementing listallinfo.
                        // It can hopefully removed later.
                        Send(new MPDCommand());
                    }
                    else
                    {
                        m_Parent.OnErrorMessageChanged(statusLine.Value);
                    }
                }
                else
                {
                    if (command.Op == "currentsong")
                    {
                        ParseSongList();
                        Callback(m_DataModel.CurrentSong.OnCurrentSongResponseReceived);
                        m_CurrentSongList.Clear();
                    }
                    // TODO: remove the latter when Mopidy starts properly supporting the former.
                    else if (command.Op == "listallinfo" || command.Op == "mopidylistallinfokludge")
                    {
                        ParseSongList();
                        Callback(m_DataModel.Database.OnListAllInfoResponseReceived);
                        m_CurrentSongList.Clear();
                    }
                    else if (command.Op == "listplaylistinfo")
                    {
                        ParseSongList();
                        Callback(m_DataModel.SavedPlaylists.OnListPlaylistInfoResponseReceived, command.Argument1);
                        m_CurrentSongList.Clear();
                    }
                    else if (command.Op == "lsinfo")
                    {
                        Callback(m_DataModel.SavedPlaylists.OnLsInfoResponseReceived);
                    }
                    else if (command.Op == "outputs")
                    {
                        Callback(m_DataModel.OutputCollection.OnOutputsResponseReceived);
                    }
                    else if (command.Op == "playlistinfo")
                    {
                        ParseSongList();
                        Callback(m_DataModel.Playlist.OnPlaylistInfoResponseReceived);
                        m_CurrentSongList.Clear();
                    }
                    else if (command.Op == "search")
                    {
                        ParseSongList();
                        Callback(m_DataModel.AdvancedSearch.OnSearchResponseReceived);
                        m_CurrentSongList.Clear();
                    }
                    else if (command.Op == "stats")
                    {
                        Callback(m_DataModel.ServerStatus.OnStatsResponseReceived);
                    }
                    else if (command.Op == "status")
                    {
                        Callback(m_DataModel.ServerStatus.OnStatusResponseReceived);
                    }
                }
            }

            m_CurrentResponse.Clear();
        }

        private MPDResponseLine GetResponseLine()
        {
            while (m_ResponseLines.Count == 0 && !Terminating)
            {
                if (!ReadMoreLines())
                {
                    Terminating = true;
                    return null;
                }
            }

            return m_ResponseLines.Count > 0 ? m_ResponseLines.Dequeue() : null;
        }

        private bool ReadMoreLines()
        {
            try
            {
                m_BytesInBuffer += m_Stream.Read(m_ReceiveBuffer, m_ReceiveBufferPosition, m_ReceiveBuffer.Length - m_ReceiveBufferPosition);
                SplitBufferIntoLines();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SplitBufferIntoLines()
        {
            int pos = 0;
            int firstDanglingByte = 0;
            int lineStartPosition = 0;

            while (pos < m_BytesInBuffer)
            {
                firstDanglingByte = pos;
                byte firstByte = m_ReceiveBuffer[pos];

                if (firstByte == (byte)'\n')
                {
                    // Complete line available, chop it off.
                    MPDResponseLine line = new MPDResponseLine(m_CharsLeftFromLastBuffer + m_UTF8.GetString(m_ReceiveBuffer, lineStartPosition, pos - lineStartPosition));
                    m_ResponseLines.Enqueue(line);
                    m_CharsLeftFromLastBuffer = "";
                    pos += 1;
                    lineStartPosition = pos;
                }
                else if (firstByte < 0x80) // Single byte character.
                {
                    pos += 1;
                }
                else if (firstByte < 0xDF) // First in a 2-byte character.
                {
                    pos += 2;
                }
                else if (firstByte < 0xF0) // First in a 3-byte character.
                {
                    pos += 3;
                }
                else
                {
                    pos += 4;
                }
            }

            if (pos == m_BytesInBuffer)
            {
                // Only complete UTF-8 characters in the buffer -- good!
                if (pos > lineStartPosition)
                {
                    m_CharsLeftFromLastBuffer = m_UTF8.GetString(m_ReceiveBuffer, lineStartPosition, pos - lineStartPosition);
                }

                m_BytesInBuffer = 0;
                m_ReceiveBufferPosition = 0;
            }
            else
            {
                // There are dangling bytes -- we need to keep them in the
                // buffer so they can be completed by the next read.
                m_CharsLeftFromLastBuffer = m_UTF8.GetString(m_ReceiveBuffer, lineStartPosition, pos - firstDanglingByte);

                for (int i = firstDanglingByte; i < m_BytesInBuffer; ++i)
                {
                    m_ReceiveBuffer[i - firstDanglingByte] = m_ReceiveBuffer[i];
                }

                m_BytesInBuffer -= firstDanglingByte;
                m_ReceiveBufferPosition = m_BytesInBuffer;
            }
        }

        private void ParseSongList()
        {
            MPDSongResponseBlock song = new MPDSongResponseBlock(null);

            foreach (MPDResponseLine line in m_CurrentResponse)
            {
                if (line.Key == MPDResponseLine.Keyword.File)
                {
                    if (song.File != null)
                    {
                        m_CurrentSongList.Add(song);
                    }

                    song = new MPDSongResponseBlock(line.Value);
                }
                else if (line.Key == MPDResponseLine.Keyword.Album)
                {
                    song.Album = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Artist)
                {
                    song.Artist = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.AlbumArtist)
                {
                    song.AlbumArtist = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Date)
                {
                    song.Date = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Genre)
                {
                    song.Genre = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Id)
                {
                    song.Id = line.IntValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.Name)
                {
                    song.Name = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Pos)
                {
                    song.Pos = line.IntValue;
                }
                else if (line.Key == MPDResponseLine.Keyword.Time)
                {
                    int? time = line.IntValue;
                    song.Time = time > 0 ? time : null;
                }
                else if (line.Key == MPDResponseLine.Keyword.Title)
                {
                    song.Title = line.Value;
                }
                else if (line.Key == MPDResponseLine.Keyword.Track)
                {
                    song.Track = line.IntValue;
                }
            }

            if (song.File != null)
            {
                m_CurrentSongList.Add(song);
            }
        }

        private void SendPassword()
        {
            string password = Crypto.DecryptPassword(Settings.Default.Password);

            if (password.Length > 0)
            {
                Send(new MPDCommand("password", password));
            }
        }

        private void UpdateThreadMessage(MPDCommand command)
        {
            string message = "";

            if (command.Op == "listallinfo" || command.Op == "mopidylistallinfokludge")
            {
                message = "Querying music database.";
            }
            else if (command.Op == "listplaylistinfo")
            {
                message = "Querying playlist " + command.Argument1 + "...";
            }
            else if (command.Op == "search")
            {
                message = "Searching for " + command.Argument2 + "...";
            }

            m_Parent.OnActivityChanged(message);
        }

        private void Callback(Action callback)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(callback);
        }

        private void Callback(GenericResponseReceivedCallback callback)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(callback, new object[] { m_CurrentResponse });
        }

        public void Callback(GenericSingleArgumentResponseReceivedCallback callback, string argument)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(callback, new object[] { m_CurrentResponse, argument });
        }

        private void Callback(SongListResponseReceivedCallback callback)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(callback, new object[] { m_CurrentSongList });
        }

        private void Callback(NamedSongListResponseReceivedCallback callback, string name)
        {
            m_DataModel.MainWindow.Dispatcher.Invoke(callback, new object[] { name, m_CurrentSongList });
        }
    }
}
