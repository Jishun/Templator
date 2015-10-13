using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using DotNetUtils;
using Templator;

namespace DocGenerate
{
    public static class TemplatorHelpDoc
    {
        private static readonly TemplatorConfig Config = TemplatorConfig.DefaultInstance;
        private static string _extensionVersion = "0.0.3-beta";

        public static string Title = "Templator";
        public static string Description = "-- An Advanced text templating engine";
        public static IList<Triple<string, string, IDictionary<string, object>[]>> Sections = new List<Triple<string, string, IDictionary<string, object>[]>>
        {
            new Triple<string, string, IDictionary<string, object>[]>("Get Started", "Search for 'Templator' in nuget package manager, current version 1.0.0.5", null),
            new Triple<string, string, IDictionary<string, object>[]>("Philosophy", "Try to Create a text processing engine with the ablity to produce all kinds of formated text using a unified input structure, with the ablity to be fully cusomized in order to overcome any possible symbol conflicts.", null),
            new Triple<string, string, IDictionary<string, object>[]>("Template Usage", "Simply put the a place holder at the position of the text which you what to output, put the desired value inside the input dictionary with the name of the holder as Key, further, the usage of the rich keywords will enable programmer to calculated/validate/re-format against the input value", null),
            new Triple<string, string, IDictionary<string, object>[]>("Syntax of a TextHolder", "With the format of {{HolderName}} or {{Category(HolderName)}} or {{Category(HolderName)[Keyword1(Param1),Keyword2()]}}, simply wrap the holder name with in the begin tag({{) and end tag(}}) will produce a TextHolder, the tags are all customizeable in the config object. See examples below:", GetSyntaxExamples().ToArray()),
            new Triple<string, string, IDictionary<string, object>[]>("Build phase validation", "Nuget Search for package 'TemplatorSyntaxBuildTask', install it to the project which contains your templates, the task will add a 'TemplatorConfig.xml' into the project and load configurations from it:", GetBuildTaskConfigurations()),
            new Triple<string, string, IDictionary<string, object>[]>("Editor SyntaxHighlighting", "Beta ready, The project 'TemplatorVsExtension' is providing syntax highlighting in visual studio. based on TemplatorConfig.xml in the project", GetVsExtensionDescriptions()),
            new Triple<string, string, IDictionary<string, object>[]>("Extensibility", "Implement an TemplatorKeyWord and use AddKeyword method to add it to config object before passing it to the parser(or if after, call PrepareKeywords() to refresh the keywords), See below for the options:", GetKeywordsProperties().ToArray()),
            new Triple<string, string, IDictionary<string, object>[]>("Configuration", "Templator allows to be fully customized through config object, see the following options for details:", GetConfiguableProperties().ToArray()),
        };

        private static IDictionary<string, object>[] GetBuildTaskConfigurations()
        {
            return new IDictionary<string, object>[]
            {
                new Dictionary<string, object>(){{"Name", "Path"}, {"Description", "The path which the task will only look into, default 'Templates'"}}, 
                new Dictionary<string, object>(){{"Name", "Filters"}, {"Description", "The file extension filters, default '.xml,.csv,.txt' "}}, 
                new Dictionary<string, object>(){{"Name", "Depth"}, {"Description", "The the depth inside the directory the task will look into, default 3"}}, 
            };
        }

        private static IDictionary<string, object>[] GetVsExtensionDescriptions()
        {
            return new IDictionary<string, object>[]
            {
                new Dictionary<string, object>(){{"Name", "Version"}, {"Description", "{0}, now only supports vs2013.".FormatInvariantCulture(_extensionVersion)}}, 
                new Dictionary<string, object>(){{"Name", "Strategy"}, {"Description", "In order to get less impact to vs performace in regular work, the extension will only try to parse the active document(xml,csv,txt) if the active project contains a valid 'TemplatorConfig.xml'"}}, 
                new Dictionary<string, object>(){{"Name", "Multiple Projects"}, {"Description", "Templates contained in multiple projects will be parsed based on each project's 'TemplatorConfig.xml', which enables different format highlighting for different project needs"}}, 
                new Dictionary<string, object>(){{"Name", "Config changes"}, {"Description", "If the 'TemplatorConfig.xml' is changed, the extension will get the change, and the opened template documents needs to be reopened to get fully renewed."}}, 
            };
        }

