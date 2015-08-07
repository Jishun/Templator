using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DotNetUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Templator;

namespace TemplatorSyntaxBuildTask
{
    public class TemplatorBuildTask : Task
    {
        public string Path { get; set; }
        public string ConfigFilePath { get; set; }
        public string ProjectPath { get; set; }
        public string Filters { get; set; }
        public int Depth { get; set; }
        public ITaskItem TaskItem { get; set; }

        public override bool Execute()
        {
            var m = new BuildMessageEventArgs("Templator Syntax checking", "", "TemplatorSyntaxChecker", MessageImportance.Normal);
            BuildEngine.LogMessageEvent(m);
            if (ConfigFilePath.IsNullOrWhiteSpace())
            {
                var proj = new Project(ProjectPath ?? BuildEngine.ProjectFileOfTaskNode);
                var configFile = proj.Items
                        .FirstOrDefault(i => i.EvaluatedInclude.Equals("TemplatorConfig.xml", StringComparison.OrdinalIgnoreCase));
                if (configFile != null)
                {
                    ConfigFilePath = configFile.EvaluatedInclude;
                }
            }

            TemplatorConfig config;
            if (ConfigFilePath.IsNullOrWhiteSpace())
            {
                config = TemplatorConfig.DefaultInstance;
            }
            else
            {
                try
                {
                    var x = XDocument.Load(ConfigFilePath);
                    config = x.Root.FromXElement<TemplatorConfig>();
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
                var message = new BuildErrorEventArgs("TemplatorSyntaxConfig", "TemplatorSyntaxError", "project", 0, 0, 0, 0, "", "TemplatorBuildTask", "TemplatorBuildTask");
                BuildEngine.LogErrorEvent(message);
                return false;
            }
            string[] filters = null;
            if (!Filters.IsNullOrWhiteSpace())
            {
                filters = Filters.Split(',');
            }

            var p = new TemplatorParser(config);
            p.GrammarCheckDirectory(Path, filters, Depth);
            if (p.ErrorCount > 0)
            {
                //foreach (var parserMessage in p.GrammarParseTree.ParserMessages)
                //{
                //    var message = new BuildErrorEventArgs("TemplatorSyntaxChecker", "TemplatorSyntaxError", p.GrammarParseTree.FileName, parserMessage.Location.Line, parserMessage.Location.Column, parserMessage.Location.Line, parserMessage.Location.Column, parserMessage.Message, "TemplatorBuildTask", "TemplatorBuildTask");
                //    BuildEngine.LogErrorEvent(message);
                //}
            }
            return p.ErrorCount ==  0;
        }

    }
}
