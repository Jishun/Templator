using System;

namespace Templator
{
    public class CustomResolver<T> : TextHolderResolverBase<T, CustomResolver<T>> where T: TextHolderMappingContext
    {
        private Func<TextHolder, T, bool> _matching;

        public CustomResolver<T> MatchWith(Func<TextHolder, T, bool> method)
        {
            _matching = method;
            return this;
        }

        public override bool Match(TextHolder holder, T context)
        {
            if (_matching == null)
            {
                throw new InvalidOperationException("Custom resolver doesn't have rule to match with the text holders");
            }
            return _matching(holder, context) 
                && MatchCollection(holder.Children != null, IsCollection) 
                && Match(holder.Category, Categories) 
                && Match(holder.Name, Names)
                && Match(context.Path, Hierarchies);
        }
    }
}
