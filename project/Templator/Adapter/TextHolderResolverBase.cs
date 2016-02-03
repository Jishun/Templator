using System;
using System.Collections.Generic;
using DotNetUtils;

namespace Templator
{
    public abstract class TextHolderResolverBase<TContext, T> : ITextHolderResolver<TContext> 
        where TContext : TextHolderMappingContext
        where T : TextHolderResolverBase<TContext, T>
    {
        public enum ResolveLifeCycle
        {
            Singleton,
            PerResolve,
            PerContext,
            PerTemplate,
        }

        protected object ResolvedValue;

        public bool? IsCollection;
        public ResolveLifeCycle ValueLifeCycle;
        public Func<TextHolder, TContext, object> ResolverMethod;
        public IList<string> Names;
        public IList<string> Hierarchies;
        public IList<string> Categories;

        protected TextHolderResolverBase()
        {
            ValueLifeCycle = ResolveLifeCycle.PerContext;
        }

        public T SpecifyHierarchies(params string[] hierarchy)
        {
            Hierarchies = hierarchy;
            return (T)this;
        }

        public T SpecifyNames(params string[] names)
        {
            Names = names;
            return (T)this;
        }

        public T SpecifyCategories(params string[] categories)
        {
            Categories = categories;
            return (T)this;
        }

        public T AsCollectionContext()
        {
            IsCollection = true;
            return (T)this;
        }

        public T NonCollection()
        {
            IsCollection = false;
            return (T)this;
        }

        public T Singleton()
        {
            ValueLifeCycle = ResolveLifeCycle.Singleton;
            return (T)this;
        }

        public T PerResolve()
        {
            ValueLifeCycle = ResolveLifeCycle.PerResolve;
            return (T)this;
        }

        public T PerTemplate()
        {
            ValueLifeCycle = ResolveLifeCycle.PerTemplate;
            return (T)this;
        }

        public virtual void ResolveAs(object constant)
        {
            ResolvedValue = constant;
        }

        public virtual void ResolveAs(Func<TextHolder, TContext, object> method)
        {
            ResolverMethod = method;
        }

        public virtual object ResolveValue(TextHolder holder, TContext mapperContext)
        {
            if (ResolverMethod == null)
            {
                return ResolvedValue;
            }
            switch (ValueLifeCycle)
            {
                case ResolveLifeCycle.PerResolve:
                    return ResolverMethod(holder, mapperContext);
                case ResolveLifeCycle.Singleton:
                    return ResolvedValue ?? (ResolvedValue = ResolverMethod(holder, mapperContext));
                case ResolveLifeCycle.PerContext:
                    if (!mapperContext.Data.ContainsKey(this))
                    {
                        mapperContext.Data.Add(this, ResolverMethod(holder, mapperContext));
                    }
                    return mapperContext.Data.GetOrDefault(this);
                case ResolveLifeCycle.PerTemplate:
                    if (!mapperContext.Root.Data.ContainsKey(this))
                    {
                        mapperContext.Root.Data.Add(this, ResolverMethod(holder, mapperContext));
                    }
                    return mapperContext.Root.Data.GetOrDefault(this);
                default:
                    return null;
            }
        }

        public abstract bool Match(TextHolder holder, TContext context);

        protected bool Match(string name, IList<string> list)
        {
            return list == null || list.Contains(name);
        }

        protected bool MatchCollection(bool collection, bool? collectionSetting)
        {
            return collectionSetting == null || collectionSetting == collection;
        }
    }
}
