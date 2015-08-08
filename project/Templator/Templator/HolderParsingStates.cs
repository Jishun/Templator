using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Web.Instrumentation;
using CsvEnumerator;
using DotNetUtils;

namespace Templator
{
    public static class HolderParsingStates
    {
        public static IDictionary<HolderParseState, Func<TemplatorParser, TextHolder>> States = new Dictionary<HolderParseState, Func<TemplatorParser, TextHolder>>()
        {
            {
                new HolderParseState(){Error = true}, parser =>
                {
                    if (parser.Config.ContinueOnError && !parser.ReachedMaxError)
                    {
                        parser.State.Error = false;
                        parser.State.End = true;
                    }
                    else
                    {
                        throw new TemplatorUnexpecetedStateException();
                    }
                    return null;
                }
            },
            {
                new HolderParseState(), parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.Begin);
                    if (matched == parser.Config.Begin)
                    {
                        if (parser.ParsingHolder != null)
                        {
                            parser.LogSyntextError(parser.Config.SyntaxErrorOverLappedHolder);
                        }
                        else
                        {
                            parser.ParsingHolder = new TextHolder() { Position = parser.Context.Text.Position - parser.Config.Begin.Length };
                        }
                        parser.State.Begin = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    parser.AppendResult(str);
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.CategorizedNameBegin, parser.Config.KeywordsBegin, parser.Config.End);
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    else if (matched == parser.Config.CategorizedNameBegin)
                    {
                        parser.ParsingHolder.Category = str;
                        parser.State.Category = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermCategorizedNameBeginEnd);
                    }
                    else
                    {
                        if (!parser.Config.CategoryOptional)
                        {
                            parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                        }
                        parser.ParsingHolder.Name = str.Replace(" ", "");
                        parser.State.Name = true;
                        parser.OnGrammerTokenCreated(parser.ParsingHolder.Name, parser.Config.TermName, matched);
                        if (matched == parser.Config.End)
                        {
                            if (str.IsNullOrWhiteSpace())
                            {
                                parser.LogSyntextError(parser.Config.TermBeginEnd);
                            }
                            parser.State.End = true;
                            parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                        }
                        else if (matched == parser.Config.KeywordsBegin)
                        {
                            parser.State.KeywordsBegin = true;
                            parser.OnGrammerTokenCreated(matched, parser.Config.TermKeywordsBeginEnd);
                        }
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Category = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.CategorizedNameEnd, parser.Config.End);
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    else if (matched == parser.Config.End)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginNameTag);
                        parser.State.End = true;
                        parser.OnGrammerTokenCreated(parser.ParsingHolder.Name, parser.Config.TermName, matched);
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    else
                    {
                        parser.OnGrammerTokenCreated(str, parser.Config.TermName, matched);
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermCategorizedNameBeginEnd);
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
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    else if (str != String.Empty)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnexpecetedString);
                    }
                    else if (matched == parser.Config.KeywordsBegin)
                    {
                        parser.State.KeywordsBegin = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermKeywordsBeginEnd);
                    }
                    else
                    {
                        parser.State.End = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeywordsBegin = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamBegin, parser.Config.Delimiter, parser.Config.KeywordsEnd, parser.Config.End);
                    if (str == parser.Config.End)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedKeywordsBeginTag);
                        parser.State.End = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    else if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    str = str.Trim();
                    if (str != String.Empty)
                    {
                        if (parser.Config.Keywords.ContainsKey(str))
                        {
                            if (parser.ParsingKeyword != null)
                            {
                                if (!parser.Config.IgnoreUnknownParam)
                                {
                                    parser.LogSyntextError(parser.Config.SyntaxErrorUnexpecetedKeywordParam);
                                }
                            }
                            parser.ParsingKeyword = parser.Config.Keywords[str].Create();
                            parser.ParsingHolder.Keywords = parser.ParsingHolder.Keywords ?? new List<TemplatorKeyword>();
                            parser.ParsingHolder.Keywords.Add(parser.ParsingKeyword);
                            parser.OnGrammerTokenCreated(str, parser.Config.TermKeyword, matched);
                        }
                        else
                        {
                            if (!parser.Config.IgnoreUnknownKeyword)
                            {
                                parser.LogSyntextError(parser.Config.SyntaxErrorUnexpecetedKeyword);
                            }
                        }
                    }
                    if (matched == parser.Config.ParamBegin)
                    {
                        parser.State.KeywordParamBegin = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermParamBeginEnd);
                    }
                    else if(matched == parser.Config.Delimiter)
                    {
                        ParseKeywordParam(parser, String.Empty);
                        parser.ParsingKeyword = null;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermDelimiter);
                    }
                    else if (matched == parser.Config.KeywordsEnd)
                    {
                        ParseKeywordParam(parser, String.Empty);
                        parser.State.KeywordsEnd = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermKeywordsBeginEnd);
                    }
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeywordsBegin = true, KeywordParamBegin = true, }, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.ParamEnd, parser.Config.End);
                    if (matched == parser.Config.End)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedKeywordsBeginTag);
                        parser.OnGrammerTokenCreated(parser.Config.End, parser.Config.TermBeginEnd);
                        parser.State.End = true;
                    }
                    else if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    if (!(parser.Config.IgnoreUnknownKeyword && parser.ParsingKeyword == null))
                    {
                        ParseKeywordParam(parser, str);
                        parser.OnGrammerTokenCreated(str, parser.Config.TermParam, matched);
                    }
                    if (matched == parser.Config.ParamEnd)
                    {
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermParamBeginEnd);
                    }
                    parser.ParsingKeyword = null;
                    parser.State.KeywordParamBegin = false;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeywordsBegin = true, KeywordsEnd = true, }, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.End);
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                    }
                    else if (str != String.Empty)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnexpecetedString);
                    }
                    parser.State.End = true;
                    parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    return null;
                }
            },
            {
                new HolderParseState(){ End = true}, parser =>
                {
                    parser.State = new HolderParseState();
                    if (parser.ParsingHolder == null || parser.ParsingHolder.Name == null )
                    {
                        return null;
                    }
                    var ret = parser.ParsingHolder;
                    parser.OnHolderCreated(ret.Name, ret);
                    parser.ParsingHolder = null;
                    parser.ParsingKeyword = null;
                    if (!parser.NoInput)
                    {
                        ret.Keywords = ret.Keywords.OrderBy(k => k.PostParse == null)
                                .ThenByDescending(k => k.CalculateInput)
                                .ThenByDescending(k => k.ManipulateInput)
                                .ThenByDescending(k => k.IsValidation && !k.ManipulateOutput)
                                .ThenByDescending(k => k.IsValidation)
                                .ThenByDescending(k => k.ManipulateOutput)
                                .ThenBy(k => k.Preority)
                                .ToList();
                        var value = parser.GetValue<object>(ret);
                        parser.AppendResult(parser.Csv ? value.SafeToString().EncodeCsvField() : value.SafeToString());
                    }
                    var notSKip = ret.Keywords.EmptyIfNull().Where(key => key.PostParse != null).Aggregate(true, (current, key) => current & key.PostParse(parser, ret));
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
                    if (!str.IsNullOrEmptyValue())
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
