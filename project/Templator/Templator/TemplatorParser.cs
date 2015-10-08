using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using DotNetUtils;

namespace Templator
{
    public class TemplatorParser
    {
        private bool _clearedSyntaxError = true;
        private string _syntaxCheckFileName = null;

        public bool Csv;
        public TemplatorConfig Config;
        public TemplatorKeyword ParsingKeyword;
        public TextHolder ParsingHolder;
        public HolderParseState State;
        public IList<XElement> RemovingElements = new List<XElement>();

        public IDictionary<string, TextHolder> Holders = new Dictionary<string, TextHolder>();

        public TemplatorXmlParsingContext XmlContext;
        public TemplatorParsingContext Context;
        public Stack<TemplatorParsingContext> Stack;
        public Stack<TemplatorXmlParsingContext> XmlStack = new Stack<TemplatorXmlParsingContext>();
        public bool NoInput { get; private set; }

        public TemplatorXmlParsingContext ParentXmlContext
        {
            get { return XmlStack.Peek(); }
        }

        public TemplatorParsingContext ParentContext
        {
            get { return Stack.Peek(); }
        }

        public int StackLevel
        {
            get { return Stack == null ? -1 : Stack.Count; }
        }

        public TemplatorParser(TemplatorConfig config)
        {
            Config = config;
            config.PrepareKeywords();
            config.Logger = config.Logger ?? new TemplatorLogger();
            Context = new TemplatorParsingContext();
        }

        public int ErrorCount { get; private set; }

        public bool ReachedMaxError
        {
            get { return ErrorCount > Config.MaxErrorCount; }
        }

        #region Grammar


        public virtual void OnHolderCreated(string text, TextHolder holder)
        {
            if (holder != null && Config.OnHolderFound != null)
            {
                Config.OnHolderFound(this, new TemplatorEventArgs(){Holder = holder, Input =  Context.Input});
            }
        }

        public virtual void OnGrammerTokenCreated(string token, string tokenName, string backwords = null)
        {
            if (token != null)
            {
                if (Config.OnTokenFound != null)
                {
                    Config.OnTokenFound(this, new TemplatorSyntaxEventArgs()
                    {
                        TokenName = tokenName, 
                        TokenText = token, 
                        Line = Context.Text.Line,
                        Column = Context.Text.Column,
                        Position = Context.Text.Position - (backwords == null ? 0 : backwords.Length),
                        HasError = !_clearedSyntaxError
                    });
                }
                _clearedSyntaxError = true;
            }
        }

        public virtual IDictionary<string, TextHolder> GrammarCheck(string template, string fileName)
        {
            _syntaxCheckFileName = fileName;
            ParsingHolder = null;
            ParsingKeyword = null;
            PushContext(null, null, null);
            Config.ContinueOnError = true;
            ParseText(template, null);
            return Context.Holders;
        }

        #endregion Grammer
        
        public void StartOver(bool clearHolders = true)
        {
            RemovingElements.Clear();
            Stack = null;
            XmlStack.Clear();
            XmlContext = null;
            Context = new TemplatorParsingContext();
            Csv = false;
            NoInput = false;
            if (clearHolders)
            {
                Holders.Clear();
            }
        }

        public object RequireValue(object sender, TextHolder holder, object defaultRet = null, IDictionary<string, object> input = null)
        {
            var recursiveCheckKey = StackLevel + holder.Name + "$Processing";
            var dict = input ?? Context.Input ?? Context.Params;
            if ((bool?)dict.GetOrDefault(recursiveCheckKey, null) == true)
            {
                dict.Remove(recursiveCheckKey);
                return defaultRet;
            }
            dict[recursiveCheckKey] = true;
            if (Config.OnRequireInput != null)
            {
                var args = new TemplatorEventArgs(){Holder = holder, Input = input ?? Context.Input};
                Config.OnRequireInput(this, args);
                dict.Remove(recursiveCheckKey);
                return args.Value ?? defaultRet;
            }
            dict.Remove(recursiveCheckKey);
            return defaultRet;
        }

#region parsing
        public virtual string ParseCsv(string src, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null)
        {
            Csv = true;
            return ParseText(src, input, preparsedHolders, mergeHoldersInto);
        }

        public virtual string ParseText(string src, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null)
        {
            PushContext(input, null, null);
            Context.Text = new SeekableString(src, Config.LineBreakOption);
            Context.PreparsedHolders= preparsedHolders;
            Context.ClearResult();
            Context.Input = input;
            if (preparsedHolders != null)
            {
                input.AddOrOverwrite(Config.KeyHolders, preparsedHolders);
            }
            ParseTextInternal();
            CollectHolderResults(mergeHoldersInto);
            return Context.GetResult();
        }

