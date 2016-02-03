using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Templator
{
    public class HierarchResolver<T> : TextHolderResolverBase<T, HierarchResolver<T>> where T : TextHolderMappingContext
    {
        private readonly string _path;

        public HierarchResolver(string path)
        {
            _path = path;
        }

        public override bool Match(TextHolder holder, T context)
        {
            return _path == context.Path && MatchCollection(holder.Children != null, IsCollection) && Match(holder.Category, Categories) && Match(holder.Name, Names);
        }
        
    }
}
