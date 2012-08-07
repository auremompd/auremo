using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public interface ITreeViewModel
    {
        string DisplayString { get; }
        void AddChild(ITreeViewModel child);
        IList<ITreeViewModel> Children { get; }
        bool IsSelected { get; set; }
        bool IsExpanded { get; set; }
    }
}
