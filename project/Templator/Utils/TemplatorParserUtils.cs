using System;
using System.Collections.Generic;
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
        public static IDictionary<string, object> MergeInput(IEnumerable<string> inputs, string fieldName, TemplatorConfig config)
        {
            if (inputs != null)
            {
                var ret = inputs.Select(ParseJsonDict).ToList();
                if (ret.Count > 0)
                {
                    return MergeInput(ret, fieldName , config);
                }
            }
            return null;
        }

        public static IDictionary<string, object> MergeInput(IList<IDictionary<string, object>> inputs, string fieldName, TemplatorConfig config)
        {
            if (inputs != null && inputs.Count > 0)
            {
                var ret = inputs[0];
                var list = GetCollectionInput(ret, fieldName, config).EmptyIfNull().ToList();
                for (var i = 1; i < inputs.Count; i++)
                {
                    var subList = GetCollectionInput(inputs[i], fieldName, config);
                    if (subList != null)
                    {
                        list.AddRange(subList);
                    }
                }
                ret[fieldName]= list.ToArray();
                return ret;
            }
            return null;
        }

        public static void MergeField(this IDictionary<string, TextHolder> fields, IEnumerable<TextHolder> addition, string mergeinto = null)
        {
            if (mergeinto != null && fields.ContainsKey(mergeinto))
            {
                var into = fields[mergeinto].Children;
                if (into != null)
                {
                    foreach (var field in addition)
                    {
                        if (into.All(f => f.Value.Name != field.Name))
                        {
                            into.Add(field.Name, field);
                        }
                    }
                }
            }
            else
            {
                foreach (var a in addition)
                {
                    if (fields.ContainsKey(a.Name) && fields[a.Name].Children != null)
                    {
                        MergeFieldCollection(fields[a.Name].Children, a.Children.Values);
                    }
                    else
                    {
                        fields.AddOrSkip(a.Name, a);
                    }
                }
            }
        }

        public static void MergeFieldCollection(IDictionary<string, TextHolder> list, IEnumerable<TextHolder> addition)
        {
            if (list == null || addition == null)
            {
                return;
            }
            foreach (var field in addition)
            {
                var existing = list.GetOrDefault(field.Name);
                if (list.ContainsKey(field.Name) )
                {
                    list.Add(field.Name, field);
                }
                else
                {
                    MergeFieldCollection(existing.Children, field.Children.Values);
                }
            }
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

        public static string XmlToJson(this string inputStr, TemplatorConfig config)
        {
            var xml = XElement.Parse(inputStr);
            var input = ParseXmlDict(xml.Elements(), config);
            return InputToJson(input);
        }

        public static IDictionary<string, object> ParseJsonDict(this Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd().ParseJsonDict();
            }
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

        public static IList<IDictionary<string, object>> GetCollectionInput(this IDictionary<string, object> input, string key, TemplatorConfig config)
        {
            if (input != null && input.ContainsKey(key))
            {
                var arr = input[key] as object[];
                if (arr != null)
                {
                    var ret = arr.Cast<IDictionary<string, object>>().ToList();
                    foreach (var item in ret)
                    {
                        item[config.ReservedKeyWordParent] = input;
                    }
                    return ret;
                }
            }
            return null;
        }

        public static string GetString(this IDictionary<string, object> input, TextHolder field, ILogger logs, TemplatorConfig config)
        {
            return input.GetOrDefault(field.Name, string.Empty).ToString();
        }

        public static TextHolder GetField(this IDictionary<string, object> input, string key, TemplatorConfig config)
        {
            TextHolder field = null;
            var fields = input.GetOrDefault(config.KeyFields) as IDictionary<string, TextHolder>;
            if (fields != null && fields.ContainsKey(key))
            {
                field = fields[key];
            }
            return field ?? new TextHolder(key);
        }

        public static object GetValue(string key, IDictionary<string, object> input, ILogger log, object defaultRet, Func<TextHolder, IDictionary<string, object>, ILogger, object> additionalSource = null, string aggregateField = null, int? inherited = null)
        {
            return input.GetOrDefault(key, defaultRet);
        }

        public static object GetValue(TextHolder field, IDictionary<string, object> input, ILogger log, object defaultRet, Func<TextHolder, IDictionary<string, object>, ILogger, object> additionalSource = null, string aggregateField = null, int inherited = 0)
        {
            return input.GetOrDefault(field.Name, defaultRet);
        }

        public static IDictionary<string, TextHolder> LoadTemplate(this TemplatorParser parser, Stream stream, IDictionary<string, object> input)
        {
            using (var rd = new StreamReader(stream))
            {
                parser.ParseText(rd.ReadToEnd(), input);
                return parser.Context.Holders;
            }
        }
        public static bool InXmlManipulation(this TemplatorParser parser)
        {
            return parser.XmlContext != null && parser.XmlContext.Attribute != null &&
                   parser.XmlContext.Attribute.Name == parser.Config.XmlReservedAttributeName;
        }
    }
}
