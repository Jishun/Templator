using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public class NameResolver<T> : TextHolderResolverBase<T, NameResolver<T>> where T : TextHolderMappingContext
    {
        private readonly string _name;

        public NameResolver(string name)
        {
            _name = name;
        }
        

        public override bool Match(TextHolder holder, T context)
        {
            return _name == holder.Name && MatchCollection(holder.Children != null, IsCollection) && Match(holder.Category, Categories) && Match(context.Path, Hierarchies);
        }
    }
}
