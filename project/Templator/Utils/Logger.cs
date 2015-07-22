using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;

namespace Templator
{
    public class Logger : ILogger
    {
        public IList<string> Errors = new List<string>(); 
        public void Log(string pattern, params object[] args)
        {
        }

        public void LogError(string subcategory, string code, string file, int lineNumber, int columnNumber, int endLineNumber,
            int endColumnNumber, string message, string helpKeyword, string senderName)
        {
            throw new NotImplementedException();
        }

        public void LogError(string subcategory, string code, string file, int lineNumber, int columnNumber, int endLineNumber,
            int endColumnNumber, string message)
        {
            throw new NotImplementedException();
        }

        public void LogError(string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber, string message)
        {
            throw new NotImplementedException();
        }

        public void LogError(string pattern, params object[] args)
        {
            Errors.AddString(pattern, args);
        }

        public void LogWarning(string pattern, params object[] args)
        {
        }

        public void Trace(string pattern, params object[] args)
        {
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
