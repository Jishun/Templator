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
        public IDictionary<string, TextHolder> Holders = new Dictionary<string, TextHolder>();
        public StringBuilder Result = new StringBuilder();
        public IDictionary<string, object> Params = new Dictionary<string, object>();

        public object this[string key]
        {
            [DebuggerStepThrough]
            get { return Params.GetOrDefault(key); }
            [DebuggerStepThrough]
            set { Params[key] = value; }
        }

    }
}