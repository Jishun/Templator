using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using DotNetUtils;
using Newtonsoft.Json;

namespace Templator
{
    public static class TemplatorUtil
    {
        public static string LoadTextTemplate(this TemplatorParser parser, Stream stream, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null)
        {
            using (var rd = new StreamReader(stream))
            {
                return parser.ParseText(rd.ReadToEnd(), input, preparsedHolders, mergeHoldersInto);
            }
        }

        public static XElement LoadXmlTemplate(this TemplatorParser parser, Stream stream, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null, XmlSchemaSet schemaSet = null)
        {
            var doc = XDocument.Load(stream);
            return parser.ParseXml(doc, input, preparsedHolders, mergeHoldersInto, schemaSet);
        }

        public static string LoadCsvTemplate(this TemplatorParser parser, Stream stream, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null)
        {
            using (var rd = new StreamReader(stream))
            {
                return parser.ParseCsv(rd.ReadToEnd(), input, preparsedHolders, mergeHoldersInto);
            }
        }

        public static bool InXmlManipulation(this TemplatorParser parser)
        {
            return parser.XmlContext != null && parser.XmlContext.Attribute != null &&
                   parser.XmlContext.Attribute.Name == parser.Config.XmlTemplatorAttributeName;
        }

        public static void GrammarCheckDirectory(this TemplatorParser parser, string path, string[] filters, int depth)
        {
            var files = filters.IsNullOrEmptyValue()
                ? Directory.EnumerateFiles(path)
                : Directory.EnumerateFiles(path).Where(f => filters.Any(fi => f.EndsWith(fi, StringComparison.OrdinalIgnoreCase)));
            foreach (var name in files)
            {
                using (var sr = new StreamReader(name))
                {
                    parser.GrammarCheck(sr.ReadToEnd(), name);
                }
                if (parser.ReachedMaxError)
                {
                    return;
                }
            }
            if (depth-- > 0)
            {
                foreach (var d in Directory.EnumerateDirectories(path))
                {
                    parser.GrammarCheckDirectory(d, filters, depth);
                    if (parser.ReachedMaxError)
                    {
                        return;
                    }
                }
            }
        }

