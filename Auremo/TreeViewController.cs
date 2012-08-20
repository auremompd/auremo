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
    public class TreeViewController
    {
        private IList<ITreeViewNode> m_RootLevelNodes = null;

        public TreeViewController(IList<ITreeViewNode> rootLevelNodes)
        {
            m_RootLevelNodes = rootLevelNodes;
            MultiSelection = new SortedSet<ITreeViewNode>();
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
            while (MultiSelection.Count > 0)
            {
                MultiSelection.First().IsMultiSelected = false;
            }
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

                SelectVisibleWithinRange(root, Math.Min(Pivot.ID, toNode.ID), Math.Max(Pivot.ID, toNode.ID));
            }
        }

        private void SelectVisibleWithinRange(ITreeViewNode node, int minID, int maxID)
        {
            // TODO: there is plenty left to optimize here.
            if (node.ID >= minID && node.ID <= maxID)
            {
                node.IsMultiSelected = true;
            }

            if (node.IsExpanded)
            {
                foreach (ITreeViewNode child in node.Children)
                {
                    SelectVisibleWithinRange(child, minID, maxID);

                    if (child.ID > maxID)
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

        public ISet<ITreeViewNode> MultiSelection
        {
            get;
            private set;
        }

        public ISet<SongMetadataTreeViewNode> Songs
        {
            get
            {
                ISet<SongMetadataTreeViewNode> result = new SortedSet<SongMetadataTreeViewNode>();

                foreach (ITreeViewNode node in MultiSelection)
                {
                    InsertSongs(node, result);
                }

                return result;
            }
        }
        
        private void InsertSongs(ITreeViewNode node, ISet<SongMetadataTreeViewNode> songs)
        {
            if (node is SongMetadataTreeViewNode)
            {
                songs.Add(node as SongMetadataTreeViewNode);
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
                    if (node.ID < current.ID)
                    {
                        best = node;
                    }
                    else
                    {
                        break;
                    }
                }

                if (best.ID < current.ID - 1 && best.IsExpanded)
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
                    else if (node.ID == current.ID + 1)
                    {
                        return node;
                    }
                    else if (node.ID > current.ID)
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
                else if (bestAfter.ID <= current.ID)
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
