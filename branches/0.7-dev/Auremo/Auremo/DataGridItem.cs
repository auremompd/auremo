using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public interface DataGridItem
    {
        object Content
        {
            get;
        }

        int Position
        {
            get;
        }

        bool IsSelected
        {
            get;
            set;
        }
    }
}
