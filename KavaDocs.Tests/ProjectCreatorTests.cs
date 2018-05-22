using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    [TestFixture]
    public class ProjectCreatorTests
    {

        [Test]
        public void CreateNewProjectTest()
        {
            var starter = new DocProjectCreator
            {
                Title = "My New Project",
                ProjectFolder = @"c:\temp\My New Project",
                Owner = "West Wind Technologies",
                Filename = "_toc.json",
                InstallFolder = "C:\\projects2010\\DocumentationMonster\\DocumentationMonster\\bin\\Debug"
            };
            var project = starter.CreateProject();

            Assert.NotNull(starter);
            Assert.NotNull(project);
        }
    }
}
