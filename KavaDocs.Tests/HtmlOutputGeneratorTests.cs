using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster;
using DocMonster.Model;
using DocMonster.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DocumentationMonster.Core.Tests
{
    [TestClass]
    public class HtmlOutputGeneratorTests
    {
        [TestMethod]
        public void ProjectOutputTest()
        {
            
            var project = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths.projectMarkdownMonsterHelpFile);
            var output = new HtmlOutputGenerator(project);
            output.Generate();

        }

        [TestMethod]
        public void GenerateTableOfContentsTest()
        {

            var project = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths.projectMarkdownMonsterHelpFile);
            var output = new HtmlOutputGenerator(project);
            output.GenerateTableOfContents();

        }
    }
}
