using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public class TemplatorEventArgs
    {
        public object Value { get; set; }
        public TextHolder Holder { get; set; }
        public IDictionary<string, object> Input { get; set; }

    }
}
