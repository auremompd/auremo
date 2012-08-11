using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class TreeViewMultiSelection
    {
        private ISet<ITreeViewModel> m_Members = new SortedSet<ITreeViewModel>();
        private ITreeViewModel m_Pivot = null; // Starting point for shift down multiselect

        public TreeViewMultiSelection()
        {
        }

        public void Clear()
        {
            while (m_Members.Count > 0)
            {
                m_Members.First().IsMultiSelected = false;
            }
        }

        public void Add(ITreeViewModel node)
        {
            m_Members.Add(node);
        }

        public void Remove(ITreeViewModel node)
        {
            m_Members.Remove(node);
        }

        public void SelectRange(ITreeViewModel toNode)
        {
            if (m_Pivot != null)
            {
                ITreeViewModel root = m_Pivot;

                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                SelectVisibleWithinRange(root, Math.Min(m_Pivot.HierarchyID, toNode.HierarchyID), Math.Max(m_Pivot.HierarchyID, toNode.HierarchyID));
            }
        }

        private void SelectVisibleWithinRange(ITreeViewModel node, int minID, int maxID)
        {
            // TODO: there is plenty left to optimize here.
            if (node.HierarchyID >= minID && node.HierarchyID <= maxID)
            {
                node.IsMultiSelected = true;
            }

            if (node.IsExpanded)
            {
                foreach (ITreeViewModel child in node.Children)
                {
                    SelectVisibleWithinRange(child, minID, maxID);

                    if (child.HierarchyID > maxID)
                    {
                        return;
                    }
                }
            }
        }

        public ITreeViewModel Pivot
        {
            get
            {
                return m_Pivot;
            }
            set
            {
                m_Pivot = value;
            }
        }

        public ISet<ITreeViewModel> Members
        {
            get
            {
                return m_Members;
            }
        }

        public ISet<SongMetadataTreeViewModel> Songs
        {
            get
            {
                ISet<SongMetadataTreeViewModel> result = new SortedSet<SongMetadataTreeViewModel>();

                foreach (ITreeViewModel node in m_Members)
                {
                    InsertSongs(node, result);
                }

                return result;
            }
        }
        
        private void InsertSongs(ITreeViewModel node, ISet<SongMetadataTreeViewModel> songs)
        {
            if (node is SongMetadataTreeViewModel)
            {
                songs.Add(node as SongMetadataTreeViewModel);
            }
            else
            {
                foreach (ITreeViewModel child in node.Children)
                {
                    InsertSongs(child, songs);
                }
            }
        }
    }
}
