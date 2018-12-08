using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Linq;

namespace Flex_Highlighter
{
    public class MultiLineToken
    {
        public IClassificationType Classification;
        public ITrackingSpan Tracking;
        public ITextVersion Version;
        public Languages Language;
    }
    public enum Cases
    {
        NoCase = 0,
        Comment = 1,
        MultiLineComment = 2,
        Option = 3,
        Include = 4,
        String = 5,
        C = 6,
        Flex = 7,
        CIndent = 8,
        CEnding = 9,
        FlexDefinitions = 10
    }
    public class Token
    {
        public int StartIndex;
        public int Length;
        public int TokenId;
        public int State;
    }

    public enum Languages
    {
        Flex,
        C,
        FlexDefinitions
    }
    /// <summary>
    /// Classifier that classifies all text as an instance of the "FlexerClassifier" classification type.
    /// </summary>
    internal sealed class FlexClassifier : IClassifier
    {
        /// <summary>
        /// Classification type.
        /// </summary>
        ///
        internal List<MultiLineToken> _multiLineTokens;
        private readonly IClassificationType classificationType;
        internal readonly IClassificationType FlexDefinitionType;
        internal readonly IClassificationType FlexSection;
        internal readonly IClassificationType CSection;
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal FlexClassifier(ITextBuffer buffer, IStandardClassificationService classification, IClassificationTypeRegistryService registry)
        {
            ClassificationRegistry = registry;
            Classification = classification;
            Buffer = buffer;
            FlexDefinitionType = registry.CreateClassificationType("Flex Definition", new IClassificationType[0]);
            CSection = registry.CreateClassificationType("CSection", new IClassificationType[0]);
            FlexSection = registry.CreateClassificationType("FlexSection", new IClassificationType[0]);
            _multiLineTokens = new List<MultiLineToken>();

            tokenizer = new FlexTokenizer(classification);
            this.classificationType = registry.GetClassificationType("FlexerClassifier");
        }


        private readonly FlexTokenizer tokenizer;

        internal ITextBuffer Buffer { get; }
        internal IClassificationTypeRegistryService ClassificationRegistry { get; }
        internal IStandardClassificationService Classification { get; }

        #region IClassifier
        internal void Invalidate(SnapshotSpan span)
        {
            if (ClassificationChanged != null)
            {
                ClassificationChanged(this, new ClassificationChangedEventArgs(span));
            }
        }

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();
            bool isInsideMultiline = false;
            Languages language = Languages.Flex;
            Cases ecase = Cases.NoCase;

            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;
            //SortTokens(snapshot);
            //bool languageAssigned = false;
            for (int i = _multiLineTokens.Count - 1; i >= 0; i--)
            {
                var multiSpan = _multiLineTokens[i].Tracking.GetSpan(span.Snapshot);
                if (multiSpan.Length == 0)
                {
                    _multiLineTokens.RemoveAt(i);
                }
                else
                {
                    if (span.IntersectsWith(multiSpan))
                    {
                        if (span.End == multiSpan.Start)
                            continue;
                        isInsideMultiline = true;
                        if (span.Snapshot.Version != _multiLineTokens[i].Version)
                        {
                            if (_multiLineTokens[i].Classification != null)
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                            //if (!languageAssigned)
                            //{
                            //    language = _multiLineTokens[i].Language;
                            //    languageAssigned = true;
                            //}
                            language = _multiLineTokens[i].Language;
                            _multiLineTokens.RemoveAt(i);
                            MultiLineToken mlt;
                            if (language == Languages.FlexDefinitions)
                                mlt = GetLanguageSpan(multiSpan, Languages.C);
                            else
                                mlt = GetLanguageSpan(multiSpan);
                            if (mlt != null)
                            {
                                _multiLineTokens.Add(mlt);
                                Invalidate(mlt.Tracking.GetSpan(span.Snapshot));
                            }
                            else
                            {
                                Invalidate(multiSpan);
                            }

                            //if (span.Start <= multiSpan.Start && span.End >= multiSpan.End)
                            //    Invalidate(span);
                            //else
                            //    Invalidate(multiSpan);
                            //return list;
                            //var classifications = GetClassificationSpansForLanguage(new SnapshotSpan(snapshot, (Span.FromBounds(Math.Min(span.Start, multiSpan.Start), Math.Max(span.End, multiSpan.End)))), l).ToList();
                            //return classifications;
                        }
                        else
                        {
                            language = _multiLineTokens[i].Language;
                            if (_multiLineTokens[i].Classification != null)
                            {
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                                return list;
                            }
                        }
                    }
                }
            }

