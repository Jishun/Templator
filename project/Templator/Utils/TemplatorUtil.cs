using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using DotNetUtils;
using Newtonsoft.Json;

namespace Templator
{
    public static class TemplatorUtil
    {
        public static IDictionary<string, TextHolder> LoadTemplate(this TemplatorParser parser, Stream stream, IDictionary<string, object> input)
        {
            using (var rd = new StreamReader(stream))
            {
                parser.ParseText(rd.ReadToEnd(), input);
                return parser.Context.Holders;
            }
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

        public static IDictionary<string, TextHolder> MergeHolders(this IDictionary<string, TextHolder> holders, IEnumerable<TextHolder> addition, string mergeinto = null)
        {
            if (holders == null || addition == null)
            {
                return holders ?? addition.ToDictionary(a => a.Name);
            }
            var ret = holders;
            if (mergeinto != null)
            {
                var into = holders.GetOrDefault(mergeinto);
                if (into == null)
                {
                    throw new TemplatorException("Merging into non-exist holder");
                }
                holders = into.Children;
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
                        existing.Children.MergeHolders(textHolder.Children.Values);
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
                var list = ret.GetChildCollection(fieldName, config).EmptyIfNull().ToList();
                for (var i = 1; i < inputs.Count; i++)
                {
                    var subList = inputs[i].GetChildCollection(fieldName, config);
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

        public static string XmlToJson(this string inputStr, TemplatorConfig config)
        {
            var xml = XElement.Parse(inputStr);
            var input = ParseXmlDict(xml.Elements(), config);
            return InputToJson(input);
        }

        public static IDictionary<string, object> ParseXmlDict(this IEnumerable<XElement> elements, TemplatorConfig config)
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
    }
}
