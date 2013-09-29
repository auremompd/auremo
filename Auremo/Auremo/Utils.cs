/*
 * Copyright 2013 Mikko Teräs and Niilo Säämänen.
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
    public class Utils
    {
        public static int? StringToInt(string s)
        {
            int result = 0;

            if (int.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static int StringToInt(string s, int dfault)
        {
            int result = 0;

            if (int.TryParse(s, out result))
            {
                return result;
            }
            else
            {
                return dfault;
            }
        }

        public static string IntToTimecode(int seconds)
        {
            if (seconds < 0)
                return "00";

            int secs = seconds % 60;
            int mins = seconds / 60 % 60;
            int hours = seconds / 3600;

            string result = "";

            if (hours > 0)
                result = hours + ":";

            if (mins < 10)
                result += "0" + mins + ":";
            else
                result += mins + ":";

            if (secs < 10)
                result += "0" + secs;
            else
                result += secs;

            return result;
        }

        public static Tuple<string, string> SplitPath(string path)
        {
            int limit = path.LastIndexOf('/');

            if (limit == -1)
            {
                return new Tuple<string, string>("", path);
            }
            else
            {
                return new Tuple<string, string>(path.Substring(0, limit), path.Substring(limit + 1));
            }
        }

        public static int Clamp(int min, int value, int max)
        {
            return min < value ? (max > value ? value : max) : min;
        }

        public static IList<T> ToTypedList<T>(System.Collections.IEnumerable source)
        {
            IList<T> result = new List<T>();

            foreach (object o in source)
            {
                try
                {
                    T item = (T)o;
                }
                catch (Exception)
                {
                    throw new Exception("ToTypedList: attempted to cast " + o.GetType().ToString() + " to " + typeof(T).ToString() + ".");
                }
                
                result.Add((T)o);
            }

            return result;
        }

        public static string ExtractYearFromDateString(string date)
        {
            if (date == null)
            {
                return null;
            }
            else
            {
                return date.Substring(0, 4);
            }
        }

        public static void ParseSongListResponse(ServerResponse response, DateNormalizer dateNormalizer, IList<PlaylistItem> result)
        {
            result.Clear();

            if (response != null && response.IsOK)
            {
                PlaylistItem item = new PlaylistItem();
                SongMetadata song = new SongMetadata();
                item.Playable = song;

                foreach (ServerResponseLine line in response.ResponseLines)
                {
                    if (line.Name == "file")
                    {
                        if (song.Path != null && (song.IsLocal || song.IsSpotify))
                        {
                            result.Add(item);
                        }

                        item = new PlaylistItem();
                        song = new SongMetadata();
                        item.Playable = song;
                        song.Path = line.Value;
                    }
                    else if (line.Name == "Title")
                    {
                        song.Title = line.Value;
                    }
                    else if (line.Name == "Artist")
                    {
                        song.Artist = line.Value;
                    }
                    else if (line.Name == "Album")
                    {
                        song.Album = line.Value;
                    }
                    else if (line.Name == "Genre")
                    {
                        song.Genre = line.Value;
                    }
                    else if (line.Name == "Time")
                    {
                        song.Length = Utils.StringToInt(line.Value);
                    }
                    else if (line.Name == "Date")
                    {
                        song.Date = dateNormalizer.Normalize(line.Value);
                    }
                    else if (line.Name == "Track")
                    {
                        song.Track = Utils.StringToInt(line.Value);
                    }
                    else if (line.Name == "Id")
                    {
                        int? id = Utils.StringToInt(line.Value);
                        item.Id = id.HasValue ? id.Value : -1;
                    }
                    else if (line.Name == "Pos")
                    {
                        int? position = Utils.StringToInt(line.Value);
                        item.Position = position.HasValue ? position.Value : -1;
                    }
                }

                if (song.Path != null)
                {
                    result.Add(item);
                }
            }
        }
    }
}
