using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public class TemplatorParamsException : TemplatorException
    {
        public TemplatorParamsException(string message = null)
            : base(message)
        {

        }
    }

    public class TemplatorException : Exception
    {
        public TemplatorException(string message = null): base(message)
        {
            
        }
    }


    public class TemplatorOverlappedTextHolderException : TemplatorException
    {
        public TemplatorOverlappedTextHolderException(string message = null): base(message)
        {

        }
    }

    public class TemplatorSyntaxException : TemplatorException
    {
        public TemplatorSyntaxException(string message = null)
            : base(message)
        {

        }
    }

    public class TemplatorUnexpecetedKeywordException : TemplatorException
    {
        public TemplatorUnexpecetedKeywordException(string message = null)
            : base(message)
        {

        }
    }
    public class TemplatorUnexpecetedStateException : TemplatorException
    {
        public TemplatorUnexpecetedStateException(string message = null)
            : base(message)
        {

        }
    }
}
