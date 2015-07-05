using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;
using Microsoft.Build.Framework;
using ILogger = DotNetUtils.ILogger;

namespace TemplatorMsBuildTaks
{
    public class MsBuildLogger : ILogger
    {
        private readonly IBuildEngine _engine;

        public MsBuildLogger(IBuildEngine engine)
        {
            _engine = engine;
        }

        public void Log(string pattern, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void LogError(string subcategory, string code, string file, int lineNumber, int columnNumber,
            int endLineNumber, int endColumnNumber, string message, string helpKeyword, string senderName)
        {
            
        }

        public void LogError(string subcategory, string code, string file, int lineNumber, int columnNumber,
            int endLineNumber, int endColumnNumber, string message)
        {

        }
        public void LogError(string file, int lineNumber, int columnNumber,
            int endLineNumber, int endColumnNumber, string message)
        {

        }

        public void LogError(string pattern, params object[] args)
        {
            var message = new BuildErrorEventArgs("TemplatorSyntax", "Templator001", "fileName", 0, 0, 0, 0, pattern.Format(args), string.Empty, "TemplatorSyntax");
            _engine.LogErrorEvent(message);
        }

        public void LogWarning(string pattern, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void Trace(string pattern, params object[] args)
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public bool IsNullOrEmpty()
        {
            throw new NotImplementedException();
        }
    }
}
