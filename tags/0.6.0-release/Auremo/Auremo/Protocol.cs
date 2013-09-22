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
using System.Linq;
using System.Text;

namespace Auremo
{
    public class Protocol
    {
        #region Admin commands (reference order)

        public static ServerResponse Update(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "update");
        }

        #endregion

        #region Informational commands (reference order)

        public static ServerResponse Stats(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "stats");
        }

        public static ServerResponse Status(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "status");
        }

        #endregion

        #region Database commands (reference order)

        public static ServerResponse ListAllInfo(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "listallinfo");
        }

        #endregion

        #region Playlist commands (reference order)

        public static ServerResponse Add(ServerConnection connection, string path)
        {
            return SendStringAndGetResponse(connection, "add " + QuoteString(path));
        }

        public static ServerResponse AddId(ServerConnection connection, string path, int position)
        {
            return SendStringAndGetResponse(connection, "addid " + QuoteString(path) + " " + position);
        }

        public static ServerResponse Clear(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "clear");
        }

        public static ServerResponse CurrentSong(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "currentsong");
        }

        public static ServerResponse DeleteId(ServerConnection connection, int id)
        {
            return SendStringAndGetResponse(connection, "deleteid " + id);
        }

        public static ServerResponse Load(ServerConnection connection, string name)
        {
            return SendStringAndGetResponse(connection, "load " + QuoteString(name));
        }

        public static ServerResponse LsInfo(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "lsinfo");
        }

        public static ServerResponse MoveId(ServerConnection connection, int id, int position)
        {
            return SendStringAndGetResponse(connection, "moveid " + id + " " + position);
        }

        public static ServerResponse PlaylistInfo(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "playlistinfo");
        }

        public static ServerResponse Rename(ServerConnection connection, string oldName, string newName)
        {
            return SendStringAndGetResponse(connection, "rename " + QuoteString(oldName) + " " + QuoteString(newName));
        }

        public static ServerResponse Rm(ServerConnection connection, string name)
        {
            return SendStringAndGetResponse(connection, "rm " + QuoteString(name));
        }

        public static ServerResponse Save(ServerConnection connection, string name)
        {
            return SendStringAndGetResponse(connection, "save " + QuoteString(name));
        }

        public static ServerResponse Shuffle(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "shuffle");
        }

        public static ServerResponse ListPlaylist(ServerConnection connection, string playlist)
        {
            return SendStringAndGetResponse(connection, "listplaylist " + QuoteString(playlist));
        }

        #endregion

        #region Playback commands (reference order)

        public static ServerResponse Next(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "next");
        }

        public static ServerResponse Pause(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "pause");
        }

        public static ServerResponse Play(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "play");
        }

        public static ServerResponse PlayId(ServerConnection connection, int id)
        {
            return SendStringAndGetResponse(connection, "playid " + id);
        }

        public static ServerResponse Previous(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "previous");
        }

        public static ServerResponse Random(ServerConnection connection, bool to)
        {
            return SendStringAndGetResponse(connection, "random " + (to ? "1" : "0"));
        }

        public static ServerResponse Repeat(ServerConnection connection, bool to)
        {
            return SendStringAndGetResponse(connection, "repeat " + (to ? "1" : "0"));
        }

        public static ServerResponse Seek(ServerConnection connection, int songIndex, int position)
        {
            return SendStringAndGetResponse(connection, "seek " + songIndex + " " + position);
        }

        public static ServerResponse SetVol(ServerConnection connection, int volume)
        {
            return SendStringAndGetResponse(connection, "setvol " + volume);
        }

        public static ServerResponse Stop(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "stop");
        }

        #endregion

        #region Outputs

        public static ServerResponse Outputs(ServerConnection connection)
        {
            return SendStringAndGetResponse(connection, "outputs");
        }

        public static ServerResponse EnableOutput(ServerConnection connection, int index)
        {
            return SendStringAndGetResponse(connection, "enableoutput " + index);
        }

        public static ServerResponse DisableOutput(ServerConnection connection, int index)
        {
            return SendStringAndGetResponse(connection, "disableoutput " + index);
        }

        #endregion

        #region Miscellaneous commands (reference order)

        public static void Close(ServerConnection connection)
        {
            SendString(connection, "close");
        }

        public static ServerResponse Password(ServerConnection connection, string password)
        {
            return SendStringAndGetResponse(connection, "password " + QuoteString(password));
        }

        #endregion

        #region Internal helpers

        private static string QuoteString(string s)
        {
            string intermediate = s.Replace("\\", "\\\\");
            string result = intermediate.Replace("\"", "\\\"");
            return "\"" + result + "\"";
        }

        private static void SendString(ServerConnection connection, string s)
        {
            if (connection != null)
            {
                connection.SendCommand(s);
            }
        }

        private static ServerResponse SendStringAndGetResponse(ServerConnection connection, string s)
        {
            connection.SendCommand(s);
            return connection.ReceiveResponse();
        }

        #endregion
    }
}
