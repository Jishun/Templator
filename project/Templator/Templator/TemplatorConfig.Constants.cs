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

        public string EscapePrefix = "\\";
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

        public string KeyWordRepeatBegin = "CollectionBegin";
        public string KeyWordRepeat = "Collection";
        public string KeyWordRepeatEnd = "CollectionEnd";
        public string KeyWordEnum = "Enum";
        public string KeyWordDisplayName = "DisplayName";
        public string KeyWordNumber = "Number";
        public string KeyWordDateTime = "DateTime";
        public string KeyWordFormat = "Format";
        public string KeyWordEven = "Even";
        public string KeyWordAwayFromZero = "AwayZero";
        public string KeyWordMap = "Map";
        public string KeyWordBit = "Bool";
        public string KeyWordReplace = "Replace";
        public string KeyWordRemoveChar = "RemoveChar";
        public string KeyWordTransform = "Transform";
        public string KeyWordUpper = "Upper";
        public string KeyWordLower = "Lower";
        public string KeyWordTruncate = "Truncate";
        public string KeyWordArray = "Array";
        public string KeyWordDefault = "Default";
        public string KeyWordRegex = "RegExp";
        public string KeyWordLength = "Length";
        public string KeyWordRefer = "Refer";
        public string KeyWordHolder = "Holder";
        public string KeyWordSelect = "Select";
        public string KeyWordExpression = "Expression";
        public string KeyWordMin = "Min";
        public string KeyWordMax = "Max";
        public string KeyWordFill = "Fill";
        public string KeyWordAppend = "Append";
        public string KeyWordPrefill = "Prepend";
        public string KeyWordFixedLength = "FixedLength";
        public string KeyWordTrim = "Trim";
        public string KeyWordArrayEnd = "ArrayEnd";
        public string KeyWordIfnot = "Ifnot";
        public string KeyWordAttributeIfnot = "AttributeIfnot";
        public string KeyWordIf = "If";
        public string KeyWordAttributeIf = "AttributeIf";
        public string KeyWordThen = "Then";
        public string KeyWordAttributeThen = "AttributeThen";
        public string KeyWordEnumElement = "EnumElement";
        public string KeyWordElementName = "ElementName";
        public string KeyWordAttributeName = "AttributeName";
        public string KeyWordSeekup = "Inherited";
        public string KeyWordSum = "Sum";
        public string KeyWordCount = "Count";
        public string KeyWordMulti = "Multi";
        public string KeyWordOptional = "Optional";
        public string KeyWordJs = "Js";
        public string KeyWordComments = "Tips";

        public string ReservedKeyWordParent = "$$P$$";
        public string ReservedKeyWordXattribute = "TemplatorParser";
    }
}
