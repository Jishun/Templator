using System;
using System.IO;
using System.Linq;
using DotNetUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Templator;

namespace TemplatorSyntaxBuildTask
{
    public class TemplatorBuildTask : Task
    {
        private const string ConfigCategory = "SyntaxBuildTask";

        public string Path { get; set; }
        public string ConfigFilePath { get; set; }
        public string ProjectPath { get; set; }
        public string Filters { get; set; }
        public int Depth { get; set; }
        public ITaskItem TaskItem { get; set; }

        public override bool Execute()
        {
            var msg = new BuildMessageEventArgs("Templator Syntax checking", "", "TemplatorSyntaxChecker", MessageImportance.Normal);
            BuildEngine.LogMessageEvent(msg);
            if (ConfigFilePath.IsNullOrWhiteSpace())
            {
                var proj = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(ProjectPath).FirstOrDefault() ?? new Project(ProjectPath);
                var configFile = proj.GetItemsByEvaluatedInclude(TemplatorConfig.DefaultConfigFileName).FirstOrDefault();

                if (configFile != null)
                {
                    ConfigFilePath = configFile.EvaluatedInclude;
                }
            }

            TemplatorConfig config;
            if (ConfigFilePath.IsNullOrWhiteSpace())
            {
                config = TemplatorConfig.DefaultInstance;
                const string url = "https://github.com/Jishun/Templator/blob/master/project/Templator/TemplatorConfig.xml";
                msg = new BuildMessageEventArgs("Unable to find '{0}', using defaults, for a config file, please find; '{1}'".FormatInvariantCulture(TemplatorConfig.DefaultConfigFileName, url), "", "TemplatorSyntaxChecker", MessageImportance.Normal);
                BuildEngine.LogMessageEvent(msg);
            }
            else
            {
                try
                {
                    config = TemplatorConfig.FromXml(ConfigFilePath);
                    var path = config.CustomOptions.EmptyIfNull().PropertyOfFirstOrDefault(c => c.Category == ConfigCategory && c.Key== "Path", pr => pr.Value);
                    Path = path ?? Path;
                    var filter = config.CustomOptions.EmptyIfNull().PropertyOfFirstOrDefault(c => c.Category == ConfigCategory && c.Key == "Filters", pr => pr.Value);
                    Filters = filter ?? Filters;
                    var depth = config.CustomOptions.EmptyIfNull().PropertyOfFirstOrDefault(c => c.Category == ConfigCategory && c.Key == "Depth", pr => pr.Value);
                    int d;
                    Depth = int.TryParse(depth, out d) ? d : Depth;
                }
                catch (Exception e)
                {
                    var message = new BuildErrorEventArgs("TemplatorSyntaxConfig", "TemplatorConfigLoadError", ConfigFilePath, 0, 0, 0, 0, e.Message, "TemplatorBuildTask", "TemplatorBuildTask");
                    BuildEngine.LogErrorEvent(message);
                    return false;
                }
            }
            if (Path.IsHtmlNullOrWhiteSpace() || !Directory.Exists(Path))
            {
                var message = new BuildErrorEventArgs("TemplatorSyntaxConfig", "Unable to find the template path: '{0}'".FormatInvariantCulture(Path), "project", 0, 0, 0, 0, "", "TemplatorBuildTask", "TemplatorBuildTask");
                BuildEngine.LogErrorEvent(message);
                return false;
            }
            string[] filters = null;
            if (!Filters.IsNullOrWhiteSpace())
            {
                filters = Filters.Split(',');
            }
            var logger = new TemplatorLogger();
            config.Logger = logger;
            var p = new TemplatorParser(config);
            p.GrammarCheckDirectory(Path, filters, Depth);
            if (p.ErrorCount > 0)
            {
                foreach (var m in logger.Errors)
                {
                    var message = new BuildErrorEventArgs("TemplatorSyntaxChecker", "TemplatorSyntaxError", m.FileName, m.Line+1, m.Column+1, m.EndLineNumber+1, m.EndColumnNumber+1, m.Message, "TemplatorBuildTask", "TemplatorBuildTask");
                    BuildEngine.LogErrorEvent(message);
                }
            }
            return p.ErrorCount ==  0;
        }

    }
}
