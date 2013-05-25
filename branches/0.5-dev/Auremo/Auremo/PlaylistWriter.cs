using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class PlaylistWriter
    {
        public static string Write(IEnumerable<Playable> playables)
        {
            if (playables.Count() > 0)
            {
                StringBuilder result = new StringBuilder();

                result.Append("[playlist]\r\n");
                result.Append("NumberOfEntries=" + playables.Count() + "\r\n");

                int entryIndex = 1;

                foreach (StreamMetadata entry in playables)
                {
                    result.Append("File" + entryIndex + "=" + entry.Path + "\r\n");
                    result.Append("Title" + entryIndex + "=" + entry.Title + "\r\n");
                    entryIndex += 1;
                }

                result.Append("Version=2\r\n");

                return result.ToString();
            }
            else
            {
                return null;
            }

        }
    }
}
