using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
        public static TemplatorConfig DefaultInstance
        {
            get { return _instance ?? (_instance = new TemplatorConfig()); }
            set { _instance = value; }
        }

        [Description("The DateTime format used by the parser to parse DateTime values")]
        public string DateFormat;
        public ILogger Logger = new Logger();
        [Description("A event fired by the parser when no input found in the input dictionary to allow additional logic to find the value")]
        public EventHandler<TemplateEventArgs> RequireInput;
        [Description("The deafult text encoding used")]
        public Encoding Encoding = Encoding.UTF8;
        [Description("Options to control line breaks in the text file, such as to ensure the file uses windows(CRLF) mode or unix(LF)")]
        public LineBreakOption LineBreakOption;
        [Description("To control the repeat behavior of repeating a group of XElement, e.g: '<a/><b/>' -> '<a/><a/><b/><b/>' or '<a/><b/><a/><b/>'")]
        public XmlElementRepeatBehavior XmlElementRepeatBehavior = XmlElementRepeatBehavior.RepeatGroupIfMultiple;
        [Description("Whether to throw exception when parser finds an unknown keyword")]
        public bool IgnoreUnknownKeyword = true;
        [Description("Whether to throw exception when parser finds an unknown keyword Parameter")]
        public bool IgnoreUnknownParam = true;
        [Description("Whether to make the category property optional of a TextHolder")]
        public bool CategoryOptional = false;
        [Description("Set to true if this parse is only for checking syntax of the template")]
        public bool SyntaxCheckOnly = false;
        [Description("Set to true to allow the parser to cache the result of a TextHolder which contains a keyword which is 'Calculated' into the input dictionary")]
        public bool CacheCalculatedResults = true;
        [Description("Set to true to allow the parser to cache the result of a TextHolder which contains a keyword which is 'ManipulateInput' into the input dictionary")]
        public bool SaveManipulatedResults = true;
        [Description("Set to true to allow the parser to cache the result(from calculation or 'RequireInput' event) of a TextHolder which is null")]
        public bool AllowCachingNullValues = false;
        [Description("Contains all the keywords definition, modify this dictionary to make advanced customizations.")]
        public IDictionary<string, TemplatorKeyword> Keywords;

        public int KeywordPriorityIncreamental = 10;
        [Description("The Enum types for the parser to use when hitting TextHolder with Enum keyword")]
        public IDictionary<string, Type> Enums = new Dictionary<string, Type>();
        [Description("Pre-defined regular expressions (stored with a key of its name) for the parser to use, the parser will try to find a match with param string as name in this dict and pull the value otherwise use the param string as the regex ")]
        public IDictionary<string, Regex> Regexes = new Dictionary<string, Regex>();

        public string XmlTemplateAttr = "Bindings";
        public string XmlFieldElementName = "Field";
        public string XmlValueNodeName = "Value";
        public string XmlNameNodeName = "Name";
        public string XmlCollectionNodeName = "CollectionItem";
        public string KeyHolders = "$Fields";
        public TemplatorConfig()
        {
            PrepareKeywords();
        }
    }
}
