using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;

namespace Templator
{
    public class TextHolder
    {
        public string Name;
        public string Category;
        public string SourceText;
        public int Position;

        public IDictionary<string, TextHolder> Children;
        public IList<TemplatorKeyWord> KeyWords = new List<TemplatorKeyWord>();
        public IDictionary<string, object> Params = new Dictionary<string, object>();

        public object this[string keywordName]
        {
            get { return Params.GetOrDefault(keywordName); }
            set { Params[keywordName] = value; }
        }

        public TextHolder()
        {
            
        }

        public TextHolder(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return SourceText ?? Name;
        }
    }
}
