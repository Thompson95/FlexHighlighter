using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Linq;
using System.Diagnostics;

namespace Flex_Highlighter
{
    internal enum Languages
    {
        Undefined,
        C,
        Flex
    }
    internal struct MultiLineToken
    {
        public IClassificationType Classification;
        public ITrackingSpan Tracking;
        public ITextVersion Version;
        public Languages Language;
    }

    public class Token
    {
        public int StartIndex;
        public int Length;
        public int TokenId;
        public int State;
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
        internal readonly IClassificationType Multiline;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal FlexClassifier(ITextBuffer buffer, IStandardClassificationService classification, IClassificationTypeRegistryService registry)
        {
            ClassificationRegistry = registry;
            Classification = classification;
            Buffer = buffer;
            Multiline = registry.CreateClassificationType("Multiline", new IClassificationType[0]);
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

            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;
            Languages language = Languages.Undefined;

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
                        {
                            continue;
                        }
                        isInsideMultiline = true;
                        if (span.Start >= multiSpan.Start) //&& span.End < multiSpan.End)
                        {
                            language = _multiLineTokens[i].Language;
                        }
                        if (span.Snapshot.Version != _multiLineTokens[i].Version)
                        {
                            if (_multiLineTokens[i].Classification != null)
                            {
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                            }
                            _multiLineTokens.RemoveAt(i);
                            Invalidate(multiSpan);
                        }
                        else
                        {
                            if (_multiLineTokens[i].Classification != null)
                            {
                                list.Add(new ClassificationSpan(multiSpan, _multiLineTokens[i].Classification));
                            }

                        }
                    }
                }
            }

            if (!isInsideMultiline || language != Languages.Undefined)
            {
                int startPosition;
                int endPosition;
                int currentOffset = 0;
                string currentText = span.GetText();

                do
                {
                    startPosition = span.Start.Position + currentOffset;
                    endPosition = startPosition;

                    var token = tokenizer.Scan(currentText, currentOffset, currentText.Length, language, -1, 0);

                    if (token != null)
                    {
                        if (token.State == (int)FlexTokenizer.Cases.Flex)
                            startPosition += 2;
                        endPosition = startPosition + token.Length;

                        while (token != null && token.State != 0 && endPosition < span.Snapshot.Length)
                        {
                            int textSize = Math.Min(span.Snapshot.Length - endPosition, 1024);
                            currentText = span.Snapshot.GetText(endPosition, textSize);
                            if(textSize == 0)
                            {
                                token.State = 0;
                                break;
                            }
                            token = tokenizer.Scan(currentText, 0, currentText.Length, language, token.TokenId, token.State);
                            if (token != null)
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
                            case 7:
                                classification = Classification.Identifier;
                                break;
                            case -1:
                                classification = Classification.Other;
                                break;
                            default:
                                multiLineToken = true;
                                break;
                        }

                        var tokenSpan = new SnapshotSpan(span.Snapshot, startPosition, (endPosition - startPosition));
                        if (token.TokenId != FlexTokenizer.Classes.C && token.TokenId != FlexTokenizer.Classes.Flex)
                        {
                            list.Add(new ClassificationSpan(tokenSpan, classification));
                        }


                        if (multiLineToken)
                        {
                            if (!_multiLineTokens.Any( a => a.Tracking.GetSpan(span.Snapshot).Span == tokenSpan.Span))
                            {
                                _multiLineTokens.Add(new MultiLineToken()
                                {
                                    Classification = classification,
                                    Version = span.Snapshot.Version,
                                    Tracking = span.Snapshot.CreateTrackingSpan(tokenSpan.Span, SpanTrackingMode.EdgeExclusive),
                                    Language = token.TokenId ==   FlexTokenizer.Classes.C ? Languages.C : 
                                                                (token.TokenId == FlexTokenizer.Classes.Flex ? Languages.Flex : Languages.Undefined)
                                });
                                if (token.TokenId == FlexTokenizer.Classes.C || token.TokenId == FlexTokenizer.Classes.Flex)
                                {
                                    Invalidate(new SnapshotSpan(tokenSpan.Start, tokenSpan.End));
                                    //list = GetClassificationSpans(tokenSpan).ToList();
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

            //while (index < length)
            //{
            //    int start = index;
            //    index = tokenizer.AdvanceWord(text, start, out IClassificationType type, globalStart, globalEnd);
            //    list.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, index - start)), type));
            //}

            //tokenizer.Tokenize(ref list, span);
            return list;
        }

        #endregion
    }
}
