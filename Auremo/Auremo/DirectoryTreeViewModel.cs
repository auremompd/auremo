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
    class DirectoryTreeViewModel : ITreeViewModel, INotifyPropertyChanged
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

        string m_DirectoryName = "";
        ITreeViewModel m_Parent = null;
        IList<ITreeViewModel> m_Children = new ObservableCollection<ITreeViewModel>();
        bool m_IsSelected = false;
        bool m_IsExpanded = false;

        public DirectoryTreeViewModel(string name, ITreeViewModel parent)
        {
            m_DirectoryName = name;
            m_Parent = parent;
        }

        public string DisplayString
        {
            get
            {
                return m_DirectoryName;
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

                    if (m_IsExpanded && m_Parent != null)
                    {
                        m_Parent.IsExpanded = true;
                    }

                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        public void AddChild(ITreeViewModel child)
        {
            m_Children.Add(child);
            NotifyPropertyChanged("Children");
        }

        public override string ToString()
        {
            if (m_Parent == null)
            {
                return m_DirectoryName;
            }
            else
            {
                return m_Parent.ToString() + "/" + m_DirectoryName;
            }
        }
    }
}
