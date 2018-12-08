using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Flex_Highlighter
{
    /// <summary>
    /// Defines an editor format for the FlexerClassifier type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Flex Definition")]
    [Name("Flex Definition")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class FlexClassifierFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public FlexClassifierFormat()
        {
            this.DisplayName = "Flex Definition"; // Human readable version of the name
            this.ForegroundColor = Color.FromRgb(189, 99, 197);
        }
    }
}
