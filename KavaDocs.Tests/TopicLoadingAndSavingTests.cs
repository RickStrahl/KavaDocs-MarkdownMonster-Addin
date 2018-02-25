using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    
    [TestFixture]
    public class TopicLoadingAndSavingTests
    {

        public const string TestProjectFile = "C:\\Temp\\wconnect_help\\_toc.json";

        [Test]
        public void SaveTopicTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestProjectFile);

            Assert.NotNull(project);

            var topic = project.LoadByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.NotNull(topic);

            Assert.True(topic.SaveTopicFile());

            string output = File.ReadAllText(topic.GetTopicFileName());
            Console.WriteLine(output);

        }

        [Test]
        public void LoadTopicTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestProjectFile);

            Assert.NotNull(project);

            var topic = project.LoadByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.NotNull(topic);

            
            Assert.True(project.SaveTopic(topic));
            Assert.True(DocProjectManager.Current.SaveProject(project, TestProjectFile));            

        }

    }
}
