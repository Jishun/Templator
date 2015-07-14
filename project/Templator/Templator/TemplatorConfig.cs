using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetUtils;
using Irony.Parsing;

namespace Templator
{
    public partial class TemplatorConfig
    {
        private static TemplatorConfig _instance;
        public static TemplatorConfig Instance
        {
            get { return _instance ?? (_instance = new TemplatorConfig()); }
            set { _instance = value; }
        }

        public string DateFormat;

        public ILogger Logger = new Logger();
        public EventHandler<TemplateEventArgs> RequireInput;

        public Encoding Encoding = Encoding.UTF8;
        public LineBreakOption LineBreakOption;
        public XmlElementRepeatBehavior XmlElementRepeatBehavior = XmlElementRepeatBehavior.RepeatGroupIfMultiple;
        public bool IgnoreUnknownKeyword = true;
        public bool IgnoreUnknownParam = true;
        public bool CategoryOptional = false;
        public bool SyntaxCheckOnly = false;

        public IDictionary<string, TemplatorKeyword> Keywords;

        public IDictionary<string, Type> Enums = new Dictionary<string, Type>();
        public IDictionary<string, Regex> Regexes = new Dictionary<string, Regex>();

        public string XmlTemplateAttr = "Bindings";
        public string XmlFieldElementName = "Field";
        public string XmlValueNodeName = "Value";
        public string XmlNameNodeName = "Name";
        public string XmlCollectionNodeName = "CollectionItem";
        public string KeyHolders = "Fields";
        public TemplatorConfig()
        {
            PrepareKeywords();
        }
    }
}
