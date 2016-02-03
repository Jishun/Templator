using System.Collections.Generic;
using DotNetUtils;

namespace Templator
{
    public class TextHolderMappingContext
    {
        public TextHolderMappingContext Root;
        public TextHolderMappingContext Parent;
        public int CollectionIndex;
        public TextHolder Holder;
        public string Path;
        public IDictionary<string, object> Input;
        public IDictionary<string, object> Result = new Dictionary<string, object>();
        public IDictionary<object, object> Data = new Dictionary<object, object>();

        public T GetFromResult<T>(string key)
        {
            return (T)Result.GetOrDefault(key);
        }

        public T GetFromInput<T>(string key)
        {
            return (T)Input.GetOrDefault(key);
        }
    }
}