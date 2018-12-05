using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flex_Highlighter
{
    internal static class FlexKeywords
    {
        private static readonly List<string> keywords = new List<string>
        {
            "%%", "%{", "}%", "int", "double", "float", "string", "void", "return", "switch", "if", "else", "while", "true", "false"
        };

        private static readonly HashSet<string> keywordSet = new HashSet<string>(keywords, StringComparer.OrdinalIgnoreCase);

        internal static IReadOnlyList<string> All { get; } = new ReadOnlyCollection<string>(keywords);

        internal static bool Contains(string word) => keywordSet.Contains(word);
    }
}
