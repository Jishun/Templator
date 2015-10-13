using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Templator.Utils
{

    internal static class ClassificationDefinitions
    {
        internal const string
            ClasificationName = "Templator.Template",
            TextHolderKeyword = "TextHolder.Keyword",
            TextHolderDefault = "TextHolder.Default",
            TextHolderBrace = "TextHolder.Brace",
            TextHolderFault = "TextHolder.Fault",
            TextHolderCategory = "TextHolder.Category",
            TextHolderParam = "TextHolder.Param",
            TextHolderParamBrace = "TextHolder.ParamBrace",
            TextHolderDescBrace = "TextHolder.DescBrace",
            TextHolderCategoryBrace = "TextHolder.CategoryBrace",
            TextHolderRecognized = "TextHolder.Recognized";

        #region Content Type and File Extension Definitions

        [Export]
        [Name(ClasificationName)]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition diffContentTypeDefinition = null;

        [Export]
        [FileExtension(".txt")]
        [ContentType(ClasificationName)]
        internal static FileExtensionToContentTypeDefinition txtFileExtensionDefinition = null;

        [Export]
        [FileExtension(".csv")]
        [ContentType(ClasificationName)]
        internal static FileExtensionToContentTypeDefinition csvFileExtensionDefinition = null;

        [Export]
        [FileExtension(".xml")]
        [BaseDefinition("XML")]
        [ContentType(ClasificationName)]
        internal static FileExtensionToContentTypeDefinition xmlFileExtensionDefinition = null;

        #endregion

        #region Classification Type Definitions
        [Export]
        [Name(TextHolderKeyword)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition NameDefinition = null;
        [Export]
        [Name(TextHolderDefault)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition DefaultDefinition = null;
        [Export]
        [Name(TextHolderBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition BraceDefinition = null;
        [Export]
        [Name(TextHolderCategory)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition CategoryDefinition = null;
        [Export]
        [Name(TextHolderParam)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition ParamDefinition = null;
        [Export]
        [Name(TextHolderParamBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition ParamBraceDefinition = null;
        [Export]
        [Name(TextHolderDescBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition DescBraceDefinition = null;
        [Export]
        [Name(TextHolderCategoryBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition CategoryBraceDefinition = null;
        [Export]
        [Name(TextHolderFault)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition FaultDefinition = null;
        [Export]
        [Name(TextHolderRecognized)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition RecognizedDefinition = null;

        #endregion

        #region Classification Format Productions

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderBrace)]
        [Name(TextHolderBrace)]
        internal sealed class TextHolderStartFormat : ClassificationFormatDefinition
        {
            public TextHolderStartFormat()
            {
                this.ForegroundColor = Colors.DarkGoldenrod;
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderParamBrace)]
        [Name(TextHolderParamBrace)]
        internal sealed class TextHolderParamBraceFormat : ClassificationFormatDefinition
        {
            public TextHolderParamBraceFormat()
            {
                this.ForegroundColor = Colors.LightSalmon;
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderCategory)]
        [Name(TextHolderCategory)]
        internal sealed class TextHolderTypeFormat : ClassificationFormatDefinition
        {
            public TextHolderTypeFormat()
            {
                this.ForegroundColor = Colors.Blue;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderParam)]
        [Name(TextHolderParam)]
        internal sealed class TextHolderParamFormat : ClassificationFormatDefinition
        {
            public TextHolderParamFormat()
            {
                this.ForegroundColor = Colors.DarkCyan;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderFault)]
        [Name(TextHolderFault)]
        internal sealed class TextHolderFaultFormat : ClassificationFormatDefinition
        {
            public TextHolderFaultFormat()
            {
                var c = new TextDecorationCollection();
                var s = new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Pen = new Pen(Brushes.Red, 2),
                    PenThicknessUnit = TextDecorationUnit.FontRecommended,
                };
                c.Add(s);
                this.TextDecorations = c;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderDescBrace)]
        [Name(TextHolderDescBrace)]
        internal sealed class TextHolderDescBraceFormat : ClassificationFormatDefinition
        {
            public TextHolderDescBraceFormat()
            {
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderCategoryBrace)]
        [Name(TextHolderCategoryBrace)]
        internal sealed class TextHolderTypeBraceFormat : ClassificationFormatDefinition
        {
            public TextHolderTypeBraceFormat()
            {
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderRecognized)]
        [Name(TextHolderRecognized)]
        internal sealed class TextHolderRecognizedFormat : ClassificationFormatDefinition
        {
            public TextHolderRecognizedFormat()
            {
                this.ForegroundColor = Colors.DimGray;
                this.TextDecorations = System.Windows.TextDecorations.Underline;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderKeyword)]
        [Name(TextHolderKeyword)]
        internal sealed class TextHolderNameDefaultFormat : ClassificationFormatDefinition
        {
            public TextHolderNameDefaultFormat()
            {
                this.ForegroundColor = Colors.Brown;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextHolderDefault)]
        [Name(TextHolderDefault)]
        internal sealed class TextHolderDefaultFormat : ClassificationFormatDefinition
        {
            public TextHolderDefaultFormat()
            {
                this.ForegroundColor = Colors.DimGray;
            }
        }
        #endregion
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType(ClassificationDefinitions.ClasificationName)]
    internal class ClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry;
        [Import]
        internal SVsServiceProvider ServiceProvider;

        private static TemplatorClassifier _diffClassifier;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return _diffClassifier ?? (_diffClassifier = new TemplatorClassifier(ClassificationRegistry, (DTE)ServiceProvider.GetService(typeof(DTE))));
        }
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(ClassificationTag))]
    public class TextTemplateTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry;
        [Import]
        internal SVsServiceProvider ServiceProvider;

        private static TemplatorTagger _instance;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return (_instance ??( _instance = new TemplatorTagger(ClassificationRegistry,(DTE)ServiceProvider.GetService(typeof(DTE))))) as ITagger<T>;
        }
    }
}
