using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public interface Playable
    {
        string Path
        {
            get;
        }

        string Title
        {
            get;
        }

        string Artist
        {
            get;
        }

        string Album
        {
            get;
        }

        int? Year
        {
            get;
        }
    }
}
