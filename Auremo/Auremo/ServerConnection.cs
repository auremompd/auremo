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
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Auremo
{
    public class ServerConnection : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        public enum State
        {
            Disconnected,
            Connecting,
            Connected
        };

        private string m_Host = null;
        private int m_Port = 0;
        private DateTime m_DisconnectTime = DateTime.MinValue;
        private TcpClient m_Connection = null;
        private IAsyncResult m_ConnectionAsyncResult = null;
        private NetworkStream m_Stream = null;
        private byte[] m_ReceiveBuffer = new byte[1024];
        private int m_BytesInReceiveBuffer = 0;
        private int m_ReceiveBufferIndex = 0;
        private State m_Status = State.Disconnected;

        public ServerConnection()
        {
        }

        public void SetHost(string host, int port)
        {
            if (m_Connection != null)
            {
                Disconnect();
                StatusDescription = "Disconnected from " + m_Host + ":" + m_Port + ".";
            }

            m_Host = host;
            m_Port = port;
        }

        public void StartConnecting()
        {
            Disconnect();
            Status = State.Connecting;
            m_Connection = new TcpClient();
            m_ConnectionAsyncResult = m_Connection.BeginConnect(m_Host, m_Port, null, null);
            StatusDescription = "Connecting to " + m_Host + ":" + m_Port + ".";
        }

        public bool IsReadyToConnect
        {
            get
            {
                return m_ConnectionAsyncResult != null && m_ConnectionAsyncResult.IsCompleted;
            }
        }

        public ServerResponse FinishConnecting()
        {
            try
            {
                m_Connection.EndConnect(m_ConnectionAsyncResult);
            }
            catch
            {
            }

            m_ConnectionAsyncResult = null;

            if (m_Connection.Connected)
            {
                m_Stream = m_Connection.GetStream();
                StatusDescription = "Connected to " + m_Host + ":" + m_Port + ".";
                Status = State.Connected;
                return ReceiveResponse();
            }
            else
            {
                Disconnect();
                StatusDescription = "Connecting to " + m_Host + ":" + m_Port + " failed.";
                return null;
            }
        }

        public void Disconnect()
        {
            if (m_Stream != null)
            {
                m_Stream.Close();
                m_Stream = null;
                StatusDescription = "Disconnected from " + m_Host + ":" + m_Port + ".";
            }

            if (m_Connection != null)
            {
                m_Connection.Close();
                m_Connection = null;
                m_DisconnectTime = DateTime.Now;
            }

            m_BytesInReceiveBuffer = 0;
            m_ReceiveBufferIndex = 0;
            m_ConnectionAsyncResult = null;
            BytesSent = 0;
            BytesReceived = 0;
            Status = State.Disconnected;
        }
        
        public State Status
        {
            get
            {
                return m_Status;
            }
            private set
            {
                if (value != m_Status)
                {
                    m_Status = value;
                    NotifyPropertyChanged("Status");
                    NotifyPropertyChanged("IsConnected");
                }
            }
            /*{
                if (m_Connection == null)
                {
                    return State.Disconnected;
                }
                else if (m_ConnectionAsyncResult == null)
                {
                    return State.Connected;
                }
                else
                {
                    return State.Connecting;
                }
            }*/
        }

        public bool IsConnected
        {
            get
            {
                return Status == State.Connected;
            }
        }

        public TimeSpan TimeSinceDisconnect
        {
            get
            {
                return DateTime.Now.Subtract(m_DisconnectTime);
            }
        }

        private string m_StatusDescription = "";

        public string StatusDescription
        {
            get
            {
                return m_StatusDescription;
            }
            set
            {
                m_StatusDescription = value;
                NotifyPropertyChanged("StatusDescription");
            }
        }


        public void SendCommand(string command)
        {
            if (Status == State.Connected)
            {
                try
                {
                    byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(command + "\n");
                    m_Stream.Write(messageBytes, 0, messageBytes.Length);
                    BytesSent += messageBytes.Length;
                }
                catch (Exception)
                {
                    Disconnect();
                    StatusDescription = "Connection to " + m_Host + ":" + m_Port + " lost.";
                }
            }
        }

        public ServerResponse ReceiveResponse()
        {
            if (Status == State.Connected)
            {
                try
                {
                    List<string> lines = new List<string>();
                    bool keepGoing = true;

                    do
                    {
                        string line = ReadSingleLineResponse();
                        keepGoing = !line.StartsWith("OK") && !line.StartsWith("ACK");
                        lines.Add(line);
                    } while (keepGoing);

                    return new ServerResponse(lines);
                }
                catch (Exception)
                {
                    Disconnect();
                    StatusDescription = "Connection to " + m_Host + ":" + m_Port + " lost.";
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public Int64 BytesSent
        {
            get;
            private set;
        }

        public Int64 BytesReceived
        {
            get;
            private set;
        }

        // Return a single line (of a possibly multiline) response with the LF stripped.
        private string ReadSingleLineResponse()
        {
            string response = "";
            bool newlinePassed = false;
            int initialOffset = m_ReceiveBufferIndex;

            while (!newlinePassed)
            {
                if (m_ReceiveBufferIndex >= m_BytesInReceiveBuffer)
                {
                    response += System.Text.Encoding.UTF8.GetString(m_ReceiveBuffer, initialOffset, m_BytesInReceiveBuffer - initialOffset);
                    m_BytesInReceiveBuffer = m_Stream.Read(m_ReceiveBuffer, 0, m_ReceiveBuffer.Length);
                    BytesReceived += m_BytesInReceiveBuffer;
                    m_ReceiveBufferIndex = 0;
                    initialOffset = 0;
                }
                else
                {
                    if (m_ReceiveBuffer[m_ReceiveBufferIndex] == '\n')
                    {
                        response += System.Text.Encoding.UTF8.GetString(m_ReceiveBuffer, initialOffset, m_ReceiveBufferIndex - initialOffset);
                        newlinePassed = true;
                    }

                    ++m_ReceiveBufferIndex;
                }
            }

            return response;
        }
    }
}
