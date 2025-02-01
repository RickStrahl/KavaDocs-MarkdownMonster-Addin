using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentationMonster.Core.Tests
{
    
    [TestClass]
    public class TopicLoadingAndSavingTests
    {

        public const string TestProjectFile = "C:\\Temp\\wconnect_help\\_toc.json";

        [TestMethod]
        public void SaveTopicTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestProjectFile);

            Assert.IsNotNull(project);

            var topic = project.LoadTopicByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.IsNotNull(topic);

            Assert.IsTrue(topic.SaveTopicFile());

            string output = File.ReadAllText(topic.GetTopicFileName());
            Console.WriteLine(output);

        }

        [TestMethod]
        public void LoadTopicTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestProjectFile);

            Assert.IsNotNull(project);

            var topic = project.LoadTopicByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.IsNotNull(topic);

            
            Assert.IsTrue(project.SaveTopic(topic));
            Assert.IsTrue(DocProjectManager.Current.SaveProject(project, TestProjectFile));            

        }

    }
}
