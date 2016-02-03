using System;
using System.Collections.Generic;
using System.Linq;
using DotNetUtils;

namespace Templator
{
    public class TemplatorInputMapper<TContext> where TContext : TextHolderMappingContext, new()
    {
        protected readonly bool UseDeclaredPreiories ;

        protected readonly IList<ITextHolderResolver<TContext>> CustomResolvers = new List<ITextHolderResolver<TContext>>();
        protected readonly IDictionary<string, IList<NameResolver<TContext>>> NameResolvers = new Dictionary<string, IList<NameResolver<TContext>>>();
        protected readonly IDictionary<string, IList<CategoryResolver<TContext>>> CategoryResolvers = new Dictionary<string, IList<CategoryResolver<TContext>>>();
        protected readonly IDictionary<string, IList<HierarchResolver<TContext>>> HierarchyResolvers = new Dictionary<string, IList<HierarchResolver<TContext>>>();
        protected Func<TextHolder, TContext, object> DefaultResovler;
        protected object DefaultResolvedValue;
        protected bool DefaultResolvedValueSet;

        public ILogger Logger;
        public Stack<TContext> Contexts;
        public IList<Func<TextHolder, ITextHolderResolver<TContext>>> ResolvingMethods = new List<Func<TextHolder, ITextHolderResolver<TContext>>>();
        public TContext Context => Contexts.Count > 0 ? Contexts.Peek() : null;

        /// <summary>
        /// Initialize a config instance
        /// </summary>
        /// <param name="useDeclaringOrderAsPreiories">Set to true to use the exact order of the resolvers added when resolving
        /// set to false to enable a method list to decide the priority of the resolver matching, the priority is determined by the order of enabling resolvers
        /// Call EnableXXXResolvers methods in the desire order, or call UseDefaultLookupOrder before generating</param>
        public TemplatorInputMapper(bool useDeclaringOrderAsPreiories = false)
        {
            UseDeclaredPreiories = useDeclaringOrderAsPreiories;
        }

        public TemplatorInputMapper(TemplatorInputMapper<TContext> configInstance, bool deepCopy = false)
        {
            UseDeclaredPreiories = configInstance.UseDeclaredPreiories;
            if (deepCopy)
            {
                CustomResolvers = configInstance.CustomResolvers.ToList();
                NameResolvers = configInstance.NameResolvers.Copy();
                CategoryResolvers = configInstance.CategoryResolvers.Copy();
                HierarchyResolvers = configInstance.HierarchyResolvers.Copy();
            }
            else
            {
                CustomResolvers = configInstance.CustomResolvers;
                NameResolvers = configInstance.NameResolvers;
                CategoryResolvers = configInstance.CategoryResolvers;
                HierarchyResolvers = configInstance.HierarchyResolvers;
            }
        }

        /// <summary>
        /// Add a resolver which matches text holder with a customized function
        /// </summary>
        /// <returns></returns>
        public CustomResolver<TContext> AddCustomResolver()
        {
            var ret = new CustomResolver<TContext>();
            CustomResolvers.Add(ret);
            return ret;
        }

        /// <summary>
        /// Add a resolver which matches primarily with the text holder's name defined
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public NameResolver<TContext> AddNameResolver(string name)
        {
            var ret = new NameResolver<TContext>(name);
            if (UseDeclaredPreiories)
            {
                CustomResolvers.Add(ret);
            }
            else
            {
                var list = GetResolverList(name, NameResolvers);
                list.Add(ret);
            }
            return ret;
        }

        /// <summary>
        /// Add a resolver which matches primarily with the text holder's category defined
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public CategoryResolver<TContext> AddCategoryResolver(string category)
        {
            var ret = new CategoryResolver<TContext>(category);
            if (UseDeclaredPreiories)
            {
                CustomResolvers.Add(ret);
            }
            else
            {
                var list = GetResolverList(category, CategoryResolvers);
                list.Add(ret);
            }
            return ret;
        }

        /// <summary>
        /// Add a resolver which matches primarily with the text holder's parent path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public HierarchResolver<TContext> AddHierarchyResolver(string path)
        {
            var ret = new HierarchResolver<TContext>(path);
            if (UseDeclaredPreiories)
            {
                CustomResolvers.Add(ret);
            }
            else
            {
                var list = GetResolverList(path, HierarchyResolvers);
                list.Add(ret);
            }
            return ret;
        }

        /// <summary>
        /// Set a default resolver called when no other resolvers were found
        /// </summary>
        /// <param name="resolverMethod"></param>
        public void SetDefaultResolver(Func<TextHolder, TContext, object> resolverMethod)
        {
            if (DefaultResolvedValueSet)
            {
                throw new InvalidOperationException("Default resolved constant value has been already set");
            }
            DefaultResovler = resolverMethod;
        }

        /// <summary>
        /// Set a default resolved value to be used when no resolver was matched for given text holder
        /// </summary>
        /// <param name="value"></param>
        public void SetDefaultValue(object value)
        {
            if (DefaultResovler != null)
            {
                throw new InvalidOperationException("Default resolver method has been already set");
            }
            DefaultResolvedValue = value;
            DefaultResolvedValueSet = true;
        }

        public IDictionary<string, object> GenerateInput(IDictionary<string, TextHolder> holders, TContext initialContext)
        {
            if (ResolvingMethods.IsNullOrEmpty())
            {
                throw new InvalidOperationException("No resolvers has been enabled yet.");
            }
            Contexts = new Stack<TContext>();
            return GenerateInputInternal(holders, initialContext);
        }

