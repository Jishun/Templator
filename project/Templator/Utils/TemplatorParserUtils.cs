using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json;
using DotNetUtils;

namespace Templator
{
    public static class TemplatorParserUtil
    {
        public static object Aggregate(this TemplatorParser parser, object current, TextHolder holder, string aggregateField, IDictionary<string, object> input, Func<object, object, object> aggregateFunc)
        {
            foreach (var c in aggregateField.Split(Constants.SemiDelimChar))
            {
                string left = null;
                var fieldName = c.GetUntil(".", out left);
                if (!left.IsNullOrWhiteSpace())
                {
                    var list = input.GetChildCollection(fieldName, parser.Config);
                    current = list.EmptyIfNull().Aggregate(current, (current1, subInput) => parser.Aggregate(current1, holder, left, subInput, aggregateFunc));
                }
                else
                {
                    var value = parser.GetValue(fieldName, input);
                    current = aggregateFunc(current, value);
                }
            }
            return current;
        }

        public static object GetValue(this TemplatorParser parser, string holderName, IDictionary<string, object> input, int inherited = 0)
        {
            var holder = GetHolder(input, holderName, parser.Config);
            holder[parser.Config.KeywordSeekup] = inherited;
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
                var arg = new TemplateEventArgs() { Holder = holder };
                parser.RequireInput(parser, arg);
                value = arg.Text;
            }
            value = holder.Keywords.EmptyIfNull()
                .Where(key => key.OnGetValue != null)
                .OrderBy(k => k.Preority)
                .Aggregate(value, (current, key) =>
                {
                    if (current.IsNullOrEmptyValue() && !key.HandleNullOrEmpty)
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
                        dictionary.AddOrSkip(config.ReservedKeywordParent, input);
                    }
                    return ret;
                }
            }
            return null;
        }

        public static TextHolder GetHolder(this IDictionary<string, object> input, string key, TemplatorConfig config)
        {
            TextHolder holder = null;
            var holders = input.GetOrDefault(config.KeyHolders) as IDictionary<string, TextHolder>;
            if (holders != null && holders.ContainsKey(key))
            {
                holder = holders[key];
            }
            return holder ?? new TextHolder(key);
        }

        public static bool InXmlManipulation(this TemplatorParser parser)
        {
            return parser.XmlContext != null && parser.XmlContext.Attribute != null &&
                   parser.XmlContext.Attribute.Name == parser.Config.XmlReservedAttributeName;
        }
    }
}