        public virtual XElement ParseXml(XDocument doc, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null, XmlSchemaSet schemaSet = null)
        {
            if (schemaSet != null)
            {
                doc.Validate(schemaSet, (o, e) => { }, true);
            }
            return ParseXml(doc.Root, input, preparsedHolders);
        }

        public virtual XElement ParseXml(XElement rootElement, IDictionary<string, object> input, IDictionary<string, TextHolder> preparsedHolders = null, string mergeHoldersInto = null)
        {
            XmlContext = new TemplatorXmlParsingContext(){Element = rootElement};
            PushContext(input, null, null);
            Context.PreparsedHolders = preparsedHolders;
            ParseXmlInternal(rootElement);
            if (preparsedHolders != null)
            {
                input.AddOrOverwrite(Config.KeyHolders, preparsedHolders);
            }
            foreach (var removingElement in RemovingElements)
            {
                if (removingElement != null && removingElement.Parent != null)
                {
                    removingElement.Remove();//RemoveWithNextWhitespace
                }
            }
            CollectHolderResults(mergeHoldersInto);
            return XmlContext.Element;
        }
        public void ParseXmlInternal(XElement element)
        {
            if (XmlContext.OnBeforeParsingElement != null)
            {
                XmlContext.OnBeforeParsingElement(this);
            }
            PushXmlContext(element);
            foreach (var a in element.Attributes().OrderBy(a => a.Name != Config.XmlTemplatorAttributeName))
            {
                XmlContext.Attribute = a;
                Context.ClearResult();
                Context.Text = new SeekableString(a.Value);
                var holders = ParseTextInternal();
                if (holders.Count > 0)
                {
                    a.Value = Context.GetResult();
                }
                if (a.Name == Config.XmlTemplatorAttributeName)
                {
                    a.Remove();
                }
                XmlContext.Attribute = null;
            }
            if (element.HasElements)
            {
                for (XmlContext.ElementIndex = 0; XmlContext.ElementIndex < XmlContext.ElementList.Count; XmlContext.ElementIndex++)
                {
                    var xElement = XmlContext.ElementList[XmlContext.ElementIndex] as XElement;
                    if (xElement != null)
                    {
                        ParseXmlInternal(xElement);
                        if (XmlContext.OnAfterParsingElement != null)
                        {
                            XmlContext.OnAfterParsingElement(this);
                        }
                    }
                    else
                    {
                        var text = XmlContext.ElementList[XmlContext.ElementIndex] as XText;
                        if (text != null)
                        {
                            Context.Text = new SeekableString(text.Value, Config.LineBreakOption);
                            Context.ClearResult();
                            var holders = ParseTextInternal();
                            if (holders.Count > 0)
                            {
                                text.Value = Context.GetResult();
                            }
                        }
                    }
                }
            }
            else
            {
                Context.Text = new SeekableString(element.Value, Config.LineBreakOption);
                Context.ClearResult();
                var holders = ParseTextInternal();
                if (holders.Count > 0)
                {
                    element.Value = Context.GetResult();
                    if (holders.Count == 1)
                    {
                        var info = element.GetSchemaInfo();
                        if (info != null)
                        {
                            var type = info.SchemaType as XmlSchemaSimpleType;
                            if (type != null)
                            {
                                var content = type.Content as XmlSchemaSimpleTypeRestriction;
                                if (content != null && content.Facets != null)
                                {
                                    foreach (var patternFacet in content.Facets.OfType<XmlSchemaPatternFacet>())
                                    {
                                        holders[0][Config.KeywordRegex] = patternFacet.Value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            PopXmlContext();
        }

        public virtual IList<TextHolder> ParseTextInternal()
        {
            State = new HolderParseState();
            var ret = new List<TextHolder>();
            while (!Context.Text.Eof || State.End) 
            {
                if (HolderParsingStates.States.ContainsKey(State))
                {
                    var fun = HolderParsingStates.States[State];
                    var holder = fun(this);
                    if (holder != null)
                    {
                        ret.Add(holder);
                        Context.Holders.AddOrSkip(holder.Name, holder);
                    }
                }
                else
                {
                    throw new TemplatorUnexpectedStateException();
                }
            }
            return ret;
        }
#endregion parsing

#region utils
        public IDictionary<string, object>[] GetChildInputs(string key)
        {
            return TemplatorUtil.GetChildCollection(Context.Input, key, Config);
        }

        public bool HasInput(string key)
        {
            return Context.Input.ContainsKey(key);
        }

        public object GetInputValue(string key, object defaultRet = null, int seekup = 0)
        {
            return TemplatorUtil.GetInputValue(this, key, Context.Input, defaultRet, seekup);
        }

        public T GetInputValue<T>(string key, T defaultRet = default(T))
        {
            return Context.Input.GetOrDefault(key, defaultRet).SafeConvert(defaultRet, Config.DateFormat);
        }

        public T GetValue<T>(string key, T defaultRet = default(T), int seekUp = 0)
        {
            return TemplatorUtil.GetValue(this, key, Context.Input, defaultRet, seekUp).SafeConvert<T>(default(T), Config.DateFormat);
        }

        public T GetValue<T>(TextHolder key, T defaultRet = default(T), int seekUp = 0)
        {
            return TemplatorUtil.GetValue(this, key, Context.Input, defaultRet).SafeConvert<T>(default(T), Config.DateFormat);
        }
        
        public void CacheValue(string key, object value, bool overWirteIfExists = false, IDictionary<string, object> input = null )
        {
            input = input ?? Context.Input;
            if (value == null && !Config.AllowCachingNullValues)
            {
                if (input != null && input.ContainsKey(key) && overWirteIfExists)
                {
                    input.Remove(key);
                }
                return;
            }
            if (input != null)
            {
                if (overWirteIfExists)
                {
                    input.AddOrOverwrite(key, value);
                }
                else
                {
                    input.AddOrSkip(key, value);
                }
            }
        }

        public TextHolder GetHolder(string name)
        {
            if (Context.PreparsedHolders != null)
            {
                return Context.PreparsedHolders.GetOrDefault(name) ?? new TextHolder(name);
            }
            return new TextHolder(name);
        }
#endregion utils

#region Contexts
        public void PushContext(IDictionary<string, object> input, ISeekable text, TextHolder parentHolder, bool skipOutput = false, bool disableLogging = false)
        {
            var holderDefinitions = parentHolder == null ? null : Context.PreparsedHolders.GetOrDefault(parentHolder.Name);
            if (parentHolder != null && Context.Holders.ContainsKey(parentHolder.Name))
            {
                parentHolder = Context.Holders[parentHolder.Name];
            }
            var newC = new TemplatorParsingContext(skipOutput)
            {
                Input = input,
                ParentHolder = parentHolder,
                Logger = disableLogging ? null : Config.Logger,
                Text = text ?? (Context == null ? null : Context.Text),
                PreparsedHolders = holderDefinitions == null ? null : holderDefinitions.Children
            };

            if (Stack == null)
            {
                Context = newC;
                Stack = new Stack<TemplatorParsingContext>();
                if (input == null)
                {
                    NoInput = true;
                }
                return;
            }
            Stack.Push(Context);
            Context = newC;
        }


        public void PushXmlContext(XElement element)
        {
            var newC = new TemplatorXmlParsingContext
            {
                ElementList = element.Nodes().ToArray(),
                Element = element,
                ElementIndex = 0
            };
            XmlStack.Push(XmlContext);
            XmlContext = newC;
        }

        public void PopContext()
        {
            var c = Context;
            Context = Stack.Pop();
            if (c.ParentHolder != null)
            {
                c.ParentHolder.Children = TemplatorUtil.MergeHolders(c.ParentHolder.Children, c.Holders.Values);
            }
            AppendResult(c.Result);
        }

        public void PopXmlContext()
        {
            XmlContext = XmlStack.Pop();
        }
#endregion Contexts
        public void LogSyntextError(string pattern, params object[] args)
        {
            _clearedSyntaxError = false;
            ErrorCount++;
            if (Context.Logger != null)
            {
                Context.Logger.LogError(_syntaxCheckFileName, Context.Text.PreviousLine, Context.Text.PreviousColumn, Context.Text.Line, Context.Text.Column, pattern.FormatInvariantCulture(args));
            }
            if (!Config.ContinueOnError && ReachedMaxError)
            {
                throw new TemplatorSyntaxException(pattern.FormatInvariantCulture(args));
            }
        }
        public void LogError(string pattern, params object[] args)
        {
            if (Context.Logger != null )
            {
                ErrorCount++;
                Context.Logger.LogError(pattern, args);
            }
        }
#region results
        public void AppendResult(object value)
        {
            if (Context.Result != null)
            {
                Context.Result.Append(value);
            }
        }

        private void CollectHolderResults(string mergeInto = null)
        {
            if (StackLevel > 0)
            {
                LogSyntextError("Collection Level not cleared: levels at {0}, possibly missing end holder of a collection/repeat holder", StackLevel);
            }
            TemplatorUtil.MergeHolders(Holders, Context.Holders.Values, mergeInto);
        }

#endregion results
    }
}