        public static IDictionary<string, object> GetInputDict(string outputPath)
        {
            _extensionVersion = GetExtensionVersion(outputPath);
            var ret = new Dictionary<string, object>
            {
                {"Title", Title},
                {"Description", Description},
                {"ExtensionVersion", _extensionVersion},
                {"Sections", Sections.Select(s => new Dictionary<string, object>{{"Name", s.First}, {"Description", s.Second}, {"Details", s.Third}}).ToArray()},
                {
                    "Keywords", Config.Keywords.Values.Where(k => k.Description != null).Select(k => new Dictionary<string, object>()
                    {
                        {"Name", k.Name},
                        {"Description", k.Description},
                        {"Params", k.Params.IsNullOrEmpty() ? null : k.Params.Select(p => new Dictionary<string, object>
                            {
                                {"Description",p.First},
                                {"Comments",p.Second},
                            }).ToArray()}
                    }).ToArray()
                }
            };
            return ret;
        }

        private static IEnumerable<IDictionary<string, object>> GetSyntaxExamples()
        {
            yield return new Dictionary<string, object> {{"Name", "Basic"}, {"Description", "{{HolderName}}"}};
            yield return new Dictionary<string, object> {{"Name", "Categorized"}, {"Description", "{{Category(HolderName)}}"}};
            yield return new Dictionary<string, object> {{"Name", "With Parameter"}, {"Description", "{{HolderName[Number(#.##)]}},{{Category(HolderName)[Number(#.##)]}}, see keywords document for details"}};
            yield return new Dictionary<string, object> { { "Name", "Nested holders" }, { "Description", "Nested holders is to make a block which wraps other TextHolers so that it can be controlled by If conditions" } };
            yield return new Dictionary<string, object> { { "Name", "Nested Text" }, { "Description", "{{Holder[]FreeTextAfterHolderInput}} or {{(Holder)FreeTextBeforeHolderItSelf[If]}}" } };
            yield return new Dictionary<string, object> { { "Name", "Nested holders, nested value comes before" }, { "Description", "{{(Holder){{AnotherOne}}[]}} or {{(Holder){{AnotherOne}}}}" } };
            yield return new Dictionary<string, object> { { "Name", "Nested holders, nested value comes After" }, { "Description", "{{(Holder)[]{{AnotherOne}}}} or {{(Holder)[If(ConditionHolder)]{{AnotherOne}}}}" } };

        }
        private static IEnumerable<IDictionary<string, object>> GetKeywordsProperties()
        {
            return (from p in typeof(TemplatorKeyword).GetFields(BindingFlags.Public | BindingFlags.Instance) let d = p.GetCustomAttribute<DescriptionAttribute>() where d != null select new Dictionary<string, object> { { "Name", p.Name }, { "Description", d.Description } }).Cast<IDictionary<string, object>>();
        }
        private static IEnumerable<IDictionary<string, object>> GetConfiguableProperties()
        {
            return (from p in typeof(TemplatorConfig).GetFields(BindingFlags.Public | BindingFlags.Instance) let d = p.GetCustomAttribute<DescriptionAttribute>() where d != null select new Dictionary<string, object>{{"Name", p.Name}, {"Description", d.Description}}).Cast<IDictionary<string, object>>();
        }

        private static string GetExtensionVersion(string outputPath)
        {
            const string extensionPath = "../../../TemplatorVsExtension/bin/Release/";
            const string extensionConfigName = "source.extension.vsixmanifest";
            const string extensionName = "Templator.Vs.Extension{0}.vsix";
            if (!File.Exists(extensionPath+extensionName.FormatInvariantCulture("")))
            {
                throw new FileNotFoundException("Extension release build is not ready");
            }
            var xml = XDocument.Load(extensionPath + extensionConfigName);
            foreach (var x in xml.Root.DescendantsAndSelf())
            {
                x.Name = x.Name.LocalName;
                x.ReplaceAttributes((from xattrib in x.Attributes().Where(xa => !xa.IsNamespaceDeclaration) select new XAttribute(xattrib.Name.LocalName, xattrib.Value)));
            }
            var element = xml.Root.XPathSelectElement("/PackageManifest/Metadata/Identity");
            var version = "beta-" + element.GetAttributeString("Version");
            File.Copy(extensionPath + extensionName.FormatInvariantCulture(""), outputPath + extensionName.FormatInvariantCulture("-"+version), true);
            return version;
        }
    }
}
