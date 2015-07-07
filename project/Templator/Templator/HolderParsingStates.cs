using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CsvEnumerator;
using DotNetUtils;

namespace Templator
{
    public static class HolderParsingStates
    {
        public static IDictionary<HolderParseState, Func<TemplatorParser, TextHolder>> States = new Dictionary<HolderParseState, Func<TemplatorParser, TextHolder>>()
        {
            {
                new HolderParseState(), parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.Begin);
                    if (matched == parser.Config.Begin)
                    {
                        if (parser.ParsingHolder != null)
                        {
                            parser.State.Error = true;
                            throw new TemplatorOverlappedTextHolderException();
                        }
                        parser.ParsingHolder = new TextHolder(){Position = parser.Context.Text.Position - parser.Config.Begin.Length};
                    }
                    parser.Context.Result.Append(str);
                    parser.State.Begin = true;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamBegin, parser.Config.KeywordsBegin, parser.Config.End);
                    if (matched == null)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    if (matched == parser.Config.ParamBegin)
                    {
                        parser.ParsingHolder.Category = str;
                        parser.State.Category = true;
                    }
                    else
                    {
                        parser.ParsingHolder.Name = str.Replace(" ", "");
                        parser.State.Name = true;
                        if (matched == parser.Config.KeywordsBegin)
                        {
                            parser.State.KeyWordsBegin = true;
                        }
                        else if (matched == parser.Config.End)
                        {
                            parser.State.End = true;
                        }
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Category = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamEnd);
                    if (matched == null)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    parser.ParsingHolder.Name = str.Replace(" ", "");
                    parser.State.Name = true;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.KeywordsBegin, parser.Config.End);
                    if (matched == null || str != String.Empty)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    if (matched == parser.Config.KeywordsBegin)
                    {
                        parser.State.KeyWordsBegin = true;
                    }
                    else
                    {
                        parser.State.End = true;
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeyWordsBegin = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamBegin, parser.Config.Delimiter, parser.Config.KeywordsEnd);
                    if (matched == null)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    str = str.Trim();
                    if (str != String.Empty)
                    {
                        if (parser.Config.KeyWords.ContainsKey(str))
                        {
                            if (parser.ParsingKeyword != null)
                            {
                                parser.State.Error = true;
                                throw new TemplatorSyntaxException();
                            }
                            parser.ParsingKeyword = parser.Config.KeyWords[str].Create();
                            parser.ParsingHolder.KeyWords = parser.ParsingHolder.KeyWords ?? new List<TemplatorKeyWord>();
                            parser.ParsingHolder.KeyWords.Add(parser.ParsingKeyword);
                        }
                        else
                        {
                            if (!parser.Config.IgnoreUnknownKeyword)
                            {
                                parser.State.Error = true;
                                throw new TemplatorUnexpecetedKeywordException();
                            }
                        }
                    }
                    if (matched == parser.Config.ParamBegin)
                    {
                        parser.State.KeywordParamBegin = true;
                    }
                    else if(matched == parser.Config.Delimiter)
                    {
                        ParseKeywordParam(parser, String.Empty);
                        parser.ParsingKeyword = null;
                    }
                    else if (matched == parser.Config.KeywordsEnd)
                    {
                        ParseKeywordParam(parser, String.Empty);
                        parser.State.KeywordsEnd = true;
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeyWordsBegin = true, KeywordParamBegin = true, }, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamEnd);
                    if (matched == null)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    if (!(parser.Config.IgnoreUnknownKeyword && parser.ParsingKeyword == null))
                    {
                        ParseKeywordParam(parser, str);
                    }
                    parser.ParsingKeyword = null;
                    parser.State.KeywordParamBegin = false;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeyWordsBegin = true, KeywordsEnd = true, }, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.End);
                    if (matched == null || str != String.Empty)
                    {
                        parser.State.Error = true;
                        throw new TemplatorSyntaxException();
                    }
                    parser.State.End = true;
                    return null;
                }
            },
            {
                new HolderParseState(){ End = true}, parser =>
                {
                    var ret = parser.ParsingHolder;
                    parser.ParsingHolder = null;
                    parser.ParsingKeyword = null;
                    parser.State = new HolderParseState();
                    var value = parser.GetValue(ret, parser.Context.Input);
                    var notSKip = ret.KeyWords.EmptyIfNull().Where(key => key.PostParse != null).Aggregate(true, (current, key) => current & key.PostParse(parser, ret));
                    parser.Context.Result.Append(parser.Csv ? value.SafeToString().EncodeCsvField() : value);
                    return notSKip? ret : null;
                }
            }
        };


        private static void ParseKeywordParam(TemplatorParser parser, string str)
        {
            if (parser.ParsingKeyword != null)
            {

                if (parser.ParsingKeyword.Parse != null)
                {
                    parser.ParsingKeyword.Parse(parser, str);
                }
                else
                {
                    if (!str.IsNullOrEmpty())
                    {
                        if (!parser.Config.IgnoreUnknownParam)
                        {
                            parser.State.Error = true;
                            throw new TemplatorParamsException();
                        }
                    }
                    parser.ParsingHolder[parser.ParsingKeyword.Name] = str.EmptyIfNull();
                }
            }
        }
    }
}
