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
    public interface ITreeViewModel : IComparable
    {
        string DisplayString { get; }
        void AddChild(ITreeViewModel child);
        ITreeViewModel Parent { get; }
        IList<ITreeViewModel> Children { get; }
        bool IsSelected { get; set; }
        bool IsExpanded { get; set; }
        bool IsMultiSelected { get; set; }
        TreeViewMultiSelection MultiSelection { get; }
        int HierarchyID { get; set; }
        void OnAncestorCollapsed();
    }
}