        protected IDictionary<string, object> GenerateInputInternal(IDictionary<string, TextHolder> holders, TContext context)
        {
            context = context ?? new TContext();
            context.Parent = Context;
            context.Root = context.Parent?.Root ?? context;
            context.Path = context.Parent?.Path + (String.IsNullOrEmpty(Context?.Path) ? null : ".") + context.Parent?.Holder?.Name;
            Contexts.Push(context);
            foreach (var holder in holders.Values)
            {
                context.Holder = holder;
                var resolver = FindResolver(holder);
                if (resolver != null)
                {
                    var value = resolver.ResolveValue(holder, context);
                    if (value == null && (DefaultResolvedValueSet || DefaultResovler != null))
                    {
                        value = (DefaultResolvedValueSet ? DefaultResolvedValue : DefaultResovler?.Invoke(holder, context));
                    }
                    if (value == null && !holder.IsOptional())
                    {
                        LogError("'{0}' is required.", holder.Name);
                    }
                    if (holder.Children == null)
                    {
                        context.Result.Add(holder.Name, value);
                    }
                    else
                    {
                        var dicts = value as IEnumerable<TContext>;
                        if (dicts == null)
                        {
                            LogError("Collection resolved non-collection value");
                        }
                        else
                        {
                            var list = new List<IDictionary<string, object>>();
                            var index = 0;
                            foreach (var item in dicts)
                            {
                                item.CollectionIndex = index++;
                                var child = GenerateInputInternal(holder.Children, item);
                                list.Add(child);
                            }
                            context.Result.Add(holder.Name, list.ToArray());
                        }
                    }
                }
                context.Holder = null;
            }
            var ret = context.Result;
            Contexts.Pop();
            return ret;
        }

        /// <summary>
        /// Remove all method from resolver method list
        /// </summary>
        public void DisableAllResolvers()
        {
            ResolvingMethods.Clear();
        }

        /// <summary>
        /// Use default fall back look up order, which is custom -> Name -> hierarchy -> category
        /// </summary>
        public void UseDefaultLookupOrder()
        {
            ResolvingMethods.Clear();
            ResolvingMethods.Add(FindCustomResolver);
            ResolvingMethods.Add(FindNameResolver);
            ResolvingMethods.Add(FindHierachyResolver);
            ResolvingMethods.Add(FindCategoryResolver);
        }

        /// <summary>
        /// Add custom resolvers into resolver method list
        /// </summary>
        public void EnableCustomResolvers()
        {
            ResolvingMethods.Add(FindCustomResolver);
        }

        /// <summary>
        /// Add name resolvers into resolver method list
        /// </summary>
        public void EnableNameResolvers()
        {
            ResolvingMethods.Add(FindNameResolver);
        }

        /// <summary>
        /// Add category resolvers into resolver method list
        /// </summary>
        public void EnableCategoryResolvers()
        {
            ResolvingMethods.Add(FindCategoryResolver);
        }

        /// <summary>
        /// Add hierarchy resolvers into resolver method list
        /// </summary>
        public void EnableHierachyResolvers()
        {
            ResolvingMethods.Add(FindHierachyResolver);
        }

        /// <summary>
        /// Add a custom method into resolver method list to provide a customized logic for resolver look up
        /// </summary>
        public void AddResolverMethod(Func<TextHolder, ITextHolderResolver<TContext>> method)
        {
            ResolvingMethods.Add(method);
        }

        protected ITextHolderResolver<TContext> FindResolver(TextHolder holder)
        {
            if (UseDeclaredPreiories)
            {
                var ret = FindCustomResolver(holder);
                if (ret != null)
                {
                    return ret;
                }
            }
            else
            {
                foreach (var method in ResolvingMethods)
                {
                    var ret = method(holder);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            if (!holder.IsOptional())
            {
                LogError("No resolvers found for holder: {0}.", holder);
            }
            return null;
        }

        protected NameResolver<TContext> FindNameResolver(TextHolder holder)
        {
            var list = NameResolvers.GetOrDefault(holder.Name);
            return list?.FirstOrDefault(item => item.Match(holder, Context));
        }

        protected ITextHolderResolver<TContext> FindCustomResolver(TextHolder holder)
        {
            return CustomResolvers.FirstOrDefault(item => item.Match(holder, Context));
        }

        protected CategoryResolver<TContext> FindCategoryResolver(TextHolder holder)
        {
            var list = CategoryResolvers.GetOrDefault(holder.Category);
            return list?.FirstOrDefault(item => item.Match(holder, Context));
        }

        protected HierarchResolver<TContext> FindHierachyResolver(TextHolder holder)
        {
            var list = HierarchyResolvers.GetOrDefault(Context.Path);
            return list?.FirstOrDefault(item => item.Match(holder, Context));
        }

        protected void LogError(string pattern, params object[] args)
        {
            Logger?.LogError(pattern, args);
        }

        protected IList<T> GetResolverList<T>(string key, IDictionary<string, IList<T>> dict) where T: ITextHolderResolver<TContext>
        {
            IList<T> list;
            if (!dict.ContainsKey(key))
            {
                list = new List<T>();
                dict.Add(key, list);
            }
            else
            {
                list = dict[key];
            }
            return list;
        }
    }
}