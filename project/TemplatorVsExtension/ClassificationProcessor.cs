using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Xml.Linq;
using DotNetUtils;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace Templator.Utils
{
    public class ClassificationProcessor
    {
        private const string TemplatorConfigFileName = "TemplatorConfig.xml";

        private readonly IClassificationTypeRegistryService _classificationTypeRegistry;
        private readonly DTE _dte;
        private readonly bool _isXml;
        private static readonly object LockObject = new object();

        private int _lastPosition = 0;
        private string _activeProjectName;
        private bool _buildDefaultConfig = false;
        private TemplatorParser _parser;
        private TemplatorConfig _defaultConfig;
        private readonly IDictionary<string, TemplatorParser> _parsers = new ConcurrentDictionary<string, TemplatorParser>();

        private static HashSet<string> _standardFields;
        private ITextSnapshot _snapshot;
        private List<ClassificationSpan> _spans;

        private readonly IClassificationType _brace;
        private readonly IClassificationType _name;
        private readonly IClassificationType _default;
        private readonly IClassificationType _fault;
        private readonly IClassificationType _type;
        private readonly IClassificationType _param;
        private readonly IClassificationType _paramBrace;
        private readonly IClassificationType _descBrace;
        private readonly IClassificationType _typeBrace;
        private readonly IClassificationType _recognized;

        public ClassificationProcessor(IClassificationTypeRegistryService registry, DTE dte, bool isXml = false)
        {
            _classificationTypeRegistry = registry;
            _dte = dte;
            _isXml = isXml;
            _brace = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldBrace);
            _name = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldName);
            _default = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldDefault);
            _fault = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldFault);
            _type = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldType);
            _param = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldParam);
            _paramBrace = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldParamBrace);
            _descBrace = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldDescBrace);
            _typeBrace = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldTypeBrace);
            _recognized = _classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextTemplateFieldRecognized);
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }
            foreach (var classificationSpan in spans.SelectMany(GetClassificationSpans))
            {
                yield return new TagSpan<ClassificationTag>(classificationSpan.Span, new ClassificationTag(classificationSpan.ClassificationType));
            }
        }

        private void BuildReference()
        {
            var project = _dte.ActiveDocument.ProjectItem.ContainingProject;
            if (project != null && _activeProjectName != project.FullName && !_parsers.ContainsKey(project.FullName))
            {
                _activeProjectName = project.FullName;
                var doc = project.ProjectItems.Item(TemplatorConfigFileName);
                var config = TryGeTemplatorConfig(doc);
                if (config != null)
                {
                    config.ContinueOnError = true;
                    config.OnTokenFound = OnTemplatorTokenFound;
                    _parsers.Add(_activeProjectName, new TemplatorParser(config));
                }
            }
            else if (project == null)
            {
                _activeProjectName = null;
            }
            lock (LockObject)
            {
                _parser = _parsers.GetOrDefault(_activeProjectName);
            }
            TemplatorConfig defaultConfig = null;
            if (_buildDefaultConfig)
            {
                _buildDefaultConfig = false;
                if (_dte != null && _dte.Solution != null)
                {
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemAdded -= SolutionItemsEvents_ItemChanged;
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemAdded += SolutionItemsEvents_ItemChanged;
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemRemoved -= SolutionItemsEvents_ItemChanged;
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemRemoved += SolutionItemsEvents_ItemChanged;
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemRenamed -= SolutionItemsEventsOnItemRenamed;
                    _dte.Solution.DTE.Events.SolutionItemsEvents.ItemRenamed += SolutionItemsEventsOnItemRenamed;
                    var projectItem = _dte.Solution.FindProjectItem(TemplatorConfigFileName);
                    if (projectItem != null && projectItem.FileCount > 0)
                    {
                        projectItem.DTE.Events.DocumentEvents.DocumentSaved -= Document_Changed;
                        projectItem.DTE.Events.DocumentEvents.DocumentSaved += Document_Changed;
                        defaultConfig = TryGeTemplatorConfig(projectItem);
                    }
                }
                else
                {
                    defaultConfig = TemplatorConfig.DefaultInstance;
                }
                if (defaultConfig != null)
                {
                    defaultConfig.OnTokenFound = OnTemplatorTokenFound;
                    lock (LockObject)
                    {
                        _defaultConfig = defaultConfig;
                    }
                }
            }
        }

        private TemplatorConfig TryGeTemplatorConfig(ProjectItem item)
        {
            if (item != null && item.FileCount > 0)
            {
                try
                {
                    return XDocument.Load(item.FileNames[1]).Root.FromXElement<TemplatorConfig>();
                }
                catch (Exception e)
                {

                }
            }
            return null;
        }

        private void OnTemplatorTokenFound(object sender, TemplatorSyntaxEventArgs args)
        {
            IClassificationType type = _default;
            if (args.HasError)
            {
                if (args.Position > _lastPosition)
                {
                    AddSpan(args.Position - _lastPosition, _fault, args.TokenText);
                }
            }
            if (args.TokenText.IsNullOrEmpty())
            {
                return;
            }
            //_recognized;
            if (args.TokenName == _parser.Config.Begin)
            {
                type = _brace;
            }
            else if (args.TokenName == _parser.Config.TermName)
            {
                type = _name;
            }
            else if (args.TokenName == _parser.Config.TermCategory)
            {
                type = _type;
            }
            else if (args.TokenName == _parser.Config.TermParam)
            {
                type = _param;
            }
            else if (args.TokenName == _parser.Config.ParamEnd)
            {
                type = _paramBrace;
            }
            else if (args.TokenName == _parser.Config.KeywordsBegin)
            {
                type = _descBrace;
            }
            else if (args.TokenName == _parser.Config.CategorizedNameBegin)
            {
                type = _typeBrace;
            }
            AddSpan(args.TokenText.Length, type);
        }

        private bool OnProjectItemChanged(ProjectItem item)
        {
            if (item.ContainingProject != null)
            {
                _activeProjectName = null;
                if (_parsers.ContainsKey(item.ContainingProject.FullName))
                {
                    _parsers.Remove(item.ContainingProject.FullName);
                }
                return true;
            }
            return false;
        }

        private void Document_Changed(Document document)
        {
            if (!OnProjectItemChanged(document.ProjectItem))
            {
                _buildDefaultConfig = true;
            }
        }

        private void SolutionItemsEventsOnItemRenamed(ProjectItem projectItem, string oldName)
        {
            if (!OnProjectItemChanged(projectItem))
            {
                if (oldName.EndsWith(TemplatorConfigFileName) ||
                    (projectItem.FileCount > 0 && projectItem.FileNames[1].EndsWith(TemplatorConfigFileName)))
                {
                    _buildDefaultConfig = true;
                }
            }
        }

        private void SolutionItemsEvents_ItemChanged(ProjectItem projectItem)
        {
            if (!OnProjectItemChanged(projectItem))
            {
                if (projectItem.FileCount > 0 && projectItem.FileNames[1].EndsWith(TemplatorConfigFileName))
                {
                    _buildDefaultConfig = true;
                }
            }
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            _snapshot = span.Snapshot;
            _spans = new List<ClassificationSpan>();
            BuildReference();
            if (_snapshot.Length == 0)
                return _spans;

            if (_parser != null)
            {
                _parser.StartOver(true);
                _parser.ParseText(span.GetText(), null);
            }
            return _spans;
        }

        private void AddSpan(int length, IClassificationType classification, string backward = null)
        {
            if (backward != null)
            {
                length -= backward.Length;
            }
            if (classification != null && length > 0)
            {
                _spans.Add(new ClassificationSpan(new SnapshotSpan(_snapshot, _lastPosition, length), classification));
                _lastPosition += length;
            }
        }

    }
}
