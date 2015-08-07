using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using DotNetUtils;

namespace Templator
{
    public partial class TemplatorConfig
    {
        private static TemplatorConfig _instance;
        [XmlIgnore]
        public static TemplatorConfig DefaultInstance
        {
            get { return _instance ?? (_instance = new TemplatorConfig()); }
            set { _instance = value; }
        }

        [XmlIgnore]
        [Description("The Logger object used for parser to log errors")]
        public ILogger Logger = new Logger();
        [XmlIgnore]
        [Description("A event fired by the parser when no input found in the input dictionary to allow additional logic to find the value")]
        public EventHandler<TemplatorEventArgs> OnRequireInput;
        [XmlIgnore]
        [Description("A event fired by the parser when it finds a token while doing syntax parsing.")]
        public EventHandler<TemplatorSyntaxEventArgs> OnTokenFound;
        [XmlIgnore]
        [Description("A event fired by the parser when it finds a TextHolder.")]
        public EventHandler<TemplatorEventArgs> OnHolderFound;
        [XmlIgnore]
        [Description("The deafult text encoding used")]
        public Encoding Encoding = Encoding.UTF8;

        [Description("The maximum number of errors found by parser before it stops parsing.")] 
        public int MaxErrorCount = 1000;
        [Description("The DateTime format used by the parser to parse DateTime values")]
        public string DateFormat;
        [XmlIgnore]
        [Description("Options to control line breaks in the text file, such as to ensure the file uses windows(CRLF) mode or unix(LF)")]
        public LineBreakOption LineBreakOption;
        [XmlIgnore]
        [Description("Whether to throw exception when parser finds an unknown keyword")]
        public bool IgnoreUnknownKeyword = true;
        [XmlIgnore]
        [Description("Whether to throw exception when parser finds an unknown keyword Parameter")]
        public bool IgnoreUnknownParam = true;
        [XmlIgnore]
        [Description("Whether to make the category property optional of a TextHolder")]
        public bool CategoryOptional = true;
        [XmlIgnore]
        [Description("Set to true if this parse is only for checking syntax of the template")]
        public bool ContinueOnError = false;
        [XmlIgnore]
        [Description("Set to true to allow the parser to cache the result of a TextHolder which contains a keyword which is 'Calculated' into the input dictionary")]
        public bool CacheCalculatedResults = true;
        [XmlIgnore]
        [Description("Set to true to allow the parser to cache the result of a TextHolder which contains a keyword which is 'ManipulateInput' into the input dictionary")]
        public bool SaveManipulatedResults = true;
        [XmlIgnore]
        [Description("Set to true to allow the parser to cache the result(from calculation or 'OnRequireInput' event) of a TextHolder which is null")]
        public bool AllowCachingNullValues = false;
        [XmlIgnore]
        [Description("Contains all the keywords definition, modify this dictionary to make advanced customizations.")]
        public IDictionary<string, TemplatorKeyword> Keywords;

        [XmlIgnore]
        public int KeywordPriorityIncreamental = 10;
        [XmlIgnore]
        [Description("The Enum types for the parser to use when hitting TextHolder with Enum keyword")]
        public IDictionary<string, Type> Enums = new Dictionary<string, Type>();
        [Description("Pre-defined regular expressions (stored with a key of its name) for the parser to use, the parser will try to find a match with param string as name in this dict and pull the value otherwise use the param string as the regex ")]
        [XmlIgnore]
        public IDictionary<string, Regex> Regexes = new Dictionary<string, Regex>();
        [Description("The TextHolder Element name when parsing xml format input")]
        public string XmlFieldElementName = "Field";
        [Description("The TextHolder Value Element name when parsing xml format input")]
        public string XmlValueNodeName = "Value";
        [Description("The TextHolder Name Element name when parsing xml format input")]
        public string XmlNameNodeName = "Name";
        [Description("The TextHolder Child collection Element name when parsing xml format input")]
        public string XmlCollectionNodeName = "CollectionItem";
        [Description("The key used by the parser to store preparsed Holder definitions.")]
        public string KeyHolders = "$Fields";
        public TemplatorConfig()
        {
            PrepareKeywords();
        }

        public static TemplatorConfig FromXml(string path = "TemplatorConfig.xml")
        {
            return XDocument.Load(path).Root.FromXElement<TemplatorConfig>();
        }
    }
}
