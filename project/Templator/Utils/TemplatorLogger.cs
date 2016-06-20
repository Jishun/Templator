using System;
using System.Collections.Generic;
using DotNetUtils;

namespace Templator
{
    public class TemplatorLogger : ILogger
    {
        public class TemplatorLogEntry
        {
            public int Line;
            public int Column;
            public int EndLineNumber;
            public int EndColumnNumber;
            public string FileName;
            public string Message;
        }


        public IList<TemplatorLogEntry> Errors = new List<TemplatorLogEntry>(); 
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
            Errors.Add(new TemplatorLogEntry()
            {
                FileName = file,
                Message = message,
                Column = columnNumber,
                Line = lineNumber,
                EndColumnNumber = endColumnNumber,
                EndLineNumber = endLineNumber
            });
        }

        public void LogError(string pattern, params object[] args)
        {
            Errors.Add(new TemplatorLogEntry(){Message = pattern.FormatInvariantCulture(args)});
        }

        public void LogWarning(string pattern, params object[] args)
        {
        }

        public void Trace(string pattern, params object[] args)
        {
        }

        public bool Clear()
        {
            throw new NotImplementedException();
        }

        public bool IsEmpty()
        {
            return Errors.Count == 0;
        }

        public bool IsNullOrEmpty()
        {
            throw new NotImplementedException();
        }

    }
}
