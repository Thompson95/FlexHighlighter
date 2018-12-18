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

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Special Character")]
    [Name("Regex Special Character")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexSpecialCharacter : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexSpecialCharacter()
        {
            this.DisplayName = "Regex Special Character"; // Human readable version of the name
            this.ForegroundColor = Colors.BurlyWood;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Letters")]
    [Name("Regex Letters")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexLetters : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexLetters()
        {
            this.DisplayName = "Regex Letters"; // Human readable version of the name
            this.ForegroundColor = Colors.LightSeaGreen;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Digits in Character Set")]
    [Name("Regex Digits in Character Set")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexDigitsInSet : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexDigitsInSet()
        {
            this.DisplayName = "Regex Digits in Character Set"; // Human readable version of the name
            this.ForegroundColor = Colors.Aqua;
            this.BackgroundColor = Color.FromRgb(123, 121, 85);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Letters in Group")]
    [Name("Regex Letters in Group")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexLettersInGroup : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexLettersInGroup()
        {
            this.DisplayName = "Regex Letters in Group"; // Human readable version of the name
            this.ForegroundColor = Color.FromRgb(189, 99, 197);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Escaped Character")]
    [Name("Regex Escaped Character")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.Default, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexEscapedCharacter : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexEscapedCharacter()
        {
            this.DisplayName = "Regex Escaped Character"; // Human readable version of the name
            this.ForegroundColor = Colors.Red;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Quantifier")]
    [Name("Regex Quantifier")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.High, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexQuantifier : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexQuantifier()
        {
            this.DisplayName = "Regex Quantifier"; // Human readable version of the name
            this.ForegroundColor = Colors.LightSkyBlue;
            this.BackgroundColor = Color.FromRgb((byte)(Colors.LightSkyBlue.R / 2), (byte)(Colors.LightSkyBlue.G / 2), (byte)(Colors.LightSkyBlue.B / 2));
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Group")]
    [Name("Regex Group")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.High, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexGroup : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexGroup()
        {
            this.DisplayName = "Regex Group"; // Human readable version of the name
            this.ForegroundColor = Color.FromRgb(100, 220, 2);

            this.BackgroundColor = Color.FromRgb((byte)(Colors.LightGreen.R / 2), (byte)(Colors.LightGreen.G / 2), (byte)(Colors.LightGreen.B / 2));
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Escaped Character in Regex Group")]
    [Name("Escaped Character in Regex Group")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.High, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class EscapedCharacterInRegexGroup : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public EscapedCharacterInRegexGroup()
        {
            this.DisplayName = "Escaped Character in Regex Group"; // Human readable version of the name
            this.ForegroundColor = Colors.Red;
            this.BackgroundColor = Color.FromRgb((byte)(Colors.LightGreen.R / 2), (byte)(Colors.LightGreen.G / 2), (byte)(Colors.LightGreen.B / 2));
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Escaped Character in Character Set")]
    [Name("Escaped Character in Character Set")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.High, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class EscapedCharacterInCharacterSet : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public EscapedCharacterInCharacterSet()
        {
            this.DisplayName = "Escaped Character in Character Set"; // Human readable version of the name
            this.ForegroundColor = Colors.Red;
            this.BackgroundColor = Color.FromRgb(123, 121, 85);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Regex Character Set")]
    [Name("Regex Character Set")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(After = Priority.High, Before = Priority.High)] // Set the priority to be after the default classifiers
    internal sealed class RegexCharacterSet : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlexClassifierFormat"/> class.
        /// </summary>
        public RegexCharacterSet()
        {
            this.DisplayName = "Regex Character Set"; // Human readable version of the name
            this.ForegroundColor = Color.FromRgb(255, 170, 0);
            this.BackgroundColor = Color.FromRgb(123, 121, 85);
        }
    }
}
