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
    public class ServerResponseLine
    {
        int m_NameValueBorder = -1;

        public ServerResponseLine(string line)
        {
            Full = line;
            m_NameValueBorder = Full.IndexOf(':');
        }

        public string Full
        {
            get;
            private set;
        }

        public string Name
        {
            get
            {
                if (m_NameValueBorder >= 0)
                {
                    return Full.Substring(0, m_NameValueBorder);
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
                if (m_NameValueBorder >= 0 && Full.Length > m_NameValueBorder + 2)
                {
                    return Full.Substring(m_NameValueBorder + 2);
                }
                else
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
 	         return Full;
        }
    }
}
