using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Templator
{
    public partial class TemplatorConfig
    {
        [Description("The reserved xml attribute to hold Templator used xml manipulation TextHolders such as Collections, this attribute will be removed while processing")]
        public string XmlTemplatorAttributeName = "Bindings";
        [Description("When goes deeper level into an Array/Repeat/Collection, the parent input will be stored in the current input with this string as key")]
        public string ReservedKeywordParent = "$$P$$";
        [Description("When looping arries/repeat/collection, retrieve the 1 based index with this name")]
        public string ReservedKeywordIndex = "$Index";
        [Description("When looping arries/repeat/collection, retrieve the 0 based index with this name")]
        public string ReservedKeyword0Index = "$0Index";

        [Description("An escape character when parsing template")]
        public string EscapePrefix = null;//"\\";
        [Description("The begginng of a TextHolder, configurable in case of symbol conflict, default '{{'")]
        public string Begin = "{{";
        [Description("The ending of a TextHolder, configurable in case of symbol conflict, default '}}'")]
        public string End = "}}";
        [Description("The delimiter of keywords, configurable in case of symbol conflict, default ','")]
        public string Delimiter = ",";
        [Description("The begginng of a keyword's param, configurable in case of symbol conflict, default '('")]
        public string ParamBegin = "(";
        [Description("The ending of a keyword's param, configurable in case of symbol conflict, default ')'")]
        public string ParamEnd = ")";
        [Description("The begginng of the TextHolders' name if category exists, configurable in case of symbol conflict, default '('")]
        public string CategorizedNameBegin = "(";
        [Description("The ending of the TextHolders' name if category exists, configurable in case of symbol conflict, default ')'")]
        public string CategorizedNameEnd = ")";
        [Description("The begginng of the keyword collection, configurable in case of symbol conflict, default '['")]
        public string KeywordsBegin = "[";
        [Description("The ending of the keyword collection, configurable in case of symbol conflict, default ']'")]
        public string KeywordsEnd = "]";

        public string SyntaxErrorCategoryNotFound = "SyntaxError: No category defined in Holder, add use Category(Name) to define name or set CategoryOptional=true in config";
        public string SyntaxErrorOverLappedHolder = "SyntaxError: Overlapped TextHolders";
        public string SyntaxErrorUnmatchedBeginTag = "SyntaxError: Reached document end before finding the closing tag";
        public string SyntaxErrorUnmatchedKeywordsBeginTag = "SyntaxError: Reached document end before finding keywords the closing tag";
        public string SyntaxErrorUnmatchedBeginNameTag = "SyntaxError: Reached document end before finding the name closing tag";
        public string SyntaxErrorHolderNameNotFound = "SyntaxError: TextHolder's Name not defined";
        public string SyntaxErrorUnexpectedString = "SyntaxError: Unexpcted string";
        public string SyntaxErrorUnexpectedKeywordParam = "SyntaxError: Unexpcted param for keyword";
        public string SyntaxErrorUnexpectedKeyword = "SyntaxError: Unexpcted keyword";

        public string TermBeginEnd = "BeginEnd";
        public string TermCategorizedNameBeginEnd = "CBeginEnd";
        public string TermParamBeginEnd = "PBeginEnd";
        public string TermKeywordsBeginEnd = "KBeginEnd";
        public string TermCategory = "Category";
        public string TermName = "Name";
        public string TermValue = "ParamValue";
        public string TermKeyword = "Keyword";
        public string TermParam = "Parameter";
        public string TermDelimiter = "Delimeter";

        public string KeywordRepeatBegin = "CollectionBegin";
        public string KeywordRepeat = "Collection";
        public string KeywordRepeatEnd = "CollectionEnd";
        public string KeywordNested = "Nested";
        public string KeywordNestedXml = "NestedXml";
        public string KeywordAlignCount = "AlignCount";
        public string KeywordEnum = "Enum";
        public string KeywordDisplayName = "DisplayName";
        public string KeywordNumber = "Number";
        public string KeywordDateTime = "DateTime";
        public string KeywordFormat = "Format";
        public string KeywordEven = "Even";
        public string KeywordAwayFromZero = "AwayZero";
        public string KeywordMap = "Map";
        public string KeywordBit = "Bool";
        public string KeywordReplace = "Replace";
        public string KeywordRemoveChar = "RemoveChar";
        public string KeywordTransform = "Transform";
        public string KeywordUpper = "Upper";
        public string KeywordLower = "Lower";
        public string KeywordTruncate = "Truncate";
        public string KeywordArray = "Array";
        public string KeywordDefault = "Default";
        public string KeywordRegex = "RegExp";
        public string KeywordLength = "Length";
        public string KeywordRefer = "Refer";
        public string KeywordHolder = "Holder";
        public string KeywordSelect = "Select";
        public string KeywordMin = "Min";
        public string KeywordMax = "Max";
        public string KeywordFill = "Fill";
        public string KeywordAppend = "Append";
        public string KeywordPrefill = "Prepend";
        public string KeywordFixedLength = "FixedLength";
        public string KeywordTrim = "Trim";
        public string KeywordArrayEnd = "ArrayEnd";
        public string KeywordIfnot = "Ifnot";
        public string KeywordAttributeIfnot = "AttributeIfnot";
        public string KeywordIf = "If";
        public string KeywordAttributeIf = "AttributeIf";
        public string KeywordThen = "Then";
        public string KeywordAttributeThen = "AttributeThen";
        public string KeywordEnumElement = "EnumElement";
        public string KeywordElementName = "ElementName";
        public string KeywordAttributeName = "AttributeName";
        public string KeywordSeekup = "Inherited";
        public string KeywordAverage = "Avg";
        public string KeywordSum = "Sum";
        public string KeywordCount = "Count";
        public string KeywordMulti = "Multi";
        public string KeywordMathMax = "MathMax";
        public string KeywordMathMin = "MathMin";
        public string KeywordOptional = "Optional";
        public string KeywordJs = "Js";
        public string KeywordComments = "Tips";
        public string KeywordCsv = "Csv";
        public string KeywordEncode = "Encode";
        public string KeywordDecode = "Decode";
        public string KeywordBase64 = "Base64";
        public string KeywordBase32 = "Base32";
        public string KeywordUrl = "Url";
        public string KeywordHtml = "Html";
        public string KeywordJoin = "Join";

        public string KeywordExpression = "Expression";
        public string KeywordMath = "Math"; //Math expression,js?
    }
}
