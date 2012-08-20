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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    /// <summary>
    /// Wraps a directory (aka folder) name so that it can be consumed by a
    /// TreeView[Item].
    /// </summary>
    public class DirectoryTreeViewNode : ITreeViewNode, INotifyPropertyChanged, IComparable
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

        private string m_DirectoryName = "";
        private IList<ITreeViewNode> m_Children = new ObservableCollection<ITreeViewNode>();
        private bool m_IsSelected = false;
        private bool m_IsExpanded = false;
        private bool m_IsMultiSelected = false;

        public DirectoryTreeViewNode(string name, ITreeViewNode parent, TreeViewMultiSelection multiSelection)
        {
            m_DirectoryName = name;
            Parent = parent;
            MultiSelection = multiSelection;
            ID = -1;
        }

        public string DisplayString
        {
            get
            {
                return m_DirectoryName;
            }
        }

        public void AddChild(ITreeViewNode child)
        {
            m_Children.Add(child);
            NotifyPropertyChanged("Children");
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
                return m_Children;
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
                return m_IsExpanded;
            }
            set
            {
                if (value != m_IsExpanded)
                {
                    m_IsExpanded = value;

                    if (m_IsExpanded)
                    {
                        if (Parent != null)
                        {
                            Parent.IsExpanded = true;
                        }
                    }
                    else
                    {
                        foreach (ITreeViewNode child in Children)
                        {
                            OnAncestorCollapsed();
                        }
                    }

                    NotifyPropertyChanged("IsExpanded");
                }
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

        public int ID
        {
            get;
            set;
        }

        public void OnAncestorCollapsed()
        {
            IsMultiSelected = false;

            foreach (ITreeViewNode child in Children)
            {
                child.OnAncestorCollapsed();
            }
        }
            
        public int CompareTo(object o)
        {
            if (o is ITreeViewNode)
            {
                return ID - ((ITreeViewNode)o).ID;
            }
            else
            {
                throw new Exception("DirectoryTreeViewNode: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            if (Parent == null)
            {
                return "";
            }
            else
            {
                return Parent.ToString() + "/" + m_DirectoryName;
            }
        }
    }
}
