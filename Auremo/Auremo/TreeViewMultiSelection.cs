using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class TreeViewMultiSelection
    {
        private IList<ITreeViewModel> m_RootLevelNodes = null;
        private ISet<ITreeViewModel> m_Members = new SortedSet<ITreeViewModel>();

        public TreeViewMultiSelection(IList<ITreeViewModel> rootLevelNodes)
        {
            m_RootLevelNodes = rootLevelNodes;
        }

        public ITreeViewModel FirstNode
        {
            get
            {
                if (m_RootLevelNodes == null || m_RootLevelNodes.Count == 0)
                {
                    return null;
                }
                else
                {
                    return m_RootLevelNodes.First();
                }
            }
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
            if (Pivot != null)
            {
                ITreeViewModel root = Pivot;

                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                SelectVisibleWithinRange(root, Math.Min(Pivot.HierarchyID, toNode.HierarchyID), Math.Max(Pivot.HierarchyID, toNode.HierarchyID));
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

        // Start point of range selection (mouse or key with shift down).
        public ITreeViewModel Pivot
        {
            get;
            set;
        }

        public ITreeViewModel Current
        {
            get;
            set;
        }

        public ITreeViewModel Previous
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetPredecessor(Current, m_RootLevelNodes, m_RootLevelNodes.First());
                }
            }
        }

        public ITreeViewModel Next
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                else
                {
                    return GetSuccessor(Current, m_RootLevelNodes, m_RootLevelNodes.Last());
                }
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

        private ITreeViewModel GetPredecessor(ITreeViewModel current, IList<ITreeViewModel> search, ITreeViewModel dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                ITreeViewModel best = dfault;

                foreach (ITreeViewModel node in search)
                {
                    if (node.HierarchyID < current.HierarchyID)
                    {
                        best = node;
                    }
                    else
                    {
                        break;
                    }
                }

                if (best.HierarchyID < current.HierarchyID - 1 && best.IsExpanded)
                {
                    return GetPredecessor(current, best.Children, best);
                }
                else
                {
                    return best;
                }
            }            
        }

        private ITreeViewModel GetSuccessor(ITreeViewModel current, IList<ITreeViewModel> search, ITreeViewModel dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                ITreeViewModel bestBefore = null;
                ITreeViewModel bestAfter = dfault;

                foreach (ITreeViewModel node in search)
                {
                    if (node == current)
                    {
                        if (node.IsExpanded)
                        {
                            return GetSuccessor(current, node.Children, bestAfter);
                        }
                    }
                    else if (node.HierarchyID == current.HierarchyID + 1)
                    {
                        return node;
                    }
                    else if (node.HierarchyID > current.HierarchyID)
                    {
                        bestAfter = node;
                        break;
                    }
                    else
                    {
                        bestBefore = node;
                    }
                }

                if (bestBefore != null && bestBefore.IsExpanded)
                {
                    return GetSuccessor(current, bestBefore.Children, bestAfter);
                }
                else if (bestAfter.HierarchyID <= current.HierarchyID)
                {
                    return current;
                }
                else
                {
                    return bestAfter;
                }
            }
        }
    }
}
