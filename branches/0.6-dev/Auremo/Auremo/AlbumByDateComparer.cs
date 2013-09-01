using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class AlbumByDateComparer : IComparer<AlbumMetadata>
    {
        public int Compare(AlbumMetadata lhs, AlbumMetadata rhs)
        {
            if (lhs.Artist != rhs.Artist)
            {
                return lhs.Artist.CompareTo(rhs.Artist);
            }
            else if (lhs.Date == rhs.Date)
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
            else if (lhs.Date == null)
            {
                return 1;
            }
            else if (rhs.Date == null)
            {
                return -1;
            }
            else 
            {
                return lhs.Date.CompareTo(rhs.Date);
            }
        }
    }
}
