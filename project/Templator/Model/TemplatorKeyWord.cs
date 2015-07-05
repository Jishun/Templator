using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using DotNetUtils;
using Irony.Parsing;

namespace Templator
{
    public class TemplatorKeyWord
    {
        public readonly string Name;
        public int Preority;
        public bool HandleNullOrEmpty;
        public bool IndicatesOptional;
        public Func<TemplatorParser, TextHolder, bool> PostParse;
        public Action<TemplatorParser, string> Parse;
        public Func<TextHolder, TemplatorParser, object, object> OnGetValue;
        public TemplatorKeyWord(string name)
        {
            Name = name;
        }

        public TemplatorKeyWord Create()
        {
            return this;
        }

        public override string ToString()
        {
            return Name;
        }

    }
}