using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class MPDResponseLine
    {
        public enum Keyword
        {
            OK = 0, ACK,
            State, Volume, Playlist, Song, Time, Random, Repeat, Audio, Error,
            File, Name, Title, Artist, Album, Genre, Date, Track, Id, Pos,
            DbUpdate,
            OutputId, OutputName, OutputEnabled,
            Unknown
        }

        // Lowercase spellings of above, ordered and excluding Unknown
        private string[] KeywordSpelling =
        {
            "ok", "ack",
            "state", "volume", "playlist", "song", "time", "random", "repeat", "audio", "error",
            "file", "name", "title", "artist", "album", "genre", "date", "track", "id", "pos",
            "db_update",
            "outputid", "outputname", "outputenabled"
        };

        public MPDResponseLine(string input)
        {
            int i = 0;

            while (i < input.Length && input[i] != ' ' && input[i] != ':')
            {
                i += 1;
            }

            string key = input.Substring(0, i).ToLowerInvariant();

            while (i < input.Length && (input[i] == ' ' || input[i] == ':'))
            {
                i += 1;
            }

            Literal = input;
            Value = input.Substring(i);
            Key = 0;

            while (Key != Keyword.Unknown && key != KeywordSpelling[(int)Key])
            {
                ++Key;
            }
        }

        public string Literal
        {
            get;
            private set;
        }

        public Keyword Key
        {
            get;
            private set;
        }

        public string Value
        {
            get;
            private set;
        }

        public int IntValue
        {
            get
            {
                int? result = Utils.StringToInt(Value);
                return result.HasValue ? result.Value : -1;
            }
        }

        public int[] IntListValue
        {
            get
            {
                string[] resultStr = Value.Split(':');
                int[] result = new int[resultStr.Length];

                for (int i = 0; i < resultStr.Count(); ++i)
                {
                    int? part = Utils.StringToInt(resultStr[i]);
                    result[i] = part.HasValue ? part.Value : -1;
                }

                return result;
            }
        }

        public bool IsStatus
        {
            get
            {
                return Key == Keyword.OK || Key == Keyword.ACK;
            }
        }

        public override string ToString()
        {
            return Literal;
        }
    }
}
