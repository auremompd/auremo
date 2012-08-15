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
    public class DirectoryTreeViewModel : ITreeViewModel, INotifyPropertyChanged, IComparable
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
        private ITreeViewModel m_Parent = null;
        private IList<ITreeViewModel> m_Children = new ObservableCollection<ITreeViewModel>();
        private bool m_IsSelected = false;
        private bool m_IsExpanded = false;
        private bool m_IsMultiSelected = false;
        private TreeViewMultiSelection m_MultiSelection = null;

        public DirectoryTreeViewModel(string name, ITreeViewModel parent, TreeViewMultiSelection multiSelection)
        {
            m_DirectoryName = name;
            m_Parent = parent;
            m_MultiSelection = multiSelection;
            HierarchyID = -1;
        }

        public string DisplayString
        {
            get
            {
                return m_DirectoryName;
            }
        }

        public void AddChild(ITreeViewModel child)
        {
            m_Children.Add(child);
            NotifyPropertyChanged("Children");
        }

        public ITreeViewModel Parent
        {
            get
            {
                return m_Parent;
            }
        }
        
        public IList<ITreeViewModel> Children
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
                        if (m_Parent != null)
                        {
                            m_Parent.IsExpanded = true;
                        }
                    }
                    else
                    {
                        foreach (ITreeViewModel child in Children)
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
                        m_MultiSelection.Add(this);
                    }
                    else
                    {
                        m_MultiSelection.Remove(this);
                    }

                    m_IsMultiSelected = value;
                    NotifyPropertyChanged("IsMultiSelected");
                }
            }
        }

        public TreeViewMultiSelection MultiSelection
        {
            get
            {
                return m_MultiSelection;
            }
        }

        public int HierarchyID
        {
            get;
            set;
        }

        public void OnAncestorCollapsed()
        {
            IsMultiSelected = false;

            foreach (ITreeViewModel child in Children)
            {
                child.OnAncestorCollapsed();
            }
        }
            
        public int CompareTo(object o)
        {
            if (o is ITreeViewModel)
            {
                return HierarchyID - ((ITreeViewModel)o).HierarchyID;
            }
            else
            {
                throw new Exception("DirectoryTreeViewModel: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            if (m_Parent == null)
            {
                return "";
            }
            else
            {
                return m_Parent.ToString() + "/" + m_DirectoryName;
            }
        }
    }
}
