using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using System.Diagnostics;

namespace Flex_Highlighter
{
    internal class CommentRanges
    {
        public int start;
        public int end;

        public CommentRanges(int s = 0, int e = 0)
        {
            start = s;
            end = e;
        }
    }
    internal sealed class FlexTokenizer
    {
        internal enum Cases
        {
            Comment,
            Option,
            Include,
            String
        }
        internal FlexTokenizer(IStandardClassificationService classifications) => Classifications = classifications;
        internal IStandardClassificationService Classifications { get; }
        private Dictionary<FlexTokenizer.Cases, bool> cases = new Dictionary<FlexTokenizer.Cases, bool>()
            {
                { Cases.Option, false },
                { Cases.Comment, false },
                { Cases.Include, false },
                { Cases.String, false }
            };
        private List<CommentRanges> Comments = new List<CommentRanges>();

        internal Token Scan( string text, int startIndex, int length, int startTokenId = -1, int startState = 0)
        {
            //public class Token
            //{
            //    public int StartIndex;
            //    public int Length;
            //    public int TokenId;
            //    public int State;
            //}

            int index = startIndex;
            Token token = new Token();
            token.StartIndex = index;
            token.TokenId = startTokenId;
            token.State = startState;
            token.Length = length - index;


            if (index + 1 < length && text[index] == '/' && text[index + 1] == '/')
            {
                token.TokenId = 3;
                return token;
            }

            if ((index + 1 < length && text[index] == '/' && text[index + 1] == '*') || token.State == 1)
            {
                if (index + 1 < length && text[index] == '/' && text[index + 1] == '*')
                {
                    index++;
                    token.State = 1;
                    token.TokenId = 2;
                }

                while (index < length)
                {
                    index = AdvanceWhile(text, ++index, chr => chr != '*');
                    if (index + 1 < length && text[index + 1] == '/')
                    {
                        token.State = 0;
                        token.Length = index + 2 - startIndex;
                        return token;
                    }
                }
                return token;
            }

            int start = index;
            index = AdvanceWhile(text, index, chr => Char.IsWhiteSpace(chr));

            if (index > start)
            {
                token.TokenId = 0;
                token.Length = index - start;
                return token;
            }

            if (text[index] == '\"')
            {
                index = AdvanceWhile(text, ++index, chr => chr != '\"');
                token.TokenId = 5;
                token.Length = ++index - start;
                return token;
            }

            start = index;
            if (Char.IsLetterOrDigit(text[index]))
            {
                index = AdvanceWhile(text, index, chr => Char.IsLetterOrDigit(chr));
            }
            else
            {
                index++;
            }

            string word = text.Substring(start, index - start);
            if (IsDecimalInteger(word))
            {
                token.TokenId = 4;
                token.Length = index - start;
                return token;
            }
            else
            {
                token.TokenId = FlexKeywords.Contains(word) ? 1 : -1;
                token.Length = index - start;
            }


            return token;
        }

        internal int AdvanceWord(string text, int index, out IClassificationType classification, int globalStart, int globalEnd)
        {
            int length = text.Length;
            if (index >= length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index + 1 < length && text[index] == '/' && text[index+1] == '/')
            {
                classification = Classifications.Comment;
                return length;
            }

            if ((index + 1 < length && text[index] == '/' && text[index + 1] == '*') || cases[Cases.Comment])
            {
                if ((index + 1 < length && text[index] == '/' && text[index + 1] == '*'))
                {
                    index += 2;
                    if (!cases[Cases.Comment])
                    {
                        if (!Comments.Any(r => r.start == index + globalStart))
                        {
                            Comments.Add(new CommentRanges(index + globalStart));
                        }
                    }
                }

                while (index < length)
                {
                    index = AdvanceWhile(text, index, chr => chr != '*');
                    if(index + 1 < length && text[index+1] == '/')
                    {
                        cases[Cases.Comment] = false;
                        index += 2;
                        Comments.Last().end = index + globalStart;
                        classification = Classifications.Comment;
                        return index;
                    }
                }
                cases[Cases.Comment] = true;
                classification = Classifications.Comment;
                return index;
            }

            int start = index;
            index = AdvanceWhile(text, index, chr => Char.IsWhiteSpace(chr));

            if (index > start)
            {
                classification = Classifications.WhiteSpace;
                return index;
            }

            if (text[index] == '\"')
            {
                index = AdvanceWhile(text, ++index, chr => chr != '\"');
                classification = Classifications.StringLiteral;
                return ++index;
            }

            if (index + 1 < length && text[index] == '%' && text[index + 1] == '%')
            {
                classification = Classifications.ExcludedCode;
                index += 2;
                return index;
            }

            if (index + 1 < length && text[index] == '%' && (text[index + 1] == '{' || text[index + 1] == '}'))
            {
                classification = Classifications.ExcludedCode;
                return ++index;
            }

            if (text[index] == '%')
            {
                classification = Classifications.ExcludedCode;
                cases[Cases.Option] = true;
                return ++index;
            }

            if (cases[Cases.Option])
            {
                index = AdvanceWhile(text, index, chr => !Char.IsWhiteSpace(chr));
                classification = Classifications.SymbolReference;
                cases[Cases.Option] = false;
                return index;
            }

            if (Char.IsPunctuation(text[index]))
            {
                classification = Classifications.Operator;
                return ++index;
            }

            start = index;
            if(Char.IsLetterOrDigit(text[index]))
            {
                index = AdvanceWhile(text, index, chr => Char.IsLetterOrDigit(chr));
            }
            else
            {
                index++;
            }

            string word = text.Substring(start, index - start);
            if (IsDecimalInteger(word))
            {
                classification = Classifications.StringLiteral;
            }
            else
            {
                classification = FlexKeywords.Contains(word) ? Classifications.Keyword : Classifications.Other;
            }

            return index;
        }

        private bool IsDecimalInteger(string word)
        {
            foreach (var chr in word)
            {
                if (chr < '0' || chr > '9')
                {
                    return false;
                }
            }
            return true;
        }

        private int AdvanceWhile(string text, int index, Func<char, bool> predicate)
        {
            for (int length = text.Length; index < length && predicate(text[index]); index++) ;
                return index;
        }
    }
}
