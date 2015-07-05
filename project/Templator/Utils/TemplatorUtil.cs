using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;

namespace Templator
{
    public static class TemplatorUtil
    {
        public static void GrammarCheckDirectory(this TemplatorParser parser, string path, string[] filters, int depth)
        {
            var files = filters.IsNullOrEmpty()
                ? Directory.EnumerateFiles(path)
                : Directory.EnumerateFiles(path).Where(f => filters.Any(fi => f.EndsWith(fi, StringComparison.OrdinalIgnoreCase)));
            foreach (var name in files)
            {
                using (var sr = new StreamReader(name))
                {
                    parser.GrammarCheck(sr.ReadToEnd(), name);
                }
                if (parser.GrammarParser.Context.HasErrors)
                {
                    return;
                }
            }
            if (depth-- > 0)
            {
                foreach (var d in Directory.EnumerateDirectories(path))
                {
                    parser.GrammarCheckDirectory(d, filters, depth);
                    if (parser.GrammarParser.Context.HasErrors)
                    {
                        return;
                    }
                }
            }
        }

        public static IDictionary<string, object>[] GetChildCollection(this IDictionary<string, object> input, string key, TemplatorConfig config)
        {
            if (input != null && input.ContainsKey(key))
            {
                var retArray = input[key] as object[];
                if (retArray != null)
                {
                    var ret = retArray.Where(r => r is IDictionary<string, object>).Cast<IDictionary<string, object>>().ToArray();
                    foreach (var dictionary in ret)
                    {
                        dictionary.AddOrSkip(config.ReservedKeyWordParent, input);
                    }
                    return ret;
                }
            }
            return null;
        }

        public static IDictionary<string, TextHolder> MergeHolders(this IDictionary<string, TextHolder> holders, IEnumerable<TextHolder> addition)
        {
            foreach (var textHolder in addition)
            {
                var existing = holders.GetOrDefault(textHolder.Name);
                if (existing == null)
                {
                    holders.Add(textHolder.Name, textHolder);
                }
                else if (textHolder.IsCollection() == existing.IsCollection())
                {
                    if (textHolder.IsCollection())
                    {
                        existing.Children.MergeHolders(textHolder.Children.Values);
                    }
                }
                else
                {
                    throw new Exception("Repeat Item redefined differently");
                }
            }
            return holders;
        }

        public static object Aggregate(this TemplatorParser parser, TextHolder holder, string aggregateField, IDictionary<string, object> input, Func<object, object, object> aggregateFunc)
        {
            object ret = null;
            foreach (var c in aggregateField.Split(Constants.SemiDelimChar))
            {
                string left = null;
                var fieldName = c.GetUntil(".", out left);
                if (!left.IsNullOrWhiteSpace())
                {
                    var list = input.GetCollectionInput(fieldName, parser.Config);
                    ret = list.EmptyIfNull().Aggregate(ret, (current, item) => aggregateFunc(current, parser.Aggregate(holder, left, item, aggregateFunc)));
                }
                else
                {
                    var value = parser.GetValue(fieldName, input);
                    ret = aggregateFunc(ret, value);
                }
            }
            return ret;
        }

        public static object GetValue(this TemplatorParser parser, string holderName, IDictionary<string, object> input, int inherited = 0)
        {
            var holder = new TextHolder(holderName);
            holder[parser.Config.KeyWordSeekup] = inherited;
            return parser.GetValue(holder, input);
        }

        public static object GetValue(this TemplatorParser parser, TextHolder holder, IDictionary<string, object> input)
        {
            object value = null;
            if (input != null && input.ContainsKey(holder.Name))
            {
                value = input[holder.Name];
            }
            else
            {
                var arg = new TemplateParserEventArgs() { Holder = holder };
                parser.RequireInput(parser, arg);
                value = arg.Text;
            }
            value = holder.KeyWords.EmptyIfNull()
                .Where(key => key.OnGetValue != null)
                .OrderBy(k => k.Preority)
                .Aggregate(value, (current, key) =>
                {
                    if (current.IsNullOrEmpty() && !key.HandleNullOrEmpty)
                    {
                        return current;
                    }
                    return key.OnGetValue(holder, parser, current);
                });
            if (null == value)
            {
                parser.LogError("'{0}' is required", holder.Name);
            }
            return value;
        }
    }
}
