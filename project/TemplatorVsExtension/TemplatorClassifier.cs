using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Templator.Utils
{
    public class TemplatorClassifier : IClassifier
    {
        private readonly ClassificationProcessor _classifier;

        internal TemplatorClassifier(IClassificationTypeRegistryService registry, DTE dte)
        {
            _classifier = new ClassificationProcessor(registry, dte);
        }

        #pragma warning disable 67
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return _classifier.GetClassificationSpans(span);
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        #pragma warning restore 67
    }
}
