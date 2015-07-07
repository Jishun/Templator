using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using CsvEnumerator;
using DotNetUtils;

namespace Templator
{
    public partial class TemplatorConfig
    {
        public void PrepareKeywords()
        {
            KeyWords = (new List<TemplatorKeyWord>()
            {
                //Structure keywords
                new TemplatorKeyWord(KeyWordRepeat){
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) => value == null ? null : parser.InXmlManipulation() ? value : String.Empty, 
                    PostParse = (parser, parsedHolder) =>
                    {
                        KeyWords[KeyWordRepeatBegin].PostParse(parser, parsedHolder);
                        if (parser.InXmlManipulation())
                        {
                            KeyWords[KeyWordRepeatEnd].PostParse(parser, parsedHolder);
                        }
                        return false;
                    } 
                },
                new TemplatorKeyWord(KeyWordRepeatBegin){
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) => value == null ? null : parser.InXmlManipulation() ? value : String.Empty, 
                    PostParse = (parser, parsedHolder) =>
                    {
                        var childInputs = parser.Context.Input.GetChildCollection(parsedHolder.Name, parser.Config);
                        IDictionary<string, object> input = null;
                        var l = parser.StackLevel + parsedHolder.Name;
                        var inputIndex = (int?)parser.Context[l + "InputIndex"] ?? 0;
                        var inputCount = 0;
                        if (childInputs.IsNullOrEmpty())
                        {
                            parser.Context[l + "InputCount"] = 0;
                            parser.Context[l + "InputIndex"] = inputIndex;
                            input = parser.Context.Input == null ? null : new Dictionary<string, object>() { { ReservedKeyWordParent, parser.Context.Input } };
                        }
                        else
                        {
                            input = childInputs[inputIndex++];
                            parser.Context[l + "InputIndex"] = inputIndex;
                            parser.Context[l + "InputCount"] = inputCount = childInputs.Length;
                        }
                        parser.Context.Holders.AddOrSkip(parsedHolder.Name, parsedHolder);
                        if (parser.InXmlManipulation())
                        {
                            parser.ParentXmlContext.OnAfterParsingElement = null;
                            if (inputIndex < inputCount )
                            {
                                parser.ParentXmlContext[l + "XmlElementIndex"] = parser.ParentXmlContext.ElementIndex;

                                parser.ParentXmlContext.OnBeforeParsingElement = p =>
                                {
                                    var ll = p.StackLevel -1  + parsedHolder.Name;
                                    var startIndex = (int)p.XmlContext[ll + "XmlElementIndex"];
                                    startIndex += p.XmlContext.ElementIndex - startIndex;
                                    var element = new XElement(p.XmlContext.ElementList[startIndex]);
                                    p.XmlContext.ElementList[startIndex] = element;
                                    p.XmlContext.Element.Add(element);
                                };

                                var newElement = new XElement(parser.ParentXmlContext.ElementList[parser.ParentXmlContext.ElementIndex]);
                                parser.ParentXmlContext.ElementList[parser.ParentXmlContext.ElementIndex] = newElement;
                                parser.ParentXmlContext.Element.Add(newElement);
                            }
                        }
                        
                        parser.PushContext(input, parsedHolder, parsedHolder.IsOptional());
                        return false;
                    } 
                },
                new TemplatorKeyWord(KeyWordRepeatEnd){
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) => String.Empty,
                    PostParse = (parser, parsedHolder) =>
                    {
                        if (parser.InXmlManipulation())
                        {
                            parser.ParentXmlContext.OnAfterParsingElement = p =>
                            {
                                p.PopContext();
                                var ll = p.StackLevel + parsedHolder.Name;
                                if ((int)p.Context[ll + "InputCount"] > (int)p.Context[ll + "InputIndex"])
                                {
                                    p.XmlContext.ElementIndex = (int)p.XmlContext[ll + "XmlElementIndex"] - 1;
                                }
                                else
                                {
                                    p.Context[ll + "InputIndex"] = null;
                                    p.XmlContext.OnBeforeParsingElement = null;
                                }
                                p.XmlContext.OnAfterParsingElement = null;
                            };
                            parser.ParentXmlContext.OnBeforeParsingElement = null;
                        }
                        else
                        {
                            var position = parser.Context.ParentHolder.Position;
                            parser.PopContext();
                            var l = parser.StackLevel + parsedHolder.Name;
                            if ((int)parser.Context[l + "InputCount"] > (int)parser.Context[l + "InputIndex"])
                            {
                                parser.Context.Text.Position = position;
                            }
                            else
                            {
                                parser.Context[l + "InputIndex"] = null;
                            }
                        }
                        return false;
                    }
                },
                //Lookup Keywords
                new TemplatorKeyWord(KeyWordRefer)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var refer = (string)holder[KeyWordRefer];
                        return parser.GetValue(refer, parser.Context.Input);
                    },
                    Parse = (parser, str) =>
                    {
                        if (str.IsNullOrWhiteSpace())
                        {
                            throw new TemplatorParamsException();
                        }
                        parser.ParsingHolder[KeyWordRefer] = str;
                    }
                },
                new TemplatorKeyWord(KeyWordSeekup)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (value == null)
                        {
                            var level = holder[KeyWordSeekup].ParseDecimalNullable() ?? 0;
                            var input = parser.Context.Input;
                            while (level-- > 0 && input != null)
                            {
                                input = (IDictionary<string, object>) input[ReservedKeyWordParent];
                                if (input.ContainsKey(holder.Name))
                                {
                                    return input[holder.Name];
                                }
                            }
                        }
                        return value;
                    }, 
                    Parse = ((parser, str) =>
                    {
                        parser.ParsingHolder[KeyWordSeekup] = str.ParseIntParam(1);
                    })
                },
                new TemplatorKeyWord(KeyWordJs)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordSum)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeyWordSum];
                        return parser.Aggregate(holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 0) + (agg.ParseDecimalNullable(0) ?? 0)).DecimalToString();
                    }
                },
                new TemplatorKeyWord(KeyWordAverage)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeyWordAverage];
                        // sum/count
                        var count = (decimal)parser.Aggregate(holder, aggregateField, parser.Context.Input, (agg, item) => (agg.ParseDecimalNullable(0) ?? 0) + 1);
                        return count != 0 ? (decimal)parser.Aggregate(holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 0) + (agg.ParseDecimalNullable(0) ?? 0))/count : 0;
                    }
                },
                new TemplatorKeyWord(KeyWordCount)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string) holder[KeyWordCount];
                        return parser.Aggregate(holder, aggregateField, parser.Context.Input, (agg, item) => (agg.ParseDecimalNullable(0) ?? 0) + 1).DecimalToString();
                    }
                },
                new TemplatorKeyWord(KeyWordMulti)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeyWordMulti];
                        return parser.Aggregate(holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 1) * (agg.ParseDecimalNullable() ?? 1)).DecimalToString();
                    }
                },
                new TemplatorKeyWord(KeyWordOptional)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) => value ?? holder[KeyWordOptional],
                },
                //Validation Keywords
                new TemplatorKeyWord(KeyWordRegex)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var regStr = (string) holder[KeyWordRegex];
                        if (!regStr.IsNullOrWhiteSpace())
                        {
                            Regex reg = null;
                            var d = Regexes.ContainsKey(regStr);
                            reg = d ? Regexes[regStr] : new Regex(regStr, RegexOptions.Compiled | RegexOptions.CultureInvariant);
                            if (!reg.IsMatch(value.SafeToString()))
                            {
                                parser.LogError("Value '{0}' test failed against pattern '{1}' for field: '{2}'", value, d ? Regexes[regStr].ToString() : regStr, holder.Name);
                                return null;
                            }
                        }
                        else
                        {
                            throw new TemplatorParamsException("Invalid Regular expression defined");
                        }
                        return value;
                    }
                },
                new TemplatorKeyWord(KeyWordLength)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        string str = null;
                        str = holder.ContainsKey(KeyWordNumber) ? Convert.ToString(value.DecimalToString() ?? value) : value.SafeToString();   
                        var length = str.Length;
                        int? maxLength = null;
                        var customLength = (Pair<string, IList<int>>)holder[KeyWordLength];
                        if (customLength.Second[0] == -1)
                        {
                            maxLength = customLength.Second[2];
                            if (length < customLength.Second[1] || length > customLength.Second[2])
                            {
                                goto invalidLength;
                            }
                        }
                        else if (!customLength.Second.Contains(length))
                        {
                            if (customLength.Second.Count == 1)
                            {
                                maxLength = customLength.Second[0];
                            }
                            goto invalidLength;
                        }
                        return str;
                        invalidLength:
                        if (maxLength.HasValue && holder.ContainsKey(KeyWordTruncate))
                        {
                            str = str.Substring(0, maxLength.Value);
                            return str;
                        }
                        parser.LogError("Invalid Field length: '{0}', value: {1}, valid length: {2}.", "HolderName", value, customLength.First);
                        return null;
                    },
                    Parse = ((parser, str) =>
                    {
                        IList<int> lengths = null;
                        if (str.Contains("-"))
                        {
                            lengths = str.Split('-').Select(s => s.ParseIntNullable()).Where(i => i.HasValue && i > 0).Select(i => i.Value).ToList();
                            if (lengths.Count != 2)
                            {
                                throw new TemplatorParamsException("Invalid length defined");
                            }
                            lengths.Insert(0, -1);
                        }
                        else if(str.Contains(";"))
                        {
                            lengths = str.Split(';').Select(s => s.ParseIntNullable()).Where(i => i.HasValue && i > 0).Select(i => i.Value).ToList();
                            if (lengths.Count == 0)
                            {
                                throw new TemplatorParamsException("Invalid length defined");
                            }
                        }
                        else
                        {
                            var l = str.ParseIntNullable();
                            if (!l.HasValue)
                            {
                                throw new TemplatorParamsException("Invalid length defined");
                            }
                            lengths = l.Value.Single().ToList();
                        }
                        parser.ParsingHolder[KeyWordLength] = new Pair<string,IList<int>>(str, lengths);
                    })
                },
                //new TemplatorKeyWord(KeyWordSelect){},
                //new TemplatorKeyWord(KeyWordExpression){},
                new TemplatorKeyWord(KeyWordMin)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var min = (decimal)holder[KeyWordMin];
                        if (holder.ContainsKey(KeyWordNumber))
                        {
                            var num = value.ParseDecimalNullable();
                            if (num < min)
                            {
                                parser.LogError("'{0}' is not valid for min: '{1}' in field: '{2}'", value, min, holder.Name);
                                return null;
                            }
                            return value;
                        }
                        throw new TemplatorParamsException();
                    },
                    Parse = ((parser, str) =>
                    {
                        parser.ParsingHolder[KeyWordMin] = str.ParseNumberParam();
                    })
                },
                new TemplatorKeyWord(KeyWordMax)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var max = (decimal) holder[KeyWordMax];
                        if (holder.ContainsKey(KeyWordNumber))
                        {
                            var num = value.ParseDecimalNullable();
                            if (num > max)
                            {
                                parser.LogError("'{0}' is not valid for max: '{1}' in field: '{2}'", value, max, holder.Name);
                                return null;
                            }
                            return value;
                        }
                        throw new TemplatorParamsException();
                    },
                    Parse = ((parser, str) =>
                    {
                        parser.ParsingHolder[KeyWordMax] = str.ParseNumberParam();
                    })
                },
                //DataType/format Keywords
                new TemplatorKeyWord(KeyWordBit){OnGetValue = (holder, parser, value) => value == null ? null : String.Empty},
                new TemplatorKeyWord(KeyWordEnum){
                    OnGetValue = (holder, parser, value) =>
                    {
                        var typeName = (string)holder[KeyWordEnum];
                        if (Enum.IsDefined(Enums[typeName], value ?? String.Empty))
                        {
                            return value;
                        }
                        parser.LogError("'{0}' is not a value of enum '{1}' in '{2}'", value, typeName, holder.Name);
                        return null;
                    },
                    Parse =  ((parser, str) =>
                    {
                        if (!str.IsNullOrWhiteSpace() && Enums.ContainsKey(str))
                        {
                            parser.ParsingHolder[KeyWordEnum] = str;
                        }
                        else
                        {
                            throw new TemplatorParamsException();
                        }
                    })
                },
                new TemplatorKeyWord(KeyWordNumber)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        if (d == null)
                        {
                            parser.LogError("'{0}' is not a number in '{1}'", value, holder.Name);
                            return null;
                        }
                        var format = (string)holder[KeyWordNumber];
                        return d.Value.ToString(format.IsNullOrWhiteSpace() ? "G29" : format);
                    }
                },
                new TemplatorKeyWord(KeyWordDateTime)
                {

                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDateTimeNullable();
                        if (d == null)
                        {
                            parser.LogError("'{0}' is not a valid DateTime in '{1}'", value, holder.Name);
                            return null;
                        }
                        var format = (string)holder[KeyWordDateTime];
                        if (format.IsNullOrWhiteSpace())
                        {
                            return d;
                        }
                        return d.Value.ToString(format);
                    }
                },
                //Format Keywords
                new TemplatorKeyWord(KeyWordEven)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        return d.HasValue ? (object) decimal.Round(d.Value, MidpointRounding.ToEven) : null;
                    }
                },
                new TemplatorKeyWord(KeyWordAwayFromZero)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        return d.HasValue ? (object) decimal.Round(d.Value, (int?)holder[KeyWordAwayFromZero].ParseDecimalNullable() ?? 2, MidpointRounding.AwayFromZero) : null;
                    }
                },
                new TemplatorKeyWord(KeyWordFormat)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (null == value)
                        {
                            return null;
                        }
                        var pattern = (string)holder[KeyWordFormat];
                        return pattern.Format(value);
                    }
                },
                new TemplatorKeyWord(KeyWordMap)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var dict = (IDictionary<string, string>)holder[KeyWordMap];
                        var str = value.SafeToString();
                        if (dict.ContainsKey(str))
                        {
                            return dict[str];
                        }
                        parser.LogError("'{0}' unexpected input value of field '{1}'", value, holder.Name);
                        return null;
                    },
                    Parse = (parser, str) =>
                    {
                        var ret = new Dictionary<string, string>();
                        foreach (var item in str.Split(Constants.SemiDelimChar))
                        {
                            string value;
                            var key = item.GetUntil(":", out value);
                            ret.Add(key.Trim(), value.Trim());
                        }
                        parser.ParsingHolder[KeyWordMap] = ret;
                    }
                },
                new TemplatorKeyWord(KeyWordReplace)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    },
                    Parse = (parser, str) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordTransform)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var str = value.SafeToString();
                        switch ((string)holder[KeyWordTransform])
                        {
                            case "Lower":
                                return str.ToLower();
                            case "Upper":
                                return str.ToUpper();
                        }
                        return str;
                    }
                },
                new TemplatorKeyWord(KeyWordUpper)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        value = value.SafeToString().ToUpper();
                        return value;
                    }
                },
                new TemplatorKeyWord(KeyWordTrim)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        var str = value.SafeToString();
                        var trim = (string)holder[KeyWordTrim];
                        switch (trim)
                        {
                            case null:
                                return str;
                            case "Begin":
                                return str.TrimStart();
                            case "End":
                                return str.TrimEnd();
                            default:
                                return str.Trim();
                        }
                    }
                },
                new TemplatorKeyWord(KeyWordCsv)
                {
                    OnGetValue = (holder, parser, value) => value.SafeToString().EncodeCsvField()
                },
                new TemplatorKeyWord(KeyWordBase32)
                {
                    OnGetValue = (holder, parser, value) => Base32.ToBase32String(parser.Config.Encoding.GetBytes(value.SafeToString()))
                },
                new TemplatorKeyWord(KeyWordBase64)
                {
                    OnGetValue = (holder, parser, value) => Convert.ToBase64String(parser.Config.Encoding.GetBytes(value.SafeToString()))
                },
                new TemplatorKeyWord(KeyWordUrl)
                {
                    OnGetValue = (holder, parser, value) => HttpUtility.UrlEncode(value.SafeToString())
                },
                new TemplatorKeyWord(KeyWordHtml)
                {
                    OnGetValue = (holder, parser, value) => HttpUtility.HtmlEncode(value.SafeToString())
                },
                new TemplatorKeyWord(KeyWordEncode)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        switch ((string)holder[KeyWordEncode])
                        {
                            case "Base64":
                                return Base32.ToBase32String(parser.Config.Encoding.GetBytes(value.SafeToString()));
                            case "Base32":
                                return Convert.ToBase64String(parser.Config.Encoding.GetBytes(value.SafeToString()));
                            case "Html":
                                return HttpUtility.HtmlEncode(value.SafeToString());
                            case "Url":
                                return HttpUtility.UrlEncode(value.SafeToString());
                            case "Csv":
                                return value.SafeToString().EncodeCsvField();
                            default:
                                throw new TemplatorParamsException();
                        }
                    }
                },
                new TemplatorKeyWord(KeyWordDecode)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        switch ((string)holder[KeyWordDecode])
                        {
                            case "Base64":
                                return parser.Config.Encoding.GetString(Base32.FromBase32String(value.SafeToString()));
                            case "Base32":
                                return parser.Config.Encoding.GetString(Convert.FromBase64String(value.SafeToString()));
                            case "Html":
                                return HttpUtility.HtmlDecode(value.SafeToString());
                            case "Url":
                                return HttpUtility.UrlDecode(value.SafeToString());
                            case "Csv":
                                 return new SeekableString(value.SafeToString()).DecodeCsvField(true);
                            default:
                                throw new TemplatorParamsException();
                        }
                    }
                },
                new TemplatorKeyWord(KeyWordHolder)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (parser.XmlContext != null && parser.XmlContext.Element != null)
                        {
                            parser.RemovingElements.Add(parser.XmlContext.Element);
                        }
                        return value == null ? null : String.Empty;
                    }
                },
                new TemplatorKeyWord(KeyWordRemoveChar)
                {
                    OnGetValue = (holder, parser, value) => value.SafeToString().RemoveCharacter((char[]) holder[KeyWordRemoveChar]),
                    Parse = ((parser, str) =>
                    {
                        if (str.Length != 1)
                        {
                            throw new TemplatorParamsException();
                        }
                        parser.ParsingHolder[KeyWordRemoveChar] = str.ToCharArray();
                    })
                },
                new TemplatorKeyWord(KeyWordFixedLength)
                {
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var length = (int) holder[KeyWordFixedLength];
                        return value.SafeToString()
                            .ToFixLength(length, (((string)holder[KeyWordFill] ?? (string)holder[KeyWordFill] ?? (string)holder[KeyWordPrefill]).NullIfEmpty() ?? " ").First(),
                                !holder.ContainsKey(KeyWordPrefill));
                    },
                    Parse = ((parser, str) =>
                    {
                        parser.ParsingHolder[KeyWordFixedLength] = str.ParseIntParam();
                    })
                },
                //document manupulation keywords
                //new TemplatorKeyWord(KeyWordArrayEnd){},
                //new TemplatorKeyWord(KeyWordArray){},
                //new TemplatorKeyWord(KeyWordDefault){},
                new TemplatorKeyWord(KeyWordIfnot)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var ifQuery = (string)holder[KeyWordIfnot];
                        if (value.IsNullOrEmpty())
                        {
                            parser.RemovingElements.Add(ifQuery.IsNullOrWhiteSpace()
                            ? parser.XmlContext.Element
                            : parser.XmlContext.Element.XPathSelectElement(ifQuery));
                        }
                        return value ?? String.Empty;
                    }
                },
                new TemplatorKeyWord(KeyWordEnumElement)
                {
                    Parse = (parser, str) =>
                    {
                        parser.ParsingHolder.KeyWords.Add(KeyWords[KeyWordEnum].Create());
                        parser.ParsingHolder[KeyWordEnum] = str;
                        parser.ParsingHolder.KeyWords.Add(KeyWords[KeyWordElementName].Create());
                    }
                },
                new TemplatorKeyWord(KeyWordElementName)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        parser.XmlContext.Element.Name = parser.XmlContext.Element.Name.Namespace + value.SafeToString();
                        return String.Empty;
                    }
                },
                new TemplatorKeyWord(KeyWordAttributeThen)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordAttributeIfnot)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordAttributeIf)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordAttributeName)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyWord(KeyWordThen)
                {
                    HandleNullOrEmpty = true,
                },
                new TemplatorKeyWord(KeyWordIf)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var eav = value;
                        var name = (string) holder[KeyWordIf];
                        if (!name.IsNullOrEmpty())
                        {
                            eav = parser.GetValue(name, parser.Context.Input, (int?) holder[KeyWordSeekup] ?? 0);
                        }
                        if (eav.IsNullOrEmpty())
                        {
                            parser.RemovingElements.Add(parser.XmlContext.Element);
                        }
                        return value ?? string.Empty;
                    },
                },
                //discriptors
                new TemplatorKeyWord(KeyWordComments)
                {
                    Parse = ((parser, s) => parser.ParsingHolder[KeyWordComments] = s)
                },
                new TemplatorKeyWord(KeyWordDisplayName)
                {
                },
                new TemplatorKeyWord(KeyWordTruncate){},
                new TemplatorKeyWord(KeyWordFill){},
                new TemplatorKeyWord(KeyWordPrefill){},
                new TemplatorKeyWord(KeyWordAppend){},
            }).ToDictionary(k => k.Name);
            var index = 1;
            foreach (var key in KeyWords.Values)
            {
                key.Preority = key.Preority > 0 ? key.Preority : index+=10;
            }
        }
    }
}
