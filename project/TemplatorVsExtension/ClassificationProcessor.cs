using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        private readonly DTE _dte;
        private readonly bool _isXml;
        private static readonly object LockObject = new object();

        private ProjectItemsEvents _solutionEvents;
        private readonly IDictionary<string, DocumentEvents> _documentEvents = new Dictionary<string, DocumentEvents>();
        private int _lastPosition = 0;
        private int _start = 0;
        private string _activeProjectName;
        private bool _buildDefaultConfig = true;
        private TemplatorParser _parser;
        private readonly IDictionary<string, TemplatorParser> _parsers = new ConcurrentDictionary<string, TemplatorParser>();

        private ITextSnapshot _snapshot;
        private List<ClassificationSpan> _spans;

        private readonly IClassificationType _brace;
        private readonly IClassificationType _keyword;
        private readonly IClassificationType _default;
        private readonly IClassificationType _fault;
        private readonly IClassificationType _category;
        private readonly IClassificationType _param;
        private readonly IClassificationType _paramBrace;
        private readonly IClassificationType _descBrace;
        private readonly IClassificationType _categoryBrace;
        private readonly IClassificationType _recognized;

        public ClassificationProcessor(IClassificationTypeRegistryService registry, DTE dte, bool isXml = false)
        {
            var classificationTypeRegistry = registry;
            _dte = dte;
            _isXml = isXml;
            _brace = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderBrace);
            _keyword = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderKeyword);
            _default = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderDefault);
            _fault = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderFault);
            _category = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderCategory);
            _param = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderParam);
            _paramBrace = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderParamBrace);
            _descBrace = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderDescBrace);
            _categoryBrace = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderCategoryBrace);
            _recognized = classificationTypeRegistry.GetClassificationType(ClassificationDefinitions.TextHolderRecognized);
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

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            _snapshot = span.Snapshot;
            _spans = new List<ClassificationSpan>();
            _lastPosition = 0;
            _start = span.Start.Position;
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

        private void BuildReference()
        {
            var project = _dte.ActiveDocument.ProjectItem.ContainingProject;
            string projectName = null;
            if (project != null)
            {
                if (_activeProjectName != project.FullName)
                {
                    if (!_parsers.ContainsKey(project.FullName))
                    {
                        ProjectItem doc = null;
                        for (var i = 1; i <= project.ProjectItems.Count; i++)
                        {
                            doc = project.ProjectItems.Item(i);
                            if (doc.Name == TemplatorConfigFileName)
                            {
                                break;
                            }
                            doc = null;
                        }
                        if (doc != null)
                        {
                            if (_documentEvents.ContainsKey(project.FullName))
                            {
                                _documentEvents[project.FullName].DocumentSaved -= Document_Changed;
                                _documentEvents.Remove(project.FullName);
                            }
                            var de = doc.DTE.Events.DocumentEvents;
                            _documentEvents.AddOrOverwrite(project.FullName, de);
                            de.DocumentSaved -= Document_Changed;
                            de.DocumentSaved += Document_Changed;
                            var config = TryGeTemplatorConfig(doc);
                            if (config != null)
                            {
                                config.ContinueOnError = true;
                                config.OnTokenFound = OnTemplatorTokenFound;
                                _parsers.AddOrOverwrite(project.FullName, new TemplatorParser(config));
                            }
                        }
                    }
                }
                projectName = project.FullName;
            }
            else
            {
                _activeProjectName = null;
            }
            if (_activeProjectName != projectName 
                || (_dte.ActiveDocument.Name == TemplatorConfigFileName) == (_parser != null))
            {
                lock (LockObject)
                {
                    _parser = _dte.ActiveDocument.Name == TemplatorConfigFileName ? null : projectName == null ? null : _parsers.GetOrDefault(projectName);
                }
            }
            _activeProjectName = projectName;
            if (_buildDefaultConfig)
            {
                if (_dte != null && _dte.Solution != null)
                {
                    _solutionEvents = _dte.Solution.DTE.Events.SolutionItemsEvents;
                    _solutionEvents.ItemAdded -= SolutionItemsEvents_ItemChanged;
                    _solutionEvents.ItemAdded += SolutionItemsEvents_ItemChanged;
                    _solutionEvents.ItemRemoved -= SolutionItemsEvents_ItemChanged;
                    _solutionEvents.ItemRemoved += SolutionItemsEvents_ItemChanged;
                    _solutionEvents.ItemRenamed -= SolutionItemsEventsOnItemRenamed;
                    _solutionEvents.ItemRenamed += SolutionItemsEventsOnItemRenamed;
                    _buildDefaultConfig = false;
                }
            }
        }

        private TemplatorConfig TryGeTemplatorConfig(ProjectItem item)
        {
            if (item != null && item.FileCount > 0)
            {
                try
                {
                    return TemplatorConfig.FromXml(item.FileNames[1]);
                }
                catch (Exception e)
                {

                }
            }
            return null;
        }

        private void OnTemplatorTokenFound(object sender, TemplatorSyntaxEventArgs args)
        {
            var parser = (TemplatorParser) sender;
            if (_dte.ActiveDocument.Name == TemplatorConfigFileName)
            {
                return;
            }
            IClassificationType type = null;
            if (args.TokenText.IsNullOrEmpty())
            {
                return;
            }
            if (args.HasError)
            {
                if (args.Position > _lastPosition)
                {
                    AddSpan(args.Position - _lastPosition, _fault, args.TokenText);
                }
            }
            _lastPosition = args.Position - args.TokenText.Length;
            type = _default;
            if (args.TokenName == parser.Config.TermBeginEnd)
            {
                type = _brace;
            }
            else if (args.TokenName == parser.Config.TermName)
            {
                type = _recognized;
            }
            else if (args.TokenName == parser.Config.TermKeyword)
            {
                type = _keyword;
            }
            else if (args.TokenName == parser.Config.TermCategory)
            {
                type = _category;
            }
            else if (args.TokenName == parser.Config.TermParam)
            {
                type = _param;
            }
            else if (args.TokenName == parser.Config.TermParamBeginEnd)
            {
                type = _paramBrace;
            }
            else if (args.TokenName == parser.Config.TermKeywordsBeginEnd)
            {
                type = _descBrace;
            }
            else if (args.TokenName == parser.Config.TermCategorizedNameBeginEnd)
            {
                type = _categoryBrace;
            }
            AddSpan(args.TokenText.Length, type);
            _lastPosition = args.Position;
        }

        private void ProjectItemChanges(ProjectItem projectItem, string oldName = "")
        {
            if (oldName.EndsWith(TemplatorConfigFileName) || (projectItem.FileCount > 0 && projectItem.FileNames[1] != null && projectItem.FileNames[1].EndsWith(TemplatorConfigFileName)))
            {
                _activeProjectName = null;
                if (_parsers.ContainsKey(projectItem.ContainingProject.FullName))
                {
                    _parsers.Remove(projectItem.ContainingProject.FullName);
                }
            }
        }

        private void Document_Changed(Document document)
        {
            ProjectItemChanges(document.ProjectItem);
        }

        private void SolutionItemsEventsOnItemRenamed(ProjectItem projectItem, string oldName)
        {
            ProjectItemChanges(projectItem, oldName);
        }

        private void SolutionItemsEvents_ItemChanged(ProjectItem projectItem)
        {
            ProjectItemChanges(projectItem);
        }

        private void AddSpan(int length, IClassificationType classification, string backward = null)
        {
            if (backward != null && length > backward.Length)
            {
                length -= backward.Length;
            }
            if (classification != null && length > 0)
            {
                _spans.Add(new ClassificationSpan(new SnapshotSpan(_snapshot, _start+_lastPosition, length), classification));
            }
        }

    }
}
