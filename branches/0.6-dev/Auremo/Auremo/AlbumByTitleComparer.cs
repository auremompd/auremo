using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class AlbumByTitleComparer : IComparer<AlbumMetadata>
    {
        public int Compare(AlbumMetadata lhs, AlbumMetadata rhs)
        {
            if (lhs.Artist != rhs.Artist)
            {
                return lhs.Artist.CompareTo(rhs.Artist);
            }
            else
            {
                return lhs.Title.CompareTo(rhs.Title);
            }
        }
    }
}
