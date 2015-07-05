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
        public IList<XElement> ElementList;
        public XElement Element;
        public XAttribute Attribute;
        public IDictionary<string, object> Params = new Dictionary<string, object>();

        public Action<TemplatorParser> OnNextElement;
        public Action<TemplatorParser> OnParsingElement;

    }
}