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
using System.IO;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class ServerResponse
    {
        public ServerResponse()
        {
            Status = null;
            ResponseLines = null;
        }

        public ServerResponse(IList<string> lines)
        {
            // TODO: destructive, could be cleaner.
            if (lines.Count > 0)
            {
                Status = new ServerResponseLine(lines.Last());
                lines.RemoveAt(lines.Count - 1);
                ResponseLines = new List<ServerResponseLine>();

                foreach (string line in lines)
                {
                    ResponseLines.Add(new ServerResponseLine(line));
                }
            }
        }

        // For debugging only.
        public static ServerResponse FromFile(string filename)
        {
            IEnumerable<string> contents = File.ReadLines(filename, Encoding.UTF8);
            IList<string> lines = new List<string>();

            foreach (string line in contents)
            {
                lines.Add(line);
            }

            return new ServerResponse(lines);
        }

        public IList<ServerResponseLine> ResponseLines
        {
            get;
            private set;
        }

        public bool IsOK
        {
            get
            {
                return Status.Full.StartsWith("OK");
            }
        }

        public bool IsACK
        {
            get
            {
                return Status.Full.StartsWith("ACK");
            }
        }

        public ServerResponseLine Status
        {
            get;
            private set;
        }
    }
}
