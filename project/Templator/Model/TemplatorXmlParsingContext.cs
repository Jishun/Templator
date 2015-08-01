using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using DotNetUtils;

namespace Templator
{
    public class TemplatorXmlParsingContext
    {
        public int ElementIndex;
        public IList<XNode> ElementList;
        public XElement Element;
        public XAttribute Attribute;
        public IDictionary<string, object> Params = new Dictionary<string, object>();

        public Action<TemplatorParser> OnAfterParsingElement;
        public Action<TemplatorParser> OnBeforeParsingElement;
        public object this[string key]
        {
            [DebuggerStepThrough]
            get { return Params.GetOrDefault(key); }
            [DebuggerStepThrough]
            set { Params[key] = value; }
        }
    }
}