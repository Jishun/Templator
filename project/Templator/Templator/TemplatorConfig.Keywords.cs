﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using CsvEnumerator;
using DotNetUtils;

namespace Templator
{
    public partial class TemplatorConfig
    {
        public void PrepareKeywords()
        {
            Keywords = (new List<TemplatorKeyword>()
            {
                //Structure keywords
                new TemplatorKeyword(KeywordRepeat){
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) => value == null ? null : parser.InXmlManipulation() ? value : String.Empty, 
                    PostParse = (parser, parsedHolder) =>
                    {
                        Keywords[KeywordRepeatBegin].PostParse(parser, parsedHolder);
                        if (parser.InXmlManipulation())
                        {
                            Keywords[KeywordRepeatEnd].PostParse(parser, parsedHolder);
                        }
                        return false;
                    } 
                },
                new TemplatorKeyword(KeywordRepeatBegin){
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) => value == null ? null : parser.InXmlManipulation() ? value : String.Empty, 
                    PostParse = (parser, parsedHolder) =>
                    {
                        var childInputs = TemplatorUtil.GetChildCollection(parser.Context.Input, parsedHolder.Name, parser.Config);
                        IDictionary<string, object> input = null;
                        var l = parser.StackLevel + parsedHolder.Name;
                        var inputIndex = (int?)parser.Context[l + "InputIndex"] ?? 0;
                        var inputCount = 0;
                        var noOutput = false;
                        if (childInputs.IsNullOrEmpty())
                        {
                            noOutput = parsedHolder.IsOptional();
                            parser.Context[l + "InputCount"] = 0;
                            parser.Context[l + "InputIndex"] = inputIndex;
                            input = parser.Context.Input == null ? null : new Dictionary<string, object>() { { ReservedKeywordParent, parser.Context.Input } };
                        }
                        else
                        {
                            if (inputIndex < childInputs.Length)
                            {
                                input = childInputs[inputIndex++];
                            }
                            else
                            {
                                noOutput = true;
                                input = new Dictionary<string, object>() { { ReservedKeywordParent, parser.Context.Input } };
                            }
                            parser.Context[l + "InputIndex"] = inputIndex;
                            if (parser.Context[l + "InputCount"] == null || !(parsedHolder.ContainsKey(KeywordLength) || parsedHolder.ContainsKey(KeywordAlignCount)))
                            {
                                inputCount = childInputs.Length;
                                parser.Context[l + "InputCount"] = inputCount;
                            }
                            else
                            {
                                inputCount = (int) parser.Context[l + "InputCount"];
                            }
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
                                    var element = new XElement(p.XmlContext.ElementList[p.XmlContext.ElementIndex]);
                                    if ((string)parsedHolder[KeywordRepeatBegin] == "Group")
                                    {
                                        parser.XmlContext.Element.Add(element);
                                    }
                                    else
                                    {
                                        p.XmlContext.ElementList[p.XmlContext.ElementIndex].AddAfterSelf(element);
                                    }
                                    p.XmlContext.ElementList[p.XmlContext.ElementIndex] = element;
                                };
                                var newElement = new XElement(parser.ParentXmlContext.ElementList[parser.ParentXmlContext.ElementIndex]);
                                if ((string)parsedHolder[KeywordRepeatBegin] == "Group")
                                {
                                    parser.ParentXmlContext.Element.Add(newElement);
                                }
                                else
                                {
                                    parser.ParentXmlContext.ElementList[parser.ParentXmlContext.ElementIndex].InsertElementAfter(newElement);
                                }
                                parser.ParentXmlContext.ElementList[parser.ParentXmlContext.ElementIndex] = newElement;
                            }
                        }

                        parser.PushContext(input, parsedHolder, parsedHolder.ContainsKey(KeywordHolder) || inputCount == 0, noOutput);
                        parser.Context["ParentPosition"] = parsedHolder.Position;
                        return false;
                    } 
                },
                new TemplatorKeyword(KeywordRepeatEnd){
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
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
                                    if ((string)parsedHolder[KeywordRepeatEnd] == "Group")
                                    {
                                        for (var i = p.XmlContext.ElementIndex+1; i < p.XmlContext.ElementList.Count; i++)
                                        {
                                            p.XmlContext.ElementList[i].MoveLast();
                                        }
                                    }
                                }
                                p.XmlContext.OnAfterParsingElement = null;
                            };
                            parser.ParentXmlContext.OnBeforeParsingElement = null;
                        }
                        else
                        {
                            var position = (int)parser.Context["ParentPosition"];
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
                //Calculated Keywords
                new TemplatorKeyword(KeywordSeekup)
                {
                    CalculateInput = true,
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (value == null)
                        {
                            var level = holder[KeywordSeekup].ParseDecimalNullable() ?? 0;
                            var input = parser.Context.Input;
                            while (level-- > 0 && input != null)
                            {
                                if (!input.ContainsKey(ReservedKeywordParent))
                                {
                                    break;
                                }
                                input = (IDictionary<string, object>) input[ReservedKeywordParent];
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
                        parser.ParsingHolder[KeywordSeekup] = str.ParseIntParam(1);
                    })
                },
                new TemplatorKeyword(KeywordJs)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyword(KeywordMathMax)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeywordMathMax];
                        return parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, num) => Math.Max((num.ParseDecimalNullable() ?? 0) , (agg.ParseDecimalNullable() ?? 0))).DecimalToString();
                    }
                },
                new TemplatorKeyword(KeywordMathMin)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeywordMathMin];
                        return parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, num) => Math.Min((num.ParseDecimalNullable() ?? 0) , (agg.ParseDecimalNullable() ?? 0))).DecimalToString();
                    }
                },
                new TemplatorKeyword(KeywordSum)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeywordSum];
                        return parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 0) + (agg.ParseDecimalNullable() ?? 0)).DecimalToString();
                    }
                },
                new TemplatorKeyword(KeywordAverage)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeywordAverage];
                        // sum/count
                        var count = (decimal)parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, item) => (agg.ParseDecimalNullable() ?? 0) + (item is object[] ? ((object[])item).Length : 1));
                        return count != 0 ? (decimal)parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 0) + (agg.ParseDecimalNullable() ?? 0)) / count : 0;
                    }
                },
                new TemplatorKeyword(KeywordCount)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string) holder[KeywordCount];
                        return parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, item) => (agg.ParseDecimalNullable() ?? 0) + (item is object[] ? ((object[])item).Length : 1)).DecimalToString();
                    }
                },
                new TemplatorKeyword(KeywordMulti)
                {
                    HandleNullOrEmpty = true,
                    CalculateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var aggregateField = (string)holder[KeywordMulti];
                        return parser.Aggregate(null, holder, aggregateField, parser.Context.Input, (agg, num) => (num.ParseDecimalNullable() ?? 1) * (agg.ParseDecimalNullable() ?? 1)).DecimalToString();
                    }
                },
                //Modifying Input
                new TemplatorKeyword(KeywordRefer)
                {
                    HandleNullOrEmpty = true,
                    ManipulateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var refer = (string)holder[KeywordRefer];
                        return TemplatorUtil.GetValue(parser, refer, parser.Context.Input, null, (int?)holder[KeywordSeekup] ?? 0);
                    },
                    Parse = (parser, str) =>
                    {
                        if (str.IsNullOrWhiteSpace())
                        {
                            throw new TemplatorParamsException();
                        }
                        parser.ParsingHolder[KeywordRefer] = str;
                    }
                },
                new TemplatorKeyword(KeywordEven)
                {
                    ManipulateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        return d.HasValue ? (object) decimal.Round(d.Value, MidpointRounding.ToEven) : null;
                    }
                },
                new TemplatorKeyword(KeywordAwayFromZero)
                {
                    ManipulateInput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        return d.HasValue ? (object) decimal.Round(d.Value, (int?)holder[KeywordAwayFromZero].ParseDecimalNullable() ?? 2, MidpointRounding.AwayFromZero) : null;
                    }
                },
                new TemplatorKeyword(KeywordDefault)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    ManipulateInput = true,
                    OnGetValue = (holder, parser, value) => value ?? holder[KeywordDefault],
                },
                new TemplatorKeyword(KeywordOptional)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => value ?? holder[KeywordOptional],
                },
                //Validation Keywords
                new TemplatorKeyword(KeywordRegex)
                {
                    IsValidation = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var regStr = (string) holder[KeywordRegex];
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
                    },
                    Parse = (parser, str) =>
                    {
                        parser.ParsingHolder[KeywordRegex] = str;
                    }
                },
                new TemplatorKeyword(KeywordLength)
                {
                    IsValidation = true,
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var isArray = holder.ContainsKey(KeywordRepeat) || holder.ContainsKey(KeywordRepeatBegin);
                        if (value.IsNullOrEmptyValue() && ! isArray)
                        {
                            return value;
                        }
                        string str = null;
                        str = holder.ContainsKey(KeywordNumber) ? Convert.ToString(value.DecimalToString() ?? value) : value.SafeToString();
                        var child = isArray ? TemplatorUtil.GetChildCollection(parser.Context.Input, holder.Name, parser.Config) : null;
                        var length = isArray ? child == null ? 0 : child.Length : str.Length;
                        int? maxLength = null;
                        var customLength = (Pair<string, IList<int>>)holder[KeywordLength];
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
                        if (maxLength.HasValue && holder.ContainsKey(KeywordTruncate))
                        {
                            if (isArray && child != null)
                            {
                                var l = parser.StackLevel + holder.Name;
                                parser.Context[l + "InputCount"] = maxLength.Value;
                            }
                            else
                            {
                                str = str.Substring(0, maxLength.Value);
                            }
                            return str;
                        }
                        parser.LogError("Invalid Field length: '{0}', value: {1}, valid length: {2}.", holder.Name, value, customLength.First);
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
                        parser.ParsingHolder[KeywordLength] = new Pair<string,IList<int>>(str, lengths);
                    })
                },
                //new TemplatorKeyword(KeywordSelect){},
                //new TemplatorKeyword(KeywordExpression){},
                new TemplatorKeyword(KeywordMin)
                {
                    IsValidation = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var min = (decimal)holder[KeywordMin];
                        if (holder.ContainsKey(KeywordNumber))
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
                        parser.ParsingHolder[KeywordMin] = str.ParseNumberParam();
                    })
                },
                new TemplatorKeyword(KeywordMax)
                {
                    IsValidation = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var max = (decimal) holder[KeywordMax];
                        if (holder.ContainsKey(KeywordNumber))
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
                        parser.ParsingHolder[KeywordMax] = str.ParseNumberParam();
                    })
                },
                //DataType/format Keywords
                new TemplatorKeyword(KeywordBit)
                {
                    IsValidation = true,
                    OnGetValue = (holder, parser, value) => value == null ? null : String.Empty
                },
                new TemplatorKeyword(KeywordEnum){
                    IsValidation = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var typeName = (string)holder[KeywordEnum];
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
                            parser.ParsingHolder[KeywordEnum] = str;
                        }
                        else
                        {
                            throw new TemplatorParamsException();
                        }
                    })
                },
                new TemplatorKeyword(KeywordNumber)
                {
                    IsValidation = true,
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDecimalNullable();
                        if (d == null)
                        {
                            parser.LogError("'{0}' is not a number in '{1}'", value, holder.Name);
                            return null;
                        }
                        var format = (string)holder[KeywordNumber];
                        return d.Value.ToString(format.IsNullOrWhiteSpace() ? "G29" : format);
                    }
                },
                new TemplatorKeyword(KeywordDateTime)
                {
                    IsValidation = true,
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var d = value.ParseDateTimeNullable();
                        if (d == null)
                        {
                            parser.LogError("'{0}' is not a valid DateTime in '{1}'", value, holder.Name);
                            return null;
                        }
                        var format = (string)holder[KeywordDateTime];
                        if (format.IsNullOrWhiteSpace())
                        {
                            return d;
                        }
                        return d.Value.ToString(format);
                    }
                },
                //Format Keywords
                new TemplatorKeyword(KeywordFormat)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (null == value)
                        {
                            return null;
                        }
                        var pattern = (string)holder[KeywordFormat];
                        return pattern.Format(value);
                    }
                },
                new TemplatorKeyword(KeywordMap)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var dict = (IDictionary<string, string>)holder[KeywordMap];
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
                        parser.ParsingHolder[KeywordMap] = ret;
                    }
                },
                new TemplatorKeyword(KeywordReplace)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (value != null)
                        {
                            var arr = (string[])holder[KeywordReplace];
                            return value.SafeToString().Replace(arr[0], arr[1]);
                        }
                        return null;
                    },
                    Parse = (parser, str) =>
                    {
                        var arr = str.Split(';');
                        if (arr.Length == 2)
                        {
                            parser.ParsingHolder[KeywordReplace] = arr;
                            return;
                        }
                        throw new TemplatorParamsException();
                    }
                },
                new TemplatorKeyword(KeywordTransform)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var str = value.SafeToString();
                        switch ((string)holder[KeywordTransform])
                        {
                            case "Lower":
                                return str.ToLower();
                            case "Upper":
                                return str.ToUpper();
                        }
                        return str;
                    }
                },
                new TemplatorKeyword(KeywordUpper)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        value = value.SafeToString().ToUpper();
                        return value;
                    }
                },
                new TemplatorKeyword(KeywordTrim)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var str = value.SafeToString();
                        var trim = (string)holder[KeywordTrim];
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
                new TemplatorKeyword(KeywordCsv)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => value.SafeToString().EncodeCsvField()
                },
                new TemplatorKeyword(KeywordBase32)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => Base32.ToBase32String(parser.Config.Encoding.GetBytes(value.SafeToString()))
                },
                new TemplatorKeyword(KeywordBase64)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => Convert.ToBase64String(parser.Config.Encoding.GetBytes(value.SafeToString()))
                },
                new TemplatorKeyword(KeywordUrl)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => HttpUtility.UrlEncode(value.SafeToString())
                },
                new TemplatorKeyword(KeywordHtml)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => HttpUtility.HtmlEncode(value.SafeToString())
                },
                new TemplatorKeyword(KeywordEncode)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        switch ((string)holder[KeywordEncode])
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
                new TemplatorKeyword(KeywordDecode)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        switch ((string)holder[KeywordDecode])
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
                new TemplatorKeyword(KeywordRemoveChar)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) => value.SafeToString().RemoveCharacter((char[]) holder[KeywordRemoveChar]),
                    Parse = ((parser, str) =>
                    {
                        if (str.Length != 1)
                        {
                            throw new TemplatorParamsException();
                        }
                        parser.ParsingHolder[KeywordRemoveChar] = str.ToCharArray();
                    })
                },
                new TemplatorKeyword(KeywordFixedLength)
                {
                    HandleNullOrEmpty = true,
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (value != null)
                        {
                            var length = (int) holder[KeywordFixedLength];
                            return value.SafeToString()
                                .ToFixLength(length,
                                    (((string) holder[KeywordFill] ??
                                      (string) holder[KeywordFill] ?? (string) holder[KeywordPrefill]).NullIfEmpty() ??
                                     " ").First(),
                                    !holder.ContainsKey(KeywordPrefill));
                        }
                        return null;
                    },
                    Parse = ((parser, str) =>
                    {
                        parser.ParsingHolder[KeywordFixedLength] = str.ParseIntParam();
                    })
                },
                new TemplatorKeyword(KeywordHolder)
                {
                    ManipulateOutput = true,
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (parser.XmlContext != null && parser.XmlContext.Element != null)
                        {
                            parser.RemovingElements.Add(parser.XmlContext.Element);
                        }
                        return value == null ? null : String.Empty;
                    }
                },
                //document manupulation keywords
                new TemplatorKeyword(KeywordIfnot)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var ifQuery = (string)holder[KeywordIfnot];
                        if (value.IsNullOrEmptyValue())
                        {
                            parser.RemovingElements.Add(ifQuery.IsNullOrWhiteSpace()
                            ? parser.XmlContext.Element
                            : parser.XmlContext.Element.XPathSelectElement(ifQuery));
                        }
                        return value ?? String.Empty;
                    }
                },
                new TemplatorKeyword(KeywordEnumElement)
                {
                    Parse = (parser, str) =>
                    {
                        parser.ParsingHolder.Keywords.Add(Keywords[KeywordEnum].Create());
                        parser.ParsingHolder[KeywordEnum] = str;
                        parser.ParsingHolder.Keywords.Add(Keywords[KeywordElementName].Create());
                    }
                },
                new TemplatorKeyword(KeywordElementName)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        parser.XmlContext.Element.Name = parser.XmlContext.Element.Name.Namespace + value.SafeToString();
                        return String.Empty;
                    }
                },
                new TemplatorKeyword(KeywordAttributeThen)
                {
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyword(KeywordAttributeIfnot)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        throw new NotImplementedException();
                    }
                },
                new TemplatorKeyword(KeywordAttributeIf)
                {
                    IndicatesOptional = true,
                    HandleNullOrEmpty = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (parser.XmlContext != null && parser.XmlContext.Attribute != null)
                        {
                            var eav = value;
                            var name = (string)holder[KeywordAttributeIf];
                            if (!name.IsNullOrEmptyValue())
                            {
                                eav = TemplatorUtil.GetInputValue(parser, name, parser.Context.Input);
                                eav = eav ?? parser.RequireValue(parser, TemplatorUtil.GetHolder(parser.Context.Input, name, parser.Config));
                            }
                            if (eav.IsNullOrEmptyValue())
                            {
                                parser.XmlContext.Attribute.Remove();
                            }
                        }
                        return value ?? string.Empty;
                    }
                },
                new TemplatorKeyword(KeywordAttributeName)
                {
                    ManipulateOutput = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        if (value == null)
                        {
                            parser.LogError("{0} is required", holder.Name);
                            return null;
                        }
                        if (parser.XmlContext != null && parser.XmlContext.Attribute != null)
                        {
                            var element = parser.XmlContext.Element;
                            var attr = parser.XmlContext.Attribute;
                            parser.ParentXmlContext.OnAfterParsingElement = templatorParser =>
                            {
                                element.SetAttributeValue(value.SafeToString(), attr.Value);
                                if (attr.Parent != null)
                                {
                                    attr.Remove();
                                }
                            };
                        }
                        return String.Empty;
                    }
                },
                new TemplatorKeyword(KeywordThen)
                {
                    HandleNullOrEmpty = true,
                },
                new TemplatorKeyword(KeywordIf)
                {
                    HandleNullOrEmpty = true,
                    IndicatesOptional = true,
                    OnGetValue = (holder, parser, value) =>
                    {
                        var eav = value;
                        var name = (string) holder[KeywordIf];
                        if (!name.IsNullOrEmptyValue())
                        {
                            eav = TemplatorUtil.GetInputValue(parser, name, parser.Context.Input);
                            eav = eav ?? parser.RequireValue(parser, TemplatorUtil.GetHolder(parser.Context.Input, name, parser.Config));
                        }
                        if (eav.IsNullOrEmptyValue())
                        {
                            parser.RemovingElements.Add(parser.XmlContext.Element);
                        }
                        return value ?? string.Empty;
                    },
                },
                //discriptors
                new TemplatorKeyword(KeywordComments)
                {
                    Parse = ((parser, s) => parser.ParsingHolder[KeywordComments] = s)
                },
                new TemplatorKeyword(KeywordDisplayName){},
                //Keyword Expand
                new TemplatorKeyword(KeywordTruncate){},
                new TemplatorKeyword(KeywordFill){},
                new TemplatorKeyword(KeywordPrefill){},
                new TemplatorKeyword(KeywordAppend){},
                //Align minCount
                new TemplatorKeyword(KeywordAlignCount)
                {
                    Parse = ((parser, s) =>
                    {
                        if (s.IsNullOrWhiteSpace())
                        {
                            throw new TemplatorParamsException();
                        }
                        parser.ParsingHolder[KeywordAlignCount] = s;
                        var childInputs = TemplatorUtil.GetChildCollection(parser.ParentContext.Input, s, parser.Config);
                        if (!childInputs.IsNullOrEmpty())
                        {
                            var l = parser.StackLevel + parser.ParsingHolder.Name;
                            parser.Context[l + "InputCount"] = childInputs.Max(c =>
                            {
                                var child = TemplatorUtil.GetChildCollection(c, parser.ParsingHolder.Name, parser.Config);
                                return child == null ? 0 : child.Length;
                            });
                        }
                    })
                },
            }).ToDictionary(k => k.Name);
            var index = 1;
            foreach (var key in Keywords.Values)
            {
                key.Preority = key.Preority > 0 ? key.Preority : index += KeywordPriorityIncreamental;
            }
        }
    }
}
