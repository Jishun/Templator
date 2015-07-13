using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public class TemplateEventArgs
    {
        public object Value { get; set; }
        public TextHolder Holder { get; set; }
    }
}
