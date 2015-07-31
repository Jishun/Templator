using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;
using Irony.Interpreter.Ast;
using Irony.Parsing;

namespace Templator
{
    [Language("Templator", "1.0", "Templator parsing data format")]
    public class TemplatorGrammar : Grammar
    {
        public TemplatorGrammar() : this(TemplatorConfig.DefaultInstance)
        {
            
        }

        public TemplatorGrammar(TemplatorConfig config)
        {
            var name = new FreeTextLiteral(config.TermName, config.ParamBegin, config.KeywordsBegin, config.ParamEnd);
            var category = new IdentifierTerminal(config.TermCategory);
            var value = new FreeTextLiteral(config.TermValue, FreeTextOptions.AllowEmpty, config.ParamEnd);
            var text = new FreeTextLiteral(config.TermText, FreeTextOptions.AllowEof, config.Begin);

            if (!config.EscapePrefix.IsNullOrWhiteSpace())
            {
                text.Escapes.Add(config.EscapePrefix + config.Begin, config.Begin);
                text.Escapes.Add(config.EscapePrefix + config.EscapePrefix, config.EscapePrefix);
                value.Escapes.Add(config.EscapePrefix + config.ParamEnd, config.ParamEnd);
                value.Escapes.Add(config.EscapePrefix + config.EscapePrefix, config.EscapePrefix);
            }

            var root = new NonTerminal("root");
            var tName = new NonTerminal("Name", typeof(IdentifierNode));
            var tHolder = new NonTerminal("Holder", typeof(StatementListNode));
            var tCategorizedName = new NonTerminal("CategorizedName"); 
            var tKeyword = new NonTerminal("tKeyword");
            var tKeywords = new NonTerminal("Keywords", typeof(ParamListNode));
            var tMultiKeywords = new NonTerminal("MultiKeywords");
            var tParamedKeyword = new NonTerminal(config.TermParamedKeyword);

            var keyword = new NonTerminal(config.TermKeyword) {Rule = ToTerm(config.Keywords.First().Key)};
            foreach (var k in config.Keywords.Skip(1))
            {
               keyword.Rule |= k.Key;
            }

            RegisterBracePair(config.Begin, config.End);
            RegisterBracePair(config.KeywordsBegin, config.KeywordsEnd);
            RegisterBracePair(config.ParamBegin, config.ParamEnd);
            MarkPunctuation(config.Begin, config.End, config.KeywordsBegin, config.KeywordsEnd, config.ParamBegin, config.ParamEnd, config.Delimiter);

            //Rules
            tCategorizedName.Rule = category + config.ParamBegin + name + config.ParamEnd;
            tParamedKeyword.Rule = keyword + config.ParamBegin + value + config.ParamEnd;
            tMultiKeywords.Rule = MakeListRule(tMultiKeywords, ToTerm(config.Delimiter), tKeyword, TermListOptions.AllowEmpty | TermListOptions.AllowTrailingDelimiter | TermListOptions.AddPreferShiftHint);

            tKeyword.Rule = keyword | tParamedKeyword;

            tName.Rule = config.CategoryOptional ? (name | tCategorizedName) : tCategorizedName;
            tKeywords.Rule = Empty | config.KeywordsBegin + tMultiKeywords + config.KeywordsEnd;
            
            tHolder.Rule = (config.Begin + tName + tKeywords + config.End);

            root.Rule = MakeStarRule(root, text | Empty, tHolder | Empty);

            this.Root = root;
        }
    }
}
