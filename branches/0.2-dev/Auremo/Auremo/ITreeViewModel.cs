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
    }
}
