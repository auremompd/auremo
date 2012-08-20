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
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    /// <summary>
    /// Wraps a SongMetadata object so that it can be consumed by a TreeView[Item].
    /// </summary>
    public class SongMetadataTreeViewNode : ITreeViewNode, INotifyPropertyChanged, IComparable
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

        private string m_Filename = "";
        private bool m_IsSelected = false;
        private bool m_IsMultiSelected = false;

        public SongMetadataTreeViewNode(string filename, SongMetadata song, ITreeViewNode parent, TreeViewMultiSelection multiSelection)
        {
            m_Filename = filename;
            Song = song;
            Parent = parent;
            MultiSelection = multiSelection;
            HierarchyID = -1;
        }

        public string DisplayString
        {
            get
            {
                return m_Filename;
            }
        }

        public void AddChild(ITreeViewNode child)
        {
            throw new Exception("Attempt to add a child to a SongMetadataTreeViewModel.");
        }

        public ITreeViewNode Parent
        {
            get;
            private set;
        }

        public IList<ITreeViewNode> Children
        {
            get
            {
                return new List<ITreeViewNode>(); // Can't have child nodes.
            }
        }

        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (value != m_IsSelected)
                {
                    m_IsSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        public bool IsExpanded
        {
            get
            {
                return false; // A song is a leaf node in any tree.
            }
            set
            {
            }
        }

        public bool IsMultiSelected
        {
            get
            {
                return m_IsMultiSelected;
            }
            set
            {
                if (value != m_IsMultiSelected)
                {
                    if (value)
                    {
                        MultiSelection.Add(this);
                    }
                    else
                    {
                        MultiSelection.Remove(this);
                    }

                    m_IsMultiSelected = value;
                    NotifyPropertyChanged("IsMultiSelected");
                }
            }
        }

        public TreeViewMultiSelection MultiSelection
        {
            get;
            private set;
        }

        public int HierarchyID
        {
            get;
            set;
        }

        public void OnAncestorCollapsed()
        {
            IsMultiSelected = false;
        }

        public SongMetadata Song
        {
            get;
            private set;
        }

        public int CompareTo(object o)
        {
            if (o is ITreeViewNode)
            {
                return HierarchyID - ((ITreeViewNode)o).HierarchyID;
            }
            else
            {
                throw new Exception("SongMetadataTreeViewModel: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return Parent.ToString() + "/" + m_Filename;
        }
    }
}