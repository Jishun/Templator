using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public interface ITextHolderResolver<in TContext> where TContext : TextHolderMappingContext
    {
        object ResolveValue(TextHolder holder, TContext mapperContext);

        bool Match(TextHolder holder, TContext context);
    }
}
