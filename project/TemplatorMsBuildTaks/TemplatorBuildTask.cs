using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;
using Microsoft.Build.Framework;
using Templator;

namespace TemplatorSyntaxBuildTask
{
    public class TemplatorBuildTask : ITask
    {
        [Required]
        public string Path { get; set; }
        public string Filters { get; set; }
        public int Depth { get; set; }
        public ITaskItem TaskItem { get; set; }
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            var m = new BuildMessageEventArgs("Templator Syntax checking", "", "TemplatorSyntaxChecker", MessageImportance.Normal);
            BuildEngine.LogMessageEvent(m);
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
            var config = new TemplatorConfig()
            {
                TermCategory = "InputType",
                TermName = "FieldName",
                TermValue = "ParamValue",
                TermText = "FreeText",
                TermKeyword = "TemplateKeyword",
                TermParamedKeyword = "TemplateParamedKeyword",
            };

            var p = new TemplatorParser(config);
            p.GrammarCheckDirectory(Path, filters, Depth);
            if (p.GrammarParser.Context.HasErrors)
            {
                foreach (var parserMessage in p.GrammarParseTree.ParserMessages)
                {
                    var message = new BuildErrorEventArgs("TemplatorSyntaxChecker", "TemplatorSyntaxError", p.GrammarParseTree.FileName, parserMessage.Location.Line, parserMessage.Location.Column, parserMessage.Location.Line, parserMessage.Location.Column, parserMessage.Message, "TemplatorBuildTask", "TemplatorBuildTask");
                    BuildEngine.LogErrorEvent(message);
                }
            }
            return !p.GrammarParser.Context.HasErrors;
        }

    }
}
