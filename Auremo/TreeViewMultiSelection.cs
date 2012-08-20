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
using System.Linq;
using System.Text;

namespace Auremo
{
    public class TreeViewMultiSelection
    {
        private IList<ITreeViewNode> m_RootLevelNodes = null;

        public TreeViewMultiSelection(IList<ITreeViewNode> rootLevelNodes)
        {
            m_RootLevelNodes = rootLevelNodes;
            Members = new SortedSet<ITreeViewNode>();
        }

        public ITreeViewNode FirstNode
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
            while (Members.Count > 0)
            {
                Members.First().IsMultiSelected = false;
            }
        }

        public void Add(ITreeViewNode node)
        {
            Members.Add(node);
        }

        public void Remove(ITreeViewNode node)
        {
            Members.Remove(node);
        }

        public void SelectRange(ITreeViewNode toNode)
        {
            if (Pivot != null)
            {
                ITreeViewNode root = Pivot;

                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                SelectVisibleWithinRange(root, Math.Min(Pivot.HierarchyID, toNode.HierarchyID), Math.Max(Pivot.HierarchyID, toNode.HierarchyID));
            }
        }

        private void SelectVisibleWithinRange(ITreeViewNode node, int minID, int maxID)
        {
            // TODO: there is plenty left to optimize here.
            if (node.HierarchyID >= minID && node.HierarchyID <= maxID)
            {
                node.IsMultiSelected = true;
            }

            if (node.IsExpanded)
            {
                foreach (ITreeViewNode child in node.Children)
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
        public ITreeViewNode Pivot
        {
            get;
            set;
        }

        public ITreeViewNode Current
        {
            get;
            set;
        }

        public ITreeViewNode Previous
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

        public ITreeViewNode Next
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

        public ISet<ITreeViewNode> Members
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewModel> Songs
        {
            get
            {
                ISet<SongMetadataTreeViewModel> result = new SortedSet<SongMetadataTreeViewModel>();

                foreach (ITreeViewNode node in Members)
                {
                    InsertSongs(node, result);
                }

                return result;
            }
        }
        
        private void InsertSongs(ITreeViewNode node, ISet<SongMetadataTreeViewModel> songs)
        {
            if (node is SongMetadataTreeViewModel)
            {
                songs.Add(node as SongMetadataTreeViewModel);
            }
            else
            {
                foreach (ITreeViewNode child in node.Children)
                {
                    InsertSongs(child, songs);
                }
            }
        }

        private ITreeViewNode GetPredecessor(ITreeViewNode current, IList<ITreeViewNode> search, ITreeViewNode dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                ITreeViewNode best = dfault;

                foreach (ITreeViewNode node in search)
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

        private ITreeViewNode GetSuccessor(ITreeViewNode current, IList<ITreeViewNode> search, ITreeViewNode dfault)
        {
            if (search == null || search.Count == 0)
            {
                return dfault;
            }
            else
            {
                ITreeViewNode bestBefore = null;
                ITreeViewNode bestAfter = dfault;

                foreach (ITreeViewNode node in search)
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
