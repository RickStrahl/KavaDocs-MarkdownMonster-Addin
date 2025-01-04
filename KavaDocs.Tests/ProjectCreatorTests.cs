using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DocumentationMonster.Core.Tests
{
    [TestClass]
    public class ProjectCreatorTests
    {

        [TestMethod]
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

            Assert.IsNotNull(starter);
            Assert.IsNotNull(project);
        }
    }
}
