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
    public class SongMetadataTreeViewModel : ITreeViewModel, INotifyPropertyChanged, IComparable
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
        private SongMetadata m_Song = null;
        private ITreeViewModel m_Parent = null;
        private bool m_IsSelected = false;
        private bool m_IsMultiSelected = false;
        private TreeViewMultiSelection m_MultiSelection = null;

        public SongMetadataTreeViewModel(string filename, SongMetadata song, ITreeViewModel parent, TreeViewMultiSelection multiSelection)
        {
            m_Filename = filename;
            m_Song = song;
            m_Parent = parent;
            m_MultiSelection = multiSelection;
            HierarchyID = -1;
        }

        public string DisplayString
        {
            get
            {
                return m_Filename;
            }
        }

        public void AddChild(ITreeViewModel child)
        {
            throw new Exception("Attempt to add a child to a SongMetadataTreeViewModel.");
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
                return new List<ITreeViewModel>(); // Can't have child nodes.
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
        }

        public SongMetadata Song
        {
            get
            {
                return m_Song;
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
                throw new Exception("SongMetadataTreeViewModel: attempt to compare to an incompatible object");
            }
        }

        public override string ToString()
        {
            return m_Parent.ToString() + "/" + m_Filename;
        }
    }
}