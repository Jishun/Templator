namespace Templator
{
    public class CategoryResolver<T> : TextHolderResolverBase<T, CategoryResolver<T>> where T : TextHolderMappingContext
    {
        private readonly string _category;

        public CategoryResolver(string category)
        {
            _category = category;
        }

        public override bool Match(TextHolder holder, T context)
        {
            return _category == holder.Category && MatchCollection(holder.Children != null, IsCollection) && Match(holder.Name, Names) && Match(context.Path, Hierarchies);
        }
    }
}
