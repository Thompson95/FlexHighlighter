using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace Flex_Highlighter
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "FlexerClassifier" classification type.
    /// </summary>
    internal sealed class FlexClassifier : IClassifier
    {
        /// <summary>
        /// Classification type.
        /// </summary>
        private readonly IClassificationType classificationType;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal FlexClassifier(ITextBuffer buffer, IStandardClassificationService classification, IClassificationTypeRegistryService registry)
        {
            ClassificationRegistry = registry;
            Classification = classification;
            Buffer = buffer;

            tokenizer = new FlexTokenizer(classification);
            this.classificationType = registry.GetClassificationType("FlexerClassifier");
        }

        private readonly FlexTokenizer tokenizer;

        internal ITextBuffer Buffer { get; }
        internal IClassificationTypeRegistryService ClassificationRegistry { get; }
        internal IStandardClassificationService Classification { get; }

        #region IClassifier

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
            ITextSnapshot snapshot = span.Snapshot;
            string text = span.GetText();
            int length = span.Length;
            int index = 0;
            int globalStart = span.Start.Position;
            int globalEnd = snapshot.Length;


            while (index < length)
            {
                int start = index;
                index = tokenizer.AdvanceWord(text, start, out IClassificationType type, globalStart, globalEnd);
                list.Add(new ClassificationSpan(new SnapshotSpan(snapshot, new Span(span.Start + start, index - start)), type));
            }
            return list;
        }

        #endregion
    }
}
