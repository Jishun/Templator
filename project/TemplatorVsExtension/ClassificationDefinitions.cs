using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Templator.Utils
{

    internal static class ClassificationDefinitions
    {
        internal const string
            ClasificationName = "TextTemplate",
            TextTemplateFieldName = "TextTemplateField.Name",
            TextTemplateFieldDefault = "TextTemplateField.Default",
            TextTemplateFieldBrace = "TextTemplateField.Brace",
            TextTemplateFieldFault = "TextTemplateField.Fault",
            TextTemplateFieldType = "TextTemplateField.Type",
            TextTemplateFieldParam = "TextTemplateField.Param",
            TextTemplateFieldParamBrace = "TextTemplateField.ParamBrace",
            TextTemplateFieldDescBrace = "TextTemplateField.DescBrace",
            TextTemplateFieldTypeBrace = "TextTemplateField.TypeBrace",
            TextTemplateFieldRecognized = "TextTemplateField.Recognized";

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
        [Name(TextTemplateFieldName)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition NameDefinition = null;
        [Export]
        [Name(TextTemplateFieldDefault)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition DefaultDefinition = null;
        [Export]
        [Name(TextTemplateFieldBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition BraceDefinition = null;
        [Export]
        [Name(TextTemplateFieldType)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition TypeDefinition = null;
        [Export]
        [Name(TextTemplateFieldParam)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition ParamDefinition = null;
        [Export]
        [Name(TextTemplateFieldParamBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition ParamBraceDefinition = null;
        [Export]
        [Name(TextTemplateFieldDescBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition DescBraceDefinition = null;
        [Export]
        [Name(TextTemplateFieldTypeBrace)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition TypeBraceDefinition = null;
        [Export]
        [Name(TextTemplateFieldFault)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition FaultDefinition = null;
        [Export]
        [Name(TextTemplateFieldRecognized)]
        [BaseDefinition(ClasificationName)]
        internal static ClassificationTypeDefinition RecognizedDefinition = null;

        #endregion

        #region Classification Format Productions

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldBrace)]
        [Name(TextTemplateFieldBrace)]
        internal sealed class TextTemplateFieldStartFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldStartFormat()
            {
                this.ForegroundColor = Colors.DarkGoldenrod;
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldParamBrace)]
        [Name(TextTemplateFieldParamBrace)]
        internal sealed class TextTemplateFieldParamBraceFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldParamBraceFormat()
            {
                this.ForegroundColor = Colors.LightSalmon;
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldType)]
        [Name(TextTemplateFieldType)]
        internal sealed class TextTemplateFieldTypeFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldTypeFormat()
            {
                this.ForegroundColor = Colors.Blue;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldParam)]
        [Name(TextTemplateFieldParam)]
        internal sealed class TextTemplateFieldParamFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldParamFormat()
            {
                this.ForegroundColor = Colors.DarkCyan;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldFault)]
        [Name(TextTemplateFieldFault)]
        internal sealed class TextTemplateFieldFaultFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldFaultFormat()
            {
                this.BackgroundColor = Colors.Red;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldDescBrace)]
        [Name(TextTemplateFieldDescBrace)]
        internal sealed class TextTemplateFieldDescBraceFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldDescBraceFormat()
            {
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldTypeBrace)]
        [Name(TextTemplateFieldTypeBrace)]
        internal sealed class TextTemplateFieldTypeBraceFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldTypeBraceFormat()
            {
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldRecognized)]
        [Name(TextTemplateFieldRecognized)]
        internal sealed class TextTemplateFieldRecognizedFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldRecognizedFormat()
            {
                this.ForegroundColor = Colors.DimGray;
                this.TextDecorations = System.Windows.TextDecorations.Underline;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldName)]
        [Name(TextTemplateFieldName)]
        internal sealed class TextTemplateFieldNameDefaultFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldNameDefaultFormat()
            {
                this.ForegroundColor = Colors.Brown;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TextTemplateFieldDefault)]
        [Name(TextTemplateFieldDefault)]
        internal sealed class TextTemplateFieldDefaultFormat : ClassificationFormatDefinition
        {
            public TextTemplateFieldDefaultFormat()
            {
                this.ForegroundColor = Colors.DimGray;
            }
        }
        #endregion
    }

    [Export(typeof(IClassifierProvider))]
    [ContentType("TextTemplate")]
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
