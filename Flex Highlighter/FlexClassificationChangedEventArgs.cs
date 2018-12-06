using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flex_Highlighter
{
    internal class FlexClassificationChangedEventArgs : ClassificationChangedEventArgs
    {
        internal Languages language;
        public FlexClassificationChangedEventArgs(SnapshotSpan span) : base(span)
        {
        }
        public FlexClassificationChangedEventArgs(SnapshotSpan span, Languages l) : base(span)
        {
            language = l;
        }
    }
}
