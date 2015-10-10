using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using DotNetUtils;

namespace Templator
{
    public class TemplatorParsingContext
    {
        public ILogger Logger;
        public ISeekable Text;
        public IDictionary<string, object> Input;
        public TextHolder ParentHolder;
        public TextHolder Holder;
        public bool NestingBefore;
        public bool NestingAfter;
        public IDictionary<string, TextHolder> Holders = new Dictionary<string, TextHolder>();
        public IDictionary<string, TextHolder> PreparsedHolders;
        public StringBuilder Result;
        public ILogger ChildLogger;
        public StringBuilder ChildResultBefore;
        public StringBuilder ChildResultAfter;
        public HolderParseState State = new HolderParseState();
        public IDictionary<string, object> Params = new Dictionary<string, object>();

        public bool Nesting
        {
            get { return NestingAfter || NestingBefore; }
        }

        public object this[string key]
        {
            [DebuggerStepThrough]
            get { return Params.GetOrDefault(key); }
            [DebuggerStepThrough]
            set { Params[key] = value; }
        }

        public TemplatorParsingContext(bool skipOutput = false)
        {
            if (!skipOutput)
            {
                Result = Text != null && Text.Length > 0 ? new StringBuilder(Text.Length) : new StringBuilder();
            }
            ChildResultBefore = new StringBuilder();
            ChildResultAfter = new StringBuilder();
        }

        public void ClearResult()
        {
            if (Result != null)
            {
                Result.Clear();
            }
        }

        public string GetResult()
        {
            return Result == null ? String.Empty : Result.ToString();
        }
    }
}