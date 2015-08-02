using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Templator;

namespace DocGenerate
{
    [TestClass]
    public class GenerateDocTest
    {
        [TestMethod]
        public void GenerateDoc()
        {
            var config = TemplatorConfig.DefaultInstance;
            config.XmlTemplatorAttributeName = "bindings";
            var parser = new TemplatorParser(config);
            var stream = "DocGenerate.Resources.index.html".GetResourceStreamFromExecutingAssembly();
            var input = TemplatorHelpDoc.GetInputDict();
            var outPut = parser.LoadXmlTemplate(stream, input);
            var file = "../../../../doc/index.html";
            if (File.Exists(file))
            {
                File.Delete(file);
            }
            using (var sw = new StreamWriter(file))
            {
                sw.Write(outPut);
            }
            if (((Logger)config.Logger).Errors.Count > 0)
            {
                Assert.Fail(((Logger)config.Logger).Errors.First());
            }
        }
    }
}
