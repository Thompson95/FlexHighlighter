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
        private static readonly List<string> keywordsC = new List<string>
        {
            "%%", "%{", "}%", "int", "double", "float", "string", "void", "return", "switch", "if", "else", "while", "true", "false"
        };
        private static readonly HashSet<string> keywordSetC = new HashSet<string>(keywordsC, StringComparer.OrdinalIgnoreCase);
        internal static IReadOnlyList<string> AllC { get; } = new ReadOnlyCollection<string>(keywordsC);
        internal static bool CContains(string word) => keywordSetC.Contains(word);


        private static readonly List<string> keywordsFlex = new List<string>
        {
            "%option"
        };
        private static readonly HashSet<string> keywordSetFlex = new HashSet<string>(keywordsFlex, StringComparer.OrdinalIgnoreCase);
        internal static IReadOnlyList<string> AllFlex { get; } = new ReadOnlyCollection<string>(keywordsFlex);
        internal static bool FlexContains(string word) => keywordSetFlex.Contains(word);
    }
}
