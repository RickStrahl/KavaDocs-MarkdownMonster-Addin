using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound;
using DocHound.Model;
using DocHound.Utilities;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    [TestFixture]
    public class HtmlOutputGeneratorTests
    {
        [Test]
        public void ProjectOutputTest()
        {
            
            var project = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths.projectFile);
            var output = new HtmlOutputGenerator(project);
            output.Generate();

        }
    }
}
