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
    class SongMetadataTreeViewModel : ITreeViewModel, INotifyPropertyChanged
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

        private SongMetadata m_Song;
        private ITreeViewModel m_Parent;
        private bool m_IsSelected = false;

        public SongMetadataTreeViewModel(SongMetadata song, ITreeViewModel parent)
        {
            m_Song = song;
            m_Parent = parent;
        }

        public string DisplayString
        {
            get
            {
                return m_Song.Title;
            }
        }

        public void AddChild(ITreeViewModel child)
        {
            throw new Exception("Attempt to add a child to a SongMetadataTreeViewModel.");
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
                throw new Exception("Attempt to expand a SongMetadataTreeViewModel.");
            }
        }

        public override string ToString()
        {
            return m_Parent.ToString() + m_Song.Title;
        }
    }
}