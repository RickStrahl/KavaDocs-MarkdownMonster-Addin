using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster;
using DocMonster.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Utilities;


namespace DocumentationMonster.Core.Tests
{

    [TestClass]
    public class TopicRenderingTests
    {

        public const string TestProjectFile = "C:\\Temp\\wconnect_help\\wconnect_help.json";

        [TestMethod]
        public void WebSugeRenderTest()
        {
            var project = DocProjectManager.Current.LoadProject("D:\\temp\\websurge3.1_project\\_toc.json");

            Assert.IsNotNull(project);

            var topic = project.LookupTopicByTitle("Command Line Interface");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.IsNotNull(topic);

            string html = topic.RenderTopic( DocMonster.Model.TopicRenderModes.Preview);

            if (topic.ErrorMessage != null)
                Console.WriteLine(topic.ErrorMessage);

            Assert.IsNotNull(html);

            Console.WriteLine(html);
            
            ShellUtils.ShowHtml(html);
        }



        [TestMethod]
        public void WconnectRenderTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\wconnect_help.json");

            Assert.IsNotNull(project);

            var topic = project.LookupTopicByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.IsNotNull(topic);

            string html = topic.RenderTopic(TopicRenderModes.Html);

            if (topic.ErrorMessage != null)
                Console.WriteLine(topic.ErrorMessage);

            Assert.IsNotNull(html);

            Console.WriteLine(html);

        }


        [TestMethod]
        public void WconnectRenderToFileTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\_kavadocs-project.json");

            Assert.IsNotNull(project);

            var topic = project.LookupTopicByTitle("Step 3 - Finish and configure Web Connection for SQL tables");

            if (project.ErrorMessage != null)
                Console.WriteLine(project.ErrorMessage);
            Assert.IsNotNull(topic);

            string result = topic.RenderTopicToFile();

            Assert.IsTrue(result != null, topic.ErrorMessage);
            Assert.IsTrue(File.Exists(topic.RenderTopicFilename),"File wasn't created.");
        }


        [TestMethod]
        public void WconnectRenderAllTopicsFileTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\_kavadocs-project.json");
            
            Assert.IsNotNull(project);
            Assert.IsTrue(project.Topics.Count > 0);
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

        [TestMethod]
        public void CreateTopicTreeTest()
        {
            var project = DocProjectManager.Current.LoadProject("C:\\Temp\\wconnect_help\\wconnect_help.json");

            Assert.IsNotNull(project);
            Assert.IsTrue(project.Topics.Count > 0);
            project.GetTopicTree();

            Assert.IsNotNull(project.Topics);
            Assert.IsTrue(project.Topics.Count > 0);
            Assert.IsNotNull(project.Topics[0].Topics.Count > 0);


        }


    }
}
