using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Auremo.Properties;

namespace Auremo
{
    public class DateNormalizer
    {
        List<DateTemplate> m_Templates = new List<DateTemplate>();

        public DateNormalizer(IEnumerable<string> formats)
        {
            foreach (string format in formats)
            {
                m_Templates.Add(new DateTemplate(format));
            }
        }

        public string Normalize(string date)
        {
            foreach (DateTemplate template in m_Templates)
            {
                string result = template.TryToParseDate(date);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
