/*
 * Copyright 2012 Mikko Teräs
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
    public class ServerResponse
    {
        private IList<ServerResponseLine> m_ResponseLines = null;
        private ServerResponseLine m_Status = null;

        public ServerResponse()
        {
        }

        public ServerResponse(IList<string> lines)
        {
            // TODO: destructive, could be cleaner.
            if (lines.Count > 0)
            {
                m_Status = new ServerResponseLine(lines.Last());
                lines.RemoveAt(lines.Count - 1);
                m_ResponseLines = new List<ServerResponseLine>();

                foreach (string line in lines)
                {
                    m_ResponseLines.Add(new ServerResponseLine(line));
                }
            }
        }

        public IList<ServerResponseLine> Lines
        {
            get
            {
                return m_ResponseLines;
            }
        }

        public bool IsOK
        {
            get
            {
                return m_Status.Full.StartsWith("OK");
            }
        }

        public bool IsACK
        {
            get
            {
                return m_Status.Full.StartsWith("ACK");
            }
        }

        public ServerResponseLine Status
        {
            get
            {
                return m_Status;
            }
        }
    }
}
