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
            "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum", "extern", "float", "for", "goto", "if", "int",
            "long", "register", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while" 
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
