using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using DotNetUtils;

namespace Templator
{
    public class TemplatorKeyword
    {
        [Description("The identifier of the Keyword")]
        public readonly string Name;
        [Description("Affects the order of the keywords while processing, the order logic is Desending: CalculateInput -> ManipulateInput -> IsValidation && !k.ManipulateOutput -> IsValidation ManipulateOutput -> ascending, Priority")]
        public int Preority;
        [Description("Indicates if Null or Empty string will be passed in to the OnGetValue or skip")]
        public bool HandleNullOrEmpty;
        [Description("Indicates this holder becomes optional if the keyword presents")]
        public bool IndicatesOptional;
        [Description("Indicates if the keyword will calculate a value as input, the input will be cached automatically for it")]
        public bool CalculateInput;
        [Description("Indicates if the keyword will change a input value, the changed input will be cached automatically for it")]
        public bool ManipulateInput;
        [Description("Indicates if the keyword will change a output value, the changed value will NOT be cached")]
        public bool ManipulateOutput;
        [Description("Indicates if the keyword is doing validation")]
        public bool IsValidation;
        [Description("Delegate called after the TextHolder is parsed before trying to get the value")]
        public Func<TemplatorParser, TextHolder, bool> PostParse;
        [Description("Delegate called when a keyword's param is found, if null, the param will be stored as is, to access the param, use holder[\"keywordname\"]")]
        public Action<TemplatorParser, string> Parse;
        [Description("Delegate called when the holder is being processed to get value")]
        public Func<TextHolder, TemplatorParser, object, object> OnGetValue;

        #region HelpContent

        public string Description { get; set; }
        public IList<Triple<string, string, string>> Examples { get; set; }
        public IList<Pair<string, string>> Params { get; set; } 
        #endregion

        public TemplatorKeyword(string name)
        {
            Name = name;
        }

        public TemplatorKeyword Create()
        {
            return this;
        }
        public TemplatorKeyword Clone()
        {
            return (TemplatorKeyword)MemberwiseClone();
        }

        public override string ToString()
        {
            return Name;
        }

    }
}