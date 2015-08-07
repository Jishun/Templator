﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using DotNetUtils;

namespace Templator
{
    public class TemplatorKeyword
    {
        public readonly string Name;
        public int Preority;
        public bool HandleNullOrEmpty;
        public bool IndicatesOptional;
        public bool CalculateInput;
        public bool ManipulateInput;
        public bool ManipulateOutput;
        public bool IsValidation;
        public Func<TemplatorParser, TextHolder, bool> PostParse;
        public Action<TemplatorParser, string> Parse;
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