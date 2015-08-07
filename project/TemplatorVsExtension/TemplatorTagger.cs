using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace Templator.Utils
{
    public class TemplatorTagger : ITagger<ClassificationTag>
    {
        private readonly ClassificationProcessor _classifier;

#pragma warning disable 67
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

        internal TemplatorTagger(IClassificationTypeRegistryService registry, DTE dte)
        {
            _classifier = new ClassificationProcessor(registry, dte, true);
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _classifier.GetTags(spans);
        }

    }
}
