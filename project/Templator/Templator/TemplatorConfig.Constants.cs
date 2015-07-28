using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public partial class TemplatorConfig
    {

        public string XmlReservedAttributeName = "Bindings";

        public string EscapePrefix = null;//"\\";
        public string Begin = "{{";
        public string End="}}";
        public string Delimiter=",";
        public string ParamBegin="(";
        public string ParamEnd = ")";
        public string KeywordsBegin = "[";
        public string KeywordsEnd = "]";

        public string TermCategory = "tCategory";
        public string TermName = "tName";
        public string TermValue = "tValue";
        public string TermText = "tText";
        public string TermKeyword = "tKeyword";
        public string TermParamedKeyword = "tParamedKeyword";

        public string KeywordRepeatBegin = "CollectionBegin";
        public string KeywordRepeat = "Collection";
        public string KeywordRepeatEnd = "CollectionEnd";
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

        public string KeywordExpression = "Expression";
        public string KeywordMath = "Math"; //Math expression,js?

        public string ReservedKeywordParent = "$$P$$";
        public string ReservedKeywordIndex = "$Index";
        public string ReservedKeyword0Index = "$0Index";
    }
}
