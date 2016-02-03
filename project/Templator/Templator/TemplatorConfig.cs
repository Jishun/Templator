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
        public const string DefaultConfigFileName = "TemplatorConfig.xml";

        private static TemplatorConfig _instance;
        [XmlIgnore]
        public static TemplatorConfig DefaultInstance
        {
            get { return _instance ?? (_instance = new TemplatorConfig()); }
            set { _instance = value; }
        }

        [XmlIgnore]
        [Description("The TemplatorLogger object used for parser to log errors")]
        public ILogger Logger = new TemplatorLogger();
        [XmlIgnore]
        [Description("An event fired by the parser when no input found in the input dictionary to allow additional logic to find the value")]
        public EventHandler<TemplatorEventArgs> OnRequireInput;
        [XmlIgnore]
        [Description("An event fired by the parser when it finds a token while doing syntax parsing.")]
        public EventHandler<TemplatorSyntaxEventArgs> OnTokenFound;
        [XmlIgnore]
        [Description("An event fired by the parser when it finds a TextHolder.")]
        public EventHandler<TemplatorEventArgs> OnHolderFound;
        [XmlIgnore]
        [Description("The default text encoding used")]
        public Encoding Encoding = Encoding.UTF8;

        [Description("The maximum number of errors found by parser before it stops parsing.")] 
        public int MaxErrorCount = 1000;
        [Description("The DateTime format used by the parser to parse DateTime values")]
        public string DateFormat;
        [XmlIgnore]
        [Description("Options to control line breaks in the text file, such as to ensure the file uses windows(CRLF) mode or Unix(LF)")]
        public LineBreakOption LineBreakOption;
        [Description("Whether to throw exception when parser finds an unknown keyword")]
        public bool IgnoreUnknownKeyword = true;
        [Description("Whether to throw exception when parser finds an unknown keyword Parameter")]
        public bool IgnoreUnknownParam = true;
        [Description("Whether to Allow nested holders")]
        public bool AllowNested = true;
        [Description("Whether to treat empty string as a valid input, default true to treat empty as no input")]
        public bool EmptyAsNulls = true;
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
        [Description("Pre-defined regular expressions (stored with a key of its name) for the parser to use, the parser will try to find a match with param string as name in this dictionary and pull the value otherwise use the param string as the regex ")]
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
        [Description("The key used by the parser to store pre-parsed Holder definitions when using 2-pass passing, which allows logics inside OnRequireInput event to be able to find unreached TaxHolder's definition")]
        public string KeyHolders = "$Fields"; 

        [XmlElement]
        [Description("List of custom added keyword names to let syntax check task and highlight extension pass the validation")]
        public string[] CustomKeywordNames;
        [XmlElement]
        [Description("List of category names to enable syntax check task and highlight extension do validation, remove to skip verifying categories")]
        public HashSet<string> AvailableCategories;
        [XmlElement]
        [Description("Custom option entries to allow other libs to load config from TemplatorConfig file. the example ships is the syntax checking build task configuration")]
        public TemplatorCustomerConfigEntry[] CustomOptions;

        public static TemplatorConfig FromXml(string path = "TemplatorConfig.xml")
        {
            return FromXml(XDocument.Load(path).Root);
        }

        public static TemplatorConfig FromXml(XElement element)
        {
            var ret = element.FromXElement<TemplatorConfig>();
            return ret;
        }

        public class TemplatorCustomerConfigEntry
        {
            [XmlAttribute]
            public string Key;
            [XmlAttribute]
            public string Value;
            [XmlAttribute]
            public string Category;
        }
    }
}
