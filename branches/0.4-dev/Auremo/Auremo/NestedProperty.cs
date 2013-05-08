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

namespace Auremo
{
    // The purpose of this seemingly trivial class is to allow reuse of a WPF
    // control style that contains DataTriggers. Normally the DataTrigger
    // needs to know the name of the property it binds to; if several controls
    // need to share the same style but bind to different properties, naming
    // becomes a problem. It can the circumvented by giving the data source
    // multiple NestedProperty members and assigning these as the controls'
    // DataContexts, instead of using the data source as the DataContext
    // directly. The NestedProperty will contain the value of interest and it
    // is always called "Value".
    //
    // Case in point: the play, stop and pause buttons have to bind to the
    // server state's IsPlaying, IsStopped and IsPaused properties, but the
    // style specification cannot know this. See these properties and controls
    // for an easy example.

    public class NestedProperty<T> : INotifyPropertyChanged where T : IComparable<T>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private T m_Value;

        public NestedProperty(T value)
        {
            m_Value = value;
        }

        public T Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (value.CompareTo(m_Value) != 0)
                {
                    m_Value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        public static implicit operator T(NestedProperty<T> rhs)
        {
            return rhs.Value;
        }
    }
}
