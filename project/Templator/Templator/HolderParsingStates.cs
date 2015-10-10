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
                    if (parser.Config.ContinueOnError || !parser.ReachedMaxError)
                    {
                        parser.Context.State.Error = false;
                        parser.Context.State.End = true;
                    }
                    else
                    {
                        throw new TemplatorUnexpectedStateException();
                    }
                    return null;
                }
            },
            {
                new HolderParseState(), parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.Begin, parser.Config.End, parser.Config.KeywordsBegin);
                    parser.AppendResult(str);
                    if (matched == parser.Config.Begin)
                    {
                        if (parser.Context.Holder != null)
                        {
                            parser.LogSyntextError(parser.Config.SyntaxErrorOverLappedHolder);
                        }
                        else
                        {
                            parser.Context.Holder = new TextHolder() { Position = parser.Context.Text.Position - parser.Config.Begin.Length };
                        }
                        parser.Context.State.Begin = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    else if(matched != null && parser.StackLevel > 0 && parser.ParentContext.Nesting)
                    {
                        parser.PopContext();
                        if (matched == parser.Config.KeywordsBegin)
                        {
                            parser.OnGrammerTokenCreated(matched, parser.Config.TermKeywordsBeginEnd);
                            parser.Context.State.KeywordsBegin = true;
                        }
                        else
                        {
                            parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                            parser.Context.State.End = true;
                        }
                    }
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
                        parser.Context.State.End = true;
                    }
                    else if (matched == parser.Config.CategorizedNameBegin)
                    {
                        if (!parser.Config.AvailableCategories.IsNullOrEmpty() && !parser.Config.AvailableCategories.Contains(str))
                        {
                            parser.LogSyntextError(parser.Config.SyntaxErrorInvalidCategory, str);
                        }
                        parser.Context.Holder.Category = str;
                        parser.Context.State.Category = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermCategorizedNameBeginEnd);
                    }
                    else
                    {
                        if (!parser.Config.CategoryOptional)
                        {
                            parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                        }
                        parser.Context.Holder.Name = str.Replace(" ", "");
                        parser.Context.State.Name = true;
                        parser.OnGrammerTokenCreated(parser.Context.Holder.Name, parser.Config.TermName, matched);
                        if (matched == parser.Config.End)
                        {
                            if (str.IsNullOrWhiteSpace())
                            {
                                parser.LogSyntextError(parser.Config.TermBeginEnd);
                                parser.Context.State.End = true;
                            }
                            parser.Context.State.End = true;
                            parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                        }
                        else if (matched == parser.Config.KeywordsBegin)
                        {
                            parser.Context.State.KeywordsBegin = true;
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
                        parser.Context.State.End = true;
                    }
                    else if (matched == parser.Config.End)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginNameTag);
                        parser.Context.State.End = true;
                        parser.OnGrammerTokenCreated(parser.Context.Holder.Name, parser.Config.TermName, matched);
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    else
                    {
                        parser.OnGrammerTokenCreated(str, parser.Config.TermName, matched);
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermCategorizedNameBeginEnd);
                    }
                    parser.Context.Holder.Name = str.Replace(" ", "");
                    parser.Context.State.Name = true;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true}, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.Begin, parser.Config.KeywordsBegin, parser.Config.End);
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                        parser.Context.State.End = true;
                    }
                    else if (matched == parser.Config.Begin)
                    {
                        parser.Context.NestingBefore = true;
                        NestHolder(parser);
                    }
                    else if (matched == parser.Config.KeywordsBegin)
                    {
                        parser.Context.State.KeywordsBegin = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermKeywordsBeginEnd);
                    }
                    else
                    {
                        parser.Context.State.End = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    parser.Context.ChildResultBefore.Append(str);
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
                        parser.Context.State.End = true;
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                    }
                    else if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                        parser.Context.State.End = true;
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
                                    parser.LogSyntextError(parser.Config.SyntaxErrorUnexpectedKeywordParam, str);
                                }
                            }
                            parser.ParsingKeyword = parser.Config.Keywords[str].Create();
                            parser.Context.Holder.Keywords = parser.Context.Holder.Keywords ?? new List<TemplatorKeyword>();
                            parser.Context.Holder.Keywords.Add(parser.ParsingKeyword);
                            parser.OnGrammerTokenCreated(str, parser.Config.TermKeyword, matched);
                        }
                        else
                        {
                            if (!parser.Config.IgnoreUnknownKeyword)
                            {
                                parser.LogSyntextError(parser.Config.SyntaxErrorUnexpectedKeyword, str);
                            }
                        }
                    }
                    if (matched == parser.Config.ParamBegin)
                    {
                        parser.Context.State.KeywordParamBegin = true;
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
                        parser.Context.State.KeywordsEnd = true;
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
                        parser.Context.State.End = true;
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
                    parser.Context.State.KeywordParamBegin = false;
                    return null;
                }
            },
            {
                new HolderParseState(){Begin = true, Name = true, KeywordsBegin = true, KeywordsEnd = true, }, parser =>
                {
                    string matched;
                    var str = parser.Context.Text.ReadTo(true, out matched, parser.Config.EscapePrefix, parser.Config.Begin, parser.Config.End);
                    if (matched == null)
                    {
                        parser.LogSyntextError(parser.Config.SyntaxErrorUnmatchedBeginTag);
                        parser.Context.State.End = true;
                    }
                    else if (matched == parser.Config.Begin)
                    {
                        parser.Context.NestingAfter = true;
                        NestHolder(parser);
                    }
                    else
                    {
                        parser.OnGrammerTokenCreated(matched, parser.Config.TermBeginEnd);
                        parser.Context.State.End = true;
                    }
                    parser.Context.ChildResultAfter.Append(str);
                    return null;
                }
            },
            {
                new HolderParseState(){ End = true}, parser =>
                {
                    parser.Context.State = new HolderParseState();
                    if (parser.Context.Holder == null || parser.Context.Holder.Name == null )
                    {
                        parser.Context.ChildResultBefore.Clear();
                        parser.Context.ChildResultAfter.Clear();
                        return null;
                    }
                    var ret = parser.Context.Holder;
                    parser.OnHolderCreated(ret.Name, ret);
                    parser.Context.Holder = null;
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
                        if (!value.IsNullOrEmptyValue())
                        {
                            value = parser.Csv ? value.SafeToString().EncodeCsvField() : value.SafeToString();
                            parser.AppendResult(value);
                        }
                    }
                    var notSKip = ret.Keywords.EmptyIfNull().Where(key => key.PostParse != null).Aggregate(true, (current, key) => current & key.PostParse(parser, ret));
                    parser.Context.ChildResultBefore.Clear();
                    parser.Context.ChildResultAfter.Clear();
                    return notSKip? ret : null;
                }
            }
        };

        private static void NestHolder(TemplatorParser parser)
        {
            if (!parser.Config.AllowNested)
            {
                parser.LogSyntextError(parser.Config.SyntaxErrorNestedHolders);
            }
            parser.OnGrammerTokenCreated(parser.Config.Begin, parser.Config.TermBeginEnd);
            parser.PushContext(parser.Context.Input, null, parser.Context.Holder);
            parser.Context.State.Begin = true;
            parser.Context.Holder = new TextHolder();
        }

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
                            parser.Context.State.Error = true;
                            throw new TemplatorParamsException();
                        }
                    }
                    parser.Context.Holder[parser.ParsingKeyword.Name] = str.EmptyIfNull();
                }
            }
        }
    }
}
