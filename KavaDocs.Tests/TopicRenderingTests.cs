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
    public class TopicRenderingTests
    {

        public const string TestProjectFile = "C:\\Temp\\wconnect_help\\wconnect_help.json";

   

        [Test]
        public void WconnectRenderTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\wconnect_help.json");

            Assert.NotNull(project);

            var topic = project.LoadByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.NotNull(topic);

            string html = topic.RenderTopic(false);

            if (topic.ErrorMessage != null)
                Console.WriteLine(topic.ErrorMessage);

            Assert.NotNull(html);

            Console.WriteLine(html);
        }


        [Test]
        public void WconnectRenderToFileTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\_kavadocs-project.json");

            Assert.NotNull(project);

            var topic = project.LoadByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.NotNull(topic);

            string result = topic.RenderTopicToFile();

            Assert.True(result != null, topic.ErrorMessage);
            Assert.True(File.Exists(topic.RenderTopicFilename),"File wasn't created.");
        }


        [Test]
        public void WconnectRenderAllTopicsFileTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\_kavadocs-project.json");
            
            Assert.NotNull(project);
            Assert.True(project.Topics.Count > 0);
            var topics = project.Topics.Where(t => t.DisplayType.ToLower() == "topic");

            int x = 0;
            foreach (var topic in topics)
            {
                topic.Project = project;
                topic.RenderTopicToFile();
                x++;
            }
            Console.WriteLine(x + " topics written");
        }

        [Test]
        public void CreateTopicTreeTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\wconnect_help.json");

            Assert.NotNull(project);
            Assert.True(project.Topics.Count > 0);
            project.GetTopicTree();

            Assert.NotNull(project.Topics);
            Assert.IsTrue(project.Topics.Count > 0);
            Assert.NotNull(project.Topics[0].Topics.Count > 0);


        }


    }
}
