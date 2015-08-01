using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;

namespace DocGenerate
{
    public class TemplatorKeywordHelp
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<Triple<string, string, string>> Examples { get; set; }
        public IList<Pair<string, string>> Params { get; set; } 
    }
}
