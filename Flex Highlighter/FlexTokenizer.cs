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

        internal static class Classes
        {
            internal readonly static short WhiteSpace = 0;
            internal readonly static short Keyword = 1;
            internal readonly static short MultiLineComment = 2;
            internal readonly static short Comment = 3;
            internal readonly static short NumberLiteral = 4;
            internal readonly static short StringLiteral = 5;
            internal readonly static short ExcludedCode = 6;
            internal readonly static short FlexDefinition = 7;
            internal readonly static short Other = -1;
            internal readonly static short C = -2;
            internal readonly static short FlexDefinitions = -3;
        }
        internal FlexTokenizer(IStandardClassificationService classifications) => Classifications = classifications;
        internal IStandardClassificationService Classifications { get; }
        private List<CommentRanges> Comments = new List<CommentRanges>();
        private List<string> Definitions = new List<string>();

        internal Token Scan( string text, int startIndex, int length, Languages language, ref Cases ecase, int startTokenId = -1, int startState = 0)
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
                token.TokenId = Classes.Comment;
                return token;
            }

            if ((index + 1 < length && text[index] == '/' && text[index + 1] == '*') || token.State == (int)Cases.MultiLineComment)
            {
                if (index + 1 < length && text[index] == '/' && text[index + 1] == '*')
                {
                    index++;
                    token.State = (int)Cases.MultiLineComment;
                    token.TokenId = Classes.MultiLineComment;
                }

                while (index < length)
                {
                    index = AdvanceWhile(text, ++index, chr => chr != '*');
                    if (index + 1 < length && text[index + 1] == '/')
                    {
                        token.State = (int)Cases.NoCase;
                        token.Length = index + 2 - startIndex;
                        return token;
                    }
                }
                return token;
            }

            
            int start = index;
            if (((index + 1 < length && text[index] == '%' && text[index + 1] == '{') || token.State == (int)Cases.C) && language == Languages.Flex)
            {
                if ((index + 1 < length && text[index] == '%' && text[index + 1] == '{'))
                {
                    index += 2;
                    token.State = (int)Cases.C;
                    token.TokenId = Classes.C;
                }

                while (index < length)
                {
                    index = AdvanceWhile(text, index, chr => chr != '%');
                    if (index + 1 < length && text[index + 1] == '}' && (index - 1 > 0 && text[index - 1] == '\n'))
                    {
                        index += 2;
                        token.StartIndex = start;
                        token.State = (int)Cases.NoCase;
                        token.TokenId = Classes.C;
                        token.Length = index - start;
                        return token;
                    }
                    index++;
                    if (index >= length)
                    {
                        index += 2;
                        token.StartIndex = start;
                        token.TokenId = Classes.C;
                        return token;
                    }
                }
            }

            if (((index + 1 < length && text[index] == '%' && text[index + 1] == '}') || token.State == (int)Cases.FlexDefinitions) && language == Languages.C)
            {
                if ((index + 1 < length && text[index] == '%' && text[index + 1] == '}'))
                {
                    index += 2;
                    token.State = (int)Cases.FlexDefinitions;
                }

                while (index < length)
                {
                    index = AdvanceWhile(text, index, chr => chr != '%');
                    if (index + 1 < length && text[index + 1] == '%' && (index - 1 > 0 && text[index - 1] == '\n'))
                    {
                        index += 2;
                        token.StartIndex = start;
                        token.State = (int)Cases.NoCase;
                        token.TokenId = Classes.FlexDefinitions;
                        token.Length = index - start;
                        return token;
                    }
                    index++;
                    if (index >= length)
                    {
                        token.StartIndex = start;
                        return token;
                    }
                }
            }

            index = start;
            if ((text[index] == '\t' || token.State == (int)Cases.CIndent) && language == Languages.Flex)
            {
                if (text[index] == '\t')
                {
                    index++;
                    token.State = (int)Cases.CIndent;
                    token.TokenId = Classes.C;
                }
                while (index < length)
                {
                    index = AdvanceWhile(text, index, chr => chr != '\n');
                    token.StartIndex = start;
                    token.State = (int)Cases.NoCase;
                    token.TokenId = Classes.C;
                    token.Length = index - start;
                    return token;
                }
            }

            index = start;
            if (((index + 1 < length && text[index] == '%' && text[index + 1] == '%') || token.State == (int)Cases.CEnding) && language == Languages.Flex)
            {
                if (index + 1 < length && text[index] == '%' && text[index + 1] == '%')
                {
                    index += 2;
                    token.State = (int)Cases.CEnding;
                }

                while(index < length)
                {
                    index = AdvanceWhile(text, index, chr => chr != '%');
                    if (index + 1 < length && text[index + 1] == '%')
                    {
                        index += 2;
                        token.StartIndex = index;
                        token.TokenId = Classes.C;
                        token.State = (int)Cases.NoCase;
                        ecase = Cases.CEnding;
                        return token;
                    }
                    if (index >= length)
                    {
                        return token;
                    }
                    index++;
                }
            }

            index = start;
            index = AdvanceWhile(text, index, chr => Char.IsWhiteSpace(chr));

            if (index > start)
            {
                token.TokenId = Classes.WhiteSpace;
                token.Length = index - start;
                return token;
            }

            if (language == Languages.FlexDefinitions)
            {
                if(start == 0)
                {
                    index = start;
                    if (text[index] == '_' || Char.IsLetter(text[index]))
                    {
                        index++;
                        index = AdvanceWhileDefinition(text, index);
                        if (Char.IsWhiteSpace(text[index]))
                        {
                            Definitions.Add(new string(text.ToCharArray(), start, index));
                            token.Length = index - start;
                            token.TokenId = Classes.FlexDefinition;
                            return token;
                        }
                    }
                    
                }
                start = index;
            }

            if(language == Languages.C)
            {
                if (text[index] == '\"')
                {
                    index = AdvanceWhile(text, ++index, chr => chr != '\"');
                    token.TokenId = Classes.StringLiteral;
                    token.Length = index - start + (text.IndexOf('\"', index) != -1 ? 1 : 0);
                    return token;
                }

                if (ecase == Cases.Include)
                {
                    index = AdvanceWhile(text, index, chr => chr != '>');
                    token.TokenId = Classes.StringLiteral;
                    token.Length = index - token.StartIndex + (text.IndexOf('>', start) != -1 ? 1 : 0);
                    ecase = Cases.NoCase;
                    return token;
                }

                string[] test = { "#include", "#define" };
                foreach (var s in test)
                {
                    int i = text.IndexOf(s);
                    if(i == index)
                    {
                        switch (s)
                        {
                            case "#include":
                                token.Length = s.Length;
                                token.TokenId = Classes.ExcludedCode;
                                ecase = Cases.Include;
                                return token;
                            case "#define":
                                token.Length = s.Length;
                                token.TokenId = Classes.ExcludedCode;
                                ecase = Cases.Include;
                                return token;
                            default:
                                break;
                        }
                    }
                }
            }
            if (language == Languages.Flex)
            {
                if (text[index] == '#')
                {
                    token.Length = 1;
                    token.TokenId = Classes.NumberLiteral;
                    return token;
                }
                string[] test = { "%option" };
                foreach (var s in test)
                {
                    int i = text.IndexOf(s);
                    if (i == index)
                    {
                        switch (s)
                        {
                            case "%option":
                                token.Length = s.Length;
                                token.TokenId = Classes.ExcludedCode;
                                ecase = Cases.Include;
                                return token;
                            default:
                                break;
                        }
                    }
                }
            }

            start = index;
            if (Char.IsDigit(text[index]))
            {
                index = AdvanceWhile(text, index, chr => Char.IsDigit(chr));
            }
            else if(Char.IsLetter(text[index]))
            {
                index = AdvanceWhile(text, index, chr => Char.IsLetter(chr));
            }
            else
            {
                index++;
            }
            string word = text.Substring(start, index - start);
            if (IsDecimalInteger(word))
            {
                token.TokenId = Classes.NumberLiteral;
                token.Length = index - start;
                return token;
            }
            else
            {
                if (language == Languages.C)
                {
                    token.TokenId = FlexKeywords.CContains(word) ? Classes.Keyword : Classes.Other;
                }
                else
                {
                    token.TokenId = FlexKeywords.FlexContains(word) ? Classes.Keyword : Classes.Other;
                }
                token.Length = index - start;
            }
            return token;
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

        private int AdvanceWhileDefinition(string text, int index)
        {
            for (int length = text.Length; index < length; index++)
            {
                if (!(Char.IsLetterOrDigit(text[index]) || text[index] == '_'))
                {
                    break;
                }
            }
            return index;
        }
    }
}
