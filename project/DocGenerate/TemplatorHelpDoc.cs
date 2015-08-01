﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Templator;

namespace DocGenerate
{
    public static class TemplatorHelpDoc
    {
        private static readonly TemplatorConfig Config = TemplatorConfig.DefaultInstance;

        public static string Title = "Templator";
        public static string Description = "-- An Advanced text templating engine";
        public static IList<Triple<string, string, IDictionary<string, object>[]>> Sections = new List<Triple<string, string, IDictionary<string, object>[]>>
        {
            new Triple<string, string, IDictionary<string, object>[]>("Configuration", "Templator allows to be fully customized through config object, see the following options for details:", GetConfiguableProperties().ToArray()),
            new Triple<string, string, IDictionary<string, object>[]>("Philosophy", "Try to Create a text processing engine with the ablity to produce all kinds of formated text using a unified input structure, with the ablity to be fully cusomized in order to overcome any possible symbol conflicts.", null),
            new Triple<string, string, IDictionary<string, object>[]>("Usage", "Simply put the a place holder at the position of the text which you what to output, put the desired value inside the input dictionary with the name of the holder as Key, further, the usage of the rich keywords will enable programmer to calculated/validate/re-format against the input value", null),
            new Triple<string, string, IDictionary<string, object>[]>("Syntax of a TextHolder", "With the format of {{HolderName}} or {{Category(HolderName)}} or {{Category(HolderName)[Keyword1(Param1),Keyword2()]}}, simply wrap the holder name with in the begin tag({{) and end tag(}}) will produce a TextHolder, the tags are all customizeable in the config object  ", null),
            new Triple<string, string, IDictionary<string, object>[]>("Examples", "", null)
        };
        public static IList<TemplatorKeywordHelp> Keywords = new List<TemplatorKeywordHelp>()
        {
            new TemplatorKeywordHelp()
            {
                Name  = Config.KeywordRepeat, 
                Description = "Indicates an Array/Collection/Repeat, correspoding input should be an array : IDictionary<string,object>[], templator will repeat the template starting from this Holder till the matching '{0}'(in plain text) or the close tag of the element(in xml).".FormatInvariantCulture(Config.KeywordRepeatEnd),
                Examples = new List<Triple<string, string, string>>
                {
                    new Triple<string, string, string>("{{HolderName[Collection]}}{{RepeatedHolder}}{{HolderName[CollectionEnd]}}","{HolderName: [{RepeatedHolder:1},{RepeatedHolder:2}]}","12"),
                    new Triple<string, string, string>("<xml><r Bindings=\"{{HolderName[Collection]}}\">Repeated</r></xml>","{HolderName: [{},{}]}","<xml><r>Repeated</r><r>Repeated</r></xml>"),
                }
            },
            new TemplatorKeywordHelp()
            {
                Name  = Config.KeywordRepeatEnd, 
                Description = "Indicates then end position (end element in xml) of Array/Collection/Repeat",
            }
        };

        public static IDictionary<string, object> GetInputDict()
        {
            var ret = new Dictionary<string, object>
            {
                {"Title", Title},
                {"Description", Description},
                {"Sections", Sections.Select(s => new Dictionary<string, object>{{"Name", s.First}, {"Description", s.Second}, {"Details", s.Third}}).ToArray()},
                {
                    "Keywords", Keywords.Select(k => new Dictionary<string, object>()
                    {
                        {"Name", k.Name},
                        {"Description", k.Description},
                    }).ToArray()
                }
            };
            return ret;
        }

        private static IEnumerable<IDictionary<string, object>> GetConfiguableProperties()
        {
            return (from p in typeof(TemplatorConfig).GetFields(BindingFlags.Public | BindingFlags.Instance) let d = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() where d != null select new Dictionary<string, object>{{"Name", p.Name}, {"Description", d.Description}}).Cast<IDictionary<string, object>>();
        }
    }
}
