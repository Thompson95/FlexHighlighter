using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Linq;
using System.Diagnostics;

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
        FlexDefinitions = 10,
        CMacro = 11,
        FlexRules = 12
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
        FlexDefinitions,
        NoLanguage,
        CEnding
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
        internal readonly IClassificationType FlexDefinitionSection;
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
            FlexDefinitionSection = registry.CreateClassificationType("FlexDefinitionSection", new IClassificationType[0]);
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
            Languages language = Languages.FlexDefinitions;
            Languages auxLanguage = Languages.FlexDefinitions;
            Cases ecase = Cases.NoCase;

            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;

            List<Tuple<Languages, int>> sectionDistances = new List<Tuple<Languages, int>>();
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
                        //if (span.Start == multiSpan.End)
                        //    continue;
                        isInsideMultiline = true;
                        if (span.Snapshot.Version != _multiLineTokens[i].Version)
                        {
                            if (_multiLineTokens[i].Classification != null)
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                            auxLanguage = _multiLineTokens[i].Language;
                            MultiLineToken mlt = null;
                            if (auxLanguage == Languages.FlexDefinitions)
                                mlt = HandleFlexDefinitions(span);
                            else if (auxLanguage == Languages.Flex)
                                mlt = GetLanguageSpan(multiSpan, Languages.FlexDefinitions);
                            else if (auxLanguage == Languages.CEnding)
                                mlt = GetLanguageSpan(multiSpan, Languages.Flex);
                            else
                                mlt = GetLanguageSpan(multiSpan);

                            if (mlt != null)
                            {
                                if (multiSpan.Start == mlt.Tracking.GetStartPoint(snapshot) && multiSpan.End == mlt.Tracking.GetEndPoint(snapshot))
                                {
                                    sectionDistances.Add(new Tuple<Languages, int>(auxLanguage, span.Start - multiSpan.Start));
                                    continue;
                                }
                                _multiLineTokens.RemoveAt(i);
                                ClearTokenIntersections(mlt.Tracking.GetSpan(snapshot), snapshot);
                                i = _multiLineTokens.Count();
                                _multiLineTokens.Add(mlt);
                                Invalidate(mlt.Tracking.GetSpan(span.Snapshot));
                                sectionDistances.Add(new Tuple<Languages, int>(auxLanguage, span.Start - multiSpan.Start));
                            }
                            else
                            {
                                _multiLineTokens.RemoveAt(i);
                                //Invalidate(multiSpan);
                            }
                        }
                        else
                        {
                            auxLanguage = _multiLineTokens[i].Language;
                            if (_multiLineTokens[i].Classification != null)
                            {
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                                return list;
                            }
                            sectionDistances.Add(new Tuple<Languages, int>(auxLanguage, span.Start - multiSpan.Start));
                        }
                    }
                }
            }
            if (_multiLineTokens.Count == 0)
            {
                var mlt = HandleFlexDefinitions(span);
                if (mlt != null)
                    _multiLineTokens.Add(mlt);
            }
            if (sectionDistances.Where(s => s.Item2 >= 0).Count() > 0)
            {
                language = sectionDistances.Where(s => s.Item2 >= 0).OrderBy(s => s.Item2).FirstOrDefault().Item1;
            }
            Debug.WriteLine("=============================");
            foreach (var item in _multiLineTokens)
            {
                Debug.WriteLine($"{item.Language}\t({item.Tracking.GetStartPoint(snapshot).Position}, {item.Tracking.GetEndPoint(snapshot).Position})");
            }

            //if (!isInsideMultiline || language == Languages.C || language == Languages.Flex)
            {
                int startPosition;
                int endPosition;
                int currentOffset = 0;
                string currentText = span.GetText();

                do
                {
                    startPosition = span.Start.Position + currentOffset;
                    endPosition = startPosition;

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, ref language, ref ecase, -1, 0);

                    if (token != null)
                    {
                        if (language == Languages.Flex && _multiLineTokens.Where(t => t.Tracking.GetStartPoint(snapshot).Position == startPosition && t.Language == Languages.Flex).Any())
                        {
                            token.State = 0;
                            token.TokenId = FlexTokenizer.Classes.Other;
                        }
                        if (token.State != (int)Cases.FlexDefinitions && token.State != (int)Cases.FlexRules && token.State != (int)Cases.C && token.State != (int)Cases.CEnding)
                        {
                            endPosition = startPosition + token.Length;
                        }
                        if (ecase == Cases.CEnding)
                        {
                            startPosition += token.StartIndex;
                            endPosition = span.Snapshot.Length;
                        }
                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            token = tokenizer.Scan(currentText, 0, currentText.Length, ref language, ref ecase, token.TokenId, token.State);
                            if (token != null)
                            {
                                endPosition += token.Length;
                            }
                        }
                        bool multiLineToken = false;
                        if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.FlexDefinitions || token.TokenId == FlexTokenizer.Classes.CEnding)
                        {
                            if (endPosition < snapshot.Length)
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
                                //classification = CSection;
                                multiLineToken = true;
                                break;
                            case -3:
                                //classification = FlexDefinitionSection;
                                multiLineToken = true;
                                break;
                            case -4:
                                //classification = CSection;
                                multiLineToken = true;
                                break;
                            case -5:
                                //classification = FlexSection;
                                multiLineToken = true;
                                break;
                            case -6:
                                //classification = CEnding;
                                multiLineToken = true;
                                break;
                            default:
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (classification != null)
                            list.Add(new ClassificationSpan(tokenSpan, classification));

                        if (multiLineToken)
                        {
                            if (!_multiLineTokens.Any(a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
                            {
                                ClearTokenIntersections(tokenSpan, snapshot);
                                _multiLineTokens.Add(new MultiLineToken()
                                {
                                    Classification = classification,
                                    Version = span.Snapshot.Version,
                                    Tracking = span.Snapshot.CreateTrackingSpan(tokenSpan.Span, SpanTrackingMode.EdgeExclusive),
                                    Language = GetLanguage(token.TokenId)
                                });

                                if (token.TokenId < FlexTokenizer.Classes.Other)
                                {
                                    //list = GetClassificationSpansForLanguage(span, Languages.C).ToList();
                                    if (token.TokenId == FlexTokenizer.Classes.FlexRules)
                                    {
                                        //AddInnerCSections(tokenSpan);
                                    }
                                    var auxSpan = new SnapshotSpan(tokenSpan.Start, tokenSpan.End.Add(tokenSpan.End > snapshot.Length - 2 ? 0 : 2));
                                    Invalidate(auxSpan);
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

        private MultiLineToken HandleFlexDefinitions(SnapshotSpan span)
        {

            var mlt = GetLanguageSpan(new SnapshotSpan(span.Snapshot, new Span(0, span.Snapshot.Length)), Languages.NoLanguage);
            return mlt;
        }

        public MultiLineToken GetLanguageSpan(SnapshotSpan span, Languages l = Languages.FlexDefinitions)
        {
            var list = new List<ClassificationSpan>();
            bool isInsideMultiline = false;
            Cases ecase = Cases.NoCase;
            Languages language = l;
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

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, ref language, ref ecase, -1, 0);

                    if (token != null)
                    {
                        if (language == Languages.Flex && _multiLineTokens.Where(t => t.Tracking.GetStartPoint(snapshot).Position == startPosition && t.Language == Languages.Flex).Any())
                        {
                            token.State = 0;
                            token.TokenId = FlexTokenizer.Classes.Other;
                        }
                        if (token.State != (int)Cases.FlexDefinitions && token.State != (int)Cases.C && token.State != (int)Cases.FlexRules && token.State != (int)Cases.CEnding)
                        {
                            endPosition = startPosition + token.Length;
                        }
                        if (ecase == Cases.CEnding)
                        {
                            startPosition += token.StartIndex;
                            endPosition = span.Snapshot.Length;
                        }
                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            token = tokenizer.Scan(currentText, 0, currentText.Length, ref language, ref ecase, token.TokenId, token.State);

                            if (token != null)
                            {
                                endPosition += token.Length;
                            }
                        }
                        bool multiLineToken = false;
                        if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.FlexDefinitions /*|| token.TokenId == FlexTokenizer.Classes.FlexRules*/ || token.TokenId == FlexTokenizer.Classes.CEnding)
                        {
                            if (endPosition < snapshot.Length)
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
                            case -4:
                                multiLineToken = true;
                                break;
                            case -5:
                                multiLineToken = true;
                                break;
                            case -6:
                                multiLineToken = true;
                                break;
                            default:
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (token.TokenId >= FlexTokenizer.Classes.Other)
                            list.Add(new ClassificationSpan(tokenSpan, classification));

                        if (multiLineToken)
                        {
                            //if (!_multiLineTokens.Any(a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
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

        private void ClearTokenIntersections(SnapshotSpan mltSpan, ITextSnapshot snapshot)
        {
            MultiLineToken[] aux = new MultiLineToken[_multiLineTokens.Count];
            _multiLineTokens.CopyTo(aux);
            List<MultiLineToken> l = aux.ToList();

            foreach (var item in l)
            {
                if (mltSpan.Start <= item.Tracking.GetStartPoint(snapshot) && mltSpan.End >= item.Tracking.GetEndPoint(snapshot) && item.Classification == null)
                {
                    _multiLineTokens.Remove(item);
                }
            }
        }
        private Languages GetLanguage(int value)
        {
            switch (value)
            {
                case -2:
                    return Languages.C;
                case -3:
                    return Languages.FlexDefinitions;
                case -4:
                    return Languages.C;
                case -5:
                    return Languages.Flex;
                case -6:
                    return Languages.CEnding;
                default:
                    return Languages.FlexDefinitions;
            }
        }

        #endregion
    }
}
