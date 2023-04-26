using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPokerEngine.Utils
{
    public static class DictionaryUtils
    {
        public static IDictionary Update(IDictionary source, params IDictionary[] targets)
        {
            foreach (var target in targets)
            {
                foreach (var key in target.Keys)
                {
                    source[key] = target[key];
                }
            }
            return source;
        }

        public static string PrintForMessageSummarizer(IDictionary source)
            => $"{{{string.Join(", ", source.Keys.Cast<object>().ToList().Select(key => $"'{key}': {source[key]}"))}}}";
    }
}