        public static IDictionary<string, TextHolder> MergeHolders(IDictionary<string, TextHolder> holders, IEnumerable<TextHolder> addition, string mergeinto = null)
        {
            if (holders == null || addition == null)
            {
                return holders ?? addition.ToDictionary(a => a.Name);
            }
            var ret = holders;
            if (mergeinto != null)
            {
                var into = holders.GetOrDefault(mergeinto);
                if (@into == null)
                {
                    throw new TemplatorException("Merging into non-exist holder");
                }
                holders = @into.Children;
            }
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
                        MergeHolders(existing.Children, textHolder.Children.Values);
                    }
                }
                else
                {
                    throw new TemplatorException("Repeat Item redefined differently");
                }
            }
            return ret;
        }

        public static IDictionary<string, object> MergeInput(IEnumerable<string> inputs, string fieldName, TemplatorConfig config)
        {
            if (inputs != null)
            {
                var ret = inputs.Select(ParseJsonDict).ToList();
                if (ret.Count > 0)
                {
                    return MergeInput(ret, fieldName, config);
                }
            }
            return null;
        }

        public static IDictionary<string, object> MergeInput(IList<IDictionary<string, object>> inputs, string fieldName, TemplatorConfig config)
        {
            if (inputs != null && inputs.Count > 0)
            {
                var ret = inputs[0];
                var list = GetChildCollection(ret, fieldName, config).EmptyIfNull().ToList();
                for (var i = 1; i < inputs.Count; i++)
                {
                    var subList = GetChildCollection(inputs[i], fieldName, config);
                    if (subList != null)
                    {
                        list.AddRange(subList);
                    }
                }
                ret[fieldName] = list.ToArray();
                return ret;
            }
            return null;
        }
        
        public static string InputToJson(this IDictionary<string, object> input)
        {
            var se = JsonSerializer.Create(new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            using (var wr = new StringWriter())
            {
                se.Serialize(wr, input);
                return wr.ToString();
            }
        }

        public static IDictionary<string, object> ParseJsonDict(this string inputStr)
        {
            var se = new JavaScriptSerializer();
            return se.Deserialize<IDictionary<string, object>>(inputStr);
        }

        public static IDictionary<string, object> ParseJsonDict(this Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd().ParseJsonDict();
            }
        }

        public static string XmlToJson(string inputStr, TemplatorConfig config)
        {
            var xml = XElement.Parse(inputStr);
            var input = ParseXmlDict(xml.Elements(), config);
            return InputToJson(input);
        }

        public static IDictionary<string, object> ParseXmlDict(IEnumerable<XElement> elements, TemplatorConfig config)
        {
            IDictionary<string, object> ret = new Dictionary<string, object>();
            foreach (var e in elements)
            {
                if (e.Name == config.XmlFieldElementName)
                {
                    var nameNode = e.Element(config.XmlNameNodeName);
                    if (nameNode != null)
                    {
                        var collection = e.Elements(config.XmlCollectionNodeName).ToArray();
                        if (collection.Length > 0)
                        {
                            ret.Add(nameNode.Value, collection.Select(item => ParseXmlDict(item.Elements(config.XmlFieldElementName), config)).ToArray());
                        }
                        else
                        {
                            var valueNode = e.Element(config.XmlValueNodeName);
                            if (valueNode != null)
                            {
                                ret.Add(nameNode.Value, valueNode.Value);
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public static IDictionary<string, object>[] GetChildCollection(IDictionary<string, object> input, string key, TemplatorConfig config)
        {
            if (input != null && input.ContainsKey(key))
            {
                var retArray = input[key] as object[];
                if (retArray != null)
                {
                    var ret = retArray.Where(r => r is IDictionary<string, object>).Cast<IDictionary<string, object>>().ToArray();
                    for (var i = 0; i < ret.Length; i++)
                    {
                        ret[i].AddOrOverwrite(config.ReservedKeywordParent, input);
                        ret[i].AddOrOverwrite(config.ReservedKeywordIndex, i + 1);
                        ret[i].AddOrOverwrite(config.ReservedKeyword0Index, i);
                    }
                    return ret;
                }
            }
            return null;
        }

        public static object Aggregate(this TemplatorParser parser, object current, TextHolder holder, string aggregateField, IDictionary<string, object> input, Func<object, object, object> aggregateFunc)
        {
            foreach (var c in aggregateField.Split(Constants.SemiDelimChar))
            {
                string left = null;
                var fieldName = c.GetUntil(".", out left);
                if (!left.IsNullOrWhiteSpace())
                {
                    var list = GetChildCollection(input, fieldName, parser.Config);
                    current = list.EmptyIfNull().Aggregate(current, (current1, subInput) => parser.Aggregate(current1, holder, left, subInput, aggregateFunc));
                }
                else
                {
                    var value = GetUnformmatedValue(parser, fieldName, input);
                    current = aggregateFunc(current, value);
                }
            }
            return current;
        }

        public static object GetUnformmatedValue(TemplatorParser parser, string fieldName, IDictionary<string, object> input, int seekUp = 0, bool requireInput = true)
        {
            var value = GetInputValue(parser, fieldName, input, null, seekUp);
            var subHolder = GetHolder(input, fieldName, parser.Config, false);
            if (value == null)
            {
                if (subHolder != null)
                {
                    value = subHolder.Keywords.EmptyIfNull().Where(k => k.CalculateInput && k.OnGetValue != null)
                        .Aggregate(value, (current, k) => KeywordPostParse(parser, subHolder, current, k));
                }
                if (requireInput)
                {
                    value = value ?? parser.RequireValue(parser, subHolder ?? new TextHolder(fieldName), null, input);
                }
            }
            if (subHolder != null)
            {
                value = subHolder.Keywords.EmptyIfNull().Where(k => k.ManipulateInput && k.OnGetValue != null)
                        .Aggregate(value, (current, k) => KeywordPostParse(parser, subHolder, current, k));
            }
            return value;
        }

        public static object GetInputValue(TemplatorParser parser, string key, IDictionary<string, object> input, object defaultRet = null, int seekup = 0)
        {
            var i = input;
            var v = i.GetOrDefault(key, defaultRet);
            while (v == null && seekup-- > 0 && i.ContainsKey(parser.Config.ReservedKeywordParent))
            {
                i = (IDictionary<string, object>)i[parser.Config.ReservedKeywordParent];
                v = GetInputValue(parser, key, i, defaultRet);
            }
            return v;
        }

        public static object GetValue(TemplatorParser parser, string holderName, IDictionary<string, object> input, object defaultRet, int inherited = 0)
        {
            var holder = GetHolder(input, holderName, parser.Config);
            if (inherited > 0)
            {
                holder[parser.Config.KeywordSeekup] = inherited;
                holder.Keywords.Add(parser.Config.Keywords[parser.Config.KeywordSeekup].Create());
            }
            return GetValue(parser, holder, input, defaultRet);
        }

        public static object GetValue(TemplatorParser parser, TextHolder holder, IDictionary<string, object> input, object defaultRet)
        {
            object value = null;
            if (input != null && input.ContainsKey(holder.Name))
            {
                value = input[holder.Name];
            }
            if (value == null)
            {
                value = holder.Keywords.EmptyIfNull().Where(k => k.CalculateInput && k.OnGetValue != null)
                        .Aggregate(value, (current, k) => KeywordPostParse(parser, holder, current, k));
            }
            value = value ?? parser.RequireValue(parser, holder, defaultRet, input);
            value = holder.Keywords.EmptyIfNull().Where(key => !key.CalculateInput && key.OnGetValue != null)
                .Aggregate(value, (current, k) => KeywordPostParse(parser, holder, current, k));
            if (null == value)
            {
                if (defaultRet != null)
                {
                    value = defaultRet;
                }
                else
                {
                    parser.LogError("'{0}' is required", holder.Name);
                }
            }
            return value;
        }


        public static TextHolder GetHolder(IDictionary<string, object> input, string key, TemplatorConfig config, bool creatIfNoFound = true)
        {
            TextHolder holder = null;
            var holders = input.GetOrDefault(config.KeyHolders) as IDictionary<string, TextHolder>;
            if (holders != null && holders.ContainsKey(key))
            {
                holder = holders[key];
            }
            return holder ?? (creatIfNoFound ? new TextHolder(key) : null);
        }

        public static void SetInputCount(TemplatorParser parser, TextHolder holder, int? count)
        {
            var key = parser.StackLevel + holder.Name + "InputCount";
            parser.Context[key] = count;
        }

        public static void SetParentInputCount(TemplatorParser parser, TextHolder holder, int? count)
        {
            if (parser.ParentContext == null)
            {
                return;
            }
            var key = (parser.StackLevel - 1) + holder.Name + "InputCount";
            parser.ParentContext[key] = count;
        }

        public static int? GetInputCount(TemplatorParser parser, TextHolder holder)
        {
            var key = parser.StackLevel + holder.Name + "InputCount";
            return (int?)parser.Context[key];
        }

        public static int? GetParentInputCount(TemplatorParser parser, TextHolder holder)
        {
            if (parser.ParentContext == null)
            {
                return null;
            }
            var key = (parser.StackLevel - 1) + holder.Name + "InputCount";
            return (int?)parser.ParentContext[key];
        }

        public static void SetInputIndex(TemplatorParser parser, TextHolder holder, int? index)
        {
            var key = parser.StackLevel + holder.Name + "InputIndex";
            parser.Context[key] = index;
        }

        public static void SetParentInputIndex(TemplatorParser parser, TextHolder holder, int? index)
        {
            if (parser.ParentContext == null)
            {
                return;
            }
            var key = (parser.StackLevel - 1) + holder.Name + "InputIndex";
            parser.ParentContext[key] = index;
        }

        public static int? GetInputIndex(TemplatorParser parser, TextHolder holder)
        {
            var key = parser.StackLevel + holder.Name + "InputIndex";
            return (int?)parser.Context[key];
        }

        public static int? GetParentInputIndex(TemplatorParser parser, TextHolder holder)
        {
            if (parser.ParentContext == null)
            {
                return null;
            }
            var key = (parser.StackLevel -1) + holder.Name + "InputIndex";
            return (int?)parser.ParentContext[key];
        }

        private static object KeywordPostParse(TemplatorParser parser, TextHolder holder, object current, TemplatorKeyword key)
        {
            if (current.IsNullOrEmptyValue() && !key.HandleNullOrEmpty)
            {
                return current;
            }
            var ret = key.OnGetValue(holder, parser, current);
            if (((key.ManipulateInput && parser.Config.SaveManipulatedResults) 
                || (key.CalculateInput && parser.Config.CacheCalculatedResults)) && !key.IndicatesOptional)
            {
                parser.CacheValue(holder.Name, ret, true);
            }
            return ret;
        }
    }
}
