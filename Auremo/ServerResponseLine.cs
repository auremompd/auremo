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
    public class ServerResponseLine
    {
        private string m_RawLine = "";
        int m_NameValueBorder = -1;

        public ServerResponseLine(string line)
        {
            m_RawLine = line;
            m_NameValueBorder = m_RawLine.IndexOf(':');
        }

        public string Full
        {
            get
            {
                return m_RawLine;
            }
        }

        public string Name
        {
            get
            {
                if (m_NameValueBorder >= 0)
                {
                    return m_RawLine.Substring(0, m_NameValueBorder);
                }
                else
                {
                    return null;
                }
            }
        }

        public string Value
        {
            get
            {
                if (m_NameValueBorder >= 0 && m_RawLine.Length > m_NameValueBorder + 2)
                {
                    return m_RawLine.Substring(m_NameValueBorder + 2);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