            if (!isInsideMultiline || language == Languages.C || language == Languages.FlexDefinitions)
            {
                int startPosition;
                int endPosition;
                int currentOffset = 0;
                string currentText = span.GetText();

                do
                {
                    startPosition = span.Start.Position + currentOffset;
                    endPosition = startPosition;

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, language, ref ecase, -1, 0);

                    if (token != null)
                    {
                        endPosition = startPosition + token.Length;

                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            token = tokenizer.Scan(currentText, 0, currentText.Length, language, ref ecase, token.TokenId, token.State);
                            if (ecase == Cases.CEnding)
                            {
                                startPosition += token.StartIndex;
                                endPosition = span.Snapshot.Length;
                            }
                            else if (token != null)
                            {
                                endPosition += token.Length;
                            }
                        }
                        bool multiLineToken = false;
                        if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.FlexDefinitions)
                        {
                            endPosition -= 2;
                        }
                        IClassificationType classification = null;

                        switch (token.TokenId)
                        {
                            case 0:
                                classification = Classification.WhiteSpace;
                                break;
                            case 1:
                                classification = Classification.Keyword;
                                break;
                            case 2:
                                classification = Classification.Comment;
                                multiLineToken = true;
                                break;
                            case 3:
                                classification = Classification.Comment;
                                break;
                            case 4:
                                classification = Classification.NumberLiteral;
                                break;
                            case 5:
                                classification = Classification.StringLiteral;
                                break;
                            case 6:
                                classification = Classification.ExcludedCode;
                                break;
                            case 7:
                                classification = FlexDefinitionType;
                                break;
                            case -1:
                                classification = Classification.Other;
                                break;
                            case -2:
                                multiLineToken = true;
                                break;
                            case -3:
                                multiLineToken = true;
                                break;
                            default:
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (token.TokenId != FlexTokenizer.Classes.C && token.TokenId != FlexTokenizer.Classes.FlexDefinitions)
                            list.Add(new ClassificationSpan(tokenSpan, classification));

                        if (multiLineToken)
                        {
                            if (!_multiLineTokens.Any(a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
                            {
                                _multiLineTokens.Add(new MultiLineToken()
                                {
                                    Classification = classification,
                                    Version = span.Snapshot.Version,
                                    Tracking = span.Snapshot.CreateTrackingSpan(tokenSpan.Span, SpanTrackingMode.EdgeExclusive),
                                    Language = GetLanguage(token.TokenId)
                                });

                                if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.FlexDefinitions)
                                {
                                    //list = GetClassificationSpansForLanguage(span, Languages.C).ToList();
                                    Invalidate(tokenSpan);
                                    return list;
                                }
                                else if (tokenSpan.End > span.End)
                                {
                                    Invalidate(new SnapshotSpan(span.End + 1, tokenSpan.End));
                                    return list;
                                }
                            }
                        }
                        currentOffset += token.Length;
                    }
                    if (token == null)
                    {
                        break;
                    }
                } while (currentOffset < currentText.Length);
            }
            return list;
        }
        private Languages GetLanguage(int value)
        {
            switch (value)
            {
                case -2:
                    return Languages.C;
                case -3:
                    return Languages.FlexDefinitions;
                default:
                    return Languages.Flex;
            }
        }
        public IList<ClassificationSpan> GetClassificationSpansForLanguage(SnapshotSpan span, Languages l)
        {
            var list = new List<ClassificationSpan>();
            bool isInsideMultiline = false;
            Languages language = l;
            Cases ecase = Cases.NoCase;

            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;

            if (!isInsideMultiline || language == Languages.C)
            {
                int startPosition;
                int endPosition;
                int currentOffset = 0;
                string currentText = span.GetText();

                do
                {
                    startPosition = span.Start.Position + currentOffset;
                    endPosition = startPosition;

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, language, ref ecase, -1, 0);

                    if (token != null)
                    {
                        endPosition = startPosition + token.Length;

                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            token = tokenizer.Scan(currentText, 0, currentText.Length, language, ref ecase, token.TokenId, token.State);
                            if (ecase == Cases.CEnding)
                            {
                                startPosition += token.StartIndex;
                                endPosition = span.Snapshot.Length;
                            }
                            else if (token != null)
                            {
                                endPosition += token.Length;
                            }
                        }
                        bool multiLineToken = false;

                        IClassificationType classification = null;

                        switch (token.TokenId)
                        {
                            case 0:
                                classification = Classification.WhiteSpace;
                                break;
                            case 1:
                                classification = Classification.Keyword;
                                break;
                            case 2:
                                classification = Classification.Comment;
                                multiLineToken = true;
                                break;
                            case 3:
                                classification = Classification.Comment;
                                break;
                            case 4:
                                classification = Classification.NumberLiteral;
                                break;
                            case 5:
                                classification = Classification.StringLiteral;
                                break;
                            case 6:
                                classification = Classification.ExcludedCode;
                                break;
                            case -1:
                                classification = Classification.Other;
                                break;
                            case -2:
                                multiLineToken = true;
                                break;
                            default:
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (token.TokenId != FlexTokenizer.Classes.C)
                            list.Add(new ClassificationSpan(tokenSpan, classification));

                        if (multiLineToken)
                        {
                            if (!_multiLineTokens.Any(a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
                            {
                                _multiLineTokens.Add(new MultiLineToken()
                                {
                                    Classification = classification,
                                    Version = span.Snapshot.Version,
                                    Tracking = span.Snapshot.CreateTrackingSpan(tokenSpan.Span, SpanTrackingMode.EdgeExclusive),
                                    Language = token.TokenId == FlexTokenizer.Classes.C ? Languages.C : language
                                });

                                if (token.TokenId == FlexTokenizer.Classes.C)
                                {
                                    list = GetClassificationSpans(tokenSpan).ToList();
                                    Invalidate(tokenSpan);
                                    return list;
                                }
                                else if (tokenSpan.End > span.End)
                                {
                                    //Invalidate(new SnapshotSpan(span.End + 1, tokenSpan.End));
                                    return list;
                                }
                            }
                        }
                        currentOffset += token.Length;
                    }
                    if (token == null)
                    {
                        break;
                    }
                } while (currentOffset < currentText.Length);
            }
            return list;
        }

        public MultiLineToken GetLanguageSpan(SnapshotSpan span, Languages language = Languages.Flex)
        {
            var list = new List<ClassificationSpan>();
            bool isInsideMultiline = false;
            Cases ecase = Cases.NoCase;

            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;

            if (!isInsideMultiline)
            {
                int startPosition;
                int endPosition;
                int currentOffset = 0;
                string currentText = span.GetText();

                do
                {
                    startPosition = span.Start.Position + currentOffset;
                    endPosition = startPosition;

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, language, ref ecase, -1, 0);

                    if (token != null)
                    {
                        if (token.State != (int)Cases.FlexDefinitions && token.State != (int)Cases.C)
                        {
                            endPosition = startPosition + token.Length;
                        }

                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            token = tokenizer.Scan(currentText, 0, currentText.Length, language, ref ecase, token.TokenId, token.State);
                            if (ecase == Cases.CEnding)
                            {
                                startPosition += token.StartIndex;
                                endPosition = span.Snapshot.Length;
                            }
                            else if (token != null)
                            {
                                endPosition += token.Length;
                            }
                        }
                        bool multiLineToken = false;
                        if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.FlexDefinitions)
                        {
                            endPosition -= 2;
                        }
                        IClassificationType classification = null;

                        switch (token.TokenId)
                        {
                            case 0:
                                classification = Classification.WhiteSpace;
                                break;
                            case 1:
                                classification = Classification.Keyword;
                                break;
                            case 2:
                                classification = Classification.Comment;
                                multiLineToken = true;
                                break;
                            case 3:
                                classification = Classification.Comment;
                                break;
                            case 4:
                                classification = Classification.NumberLiteral;
                                break;
                            case 5:
                                classification = Classification.StringLiteral;
                                break;
                            case 6:
                                classification = Classification.ExcludedCode;
                                break;
                            case 7:
                                classification = FlexDefinitionType;
                                break;
                            case -1:
                                classification = Classification.Other;
                                break;
                            case -2:
                                multiLineToken = true;
                                break;
                            case -3:
                                multiLineToken = true;
                                break;
                            default:
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (token.TokenId != FlexTokenizer.Classes.C && token.TokenId != FlexTokenizer.Classes.FlexDefinitions)
                            list.Add(new ClassificationSpan(tokenSpan, classification));

                        if (multiLineToken)
                        {
                            if (!_multiLineTokens.Any(a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
                            {
                                 return new MultiLineToken()
                                {
                                    Classification = classification,
                                    Version = span.Snapshot.Version,
                                    Tracking = span.Snapshot.CreateTrackingSpan(tokenSpan.Span, SpanTrackingMode.EdgeExclusive),
                                    Language = GetLanguage(token.TokenId)
                                };

                            }
                        }
                        currentOffset += token.Length;
                    }
                    if (token == null)
                    {
                        break;
                    }
                } while (currentOffset < currentText.Length);
            }
            return null;
        }

        //private void SortTokens(ITextSnapshot span)
        //{
        //    var sortedTokens = new List<MultiLineToken>();
        //    MultiLineToken[] aux = new MultiLineToken[_multiLineTokens.Count];
        //    _multiLineTokens.CopyTo(aux);
        //    List<MultiLineToken> l = aux.ToList();
        //    List<SnapshotPoint> points = new List<SnapshotPoint>();
        //    foreach (var item in l)
        //    {
        //        points.Add(item.Tracking.GetStartPoint(span));
        //    }
        //    for (int i = 0; i < _multiLineTokens.Count; i++)
        //    {
        //        int min = 0;

        //        var token = l.Where(t => t.Tracking);
        //        sortedTokens.Add()
        //    }
        //}

        #endregion
    }
}
