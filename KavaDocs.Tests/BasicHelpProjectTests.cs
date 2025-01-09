using System;
using System.IO;
using DocMonster;
using DocMonster.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentationMonster.Core.Tests
{
    // Convert all test classes in this project to MSTest 

    [TestClass]
    public class BasicHelpProjectTests
    {
        
        string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");

        

        [TestMethod]
        public void CreateTopicsTest()
        {
            var project = CreateTopics();
            
            Assert.IsTrue(project.Topics.Count >0, "Should have 3 topics.");
        }

        [TestMethod]
        public void CreateTopicsAndSaveTest()
        {
            string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");

            var project = CreateTopics();
            Assert.IsTrue(project.Topics.Count == 3, "Should have 3 topics.");
            
            project.SaveProject(outputFile);

            Assert.IsTrue(File.Exists(outputFile));

            string json = File.ReadAllText(outputFile);
            //File.Delete(outputFile);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Custom Field 3"));
        }

        [TestMethod]
        public void LoadTopicsTest()
        {
            CreateTopicsAndSaveTest();
            
            var project = DocProjectManager.Current.LoadProject(outputFile);
            
            Assert.IsTrue(project.Topics.Count > 2, "Should have 3 topics.");
            
            string json = File.ReadAllText(outputFile);

            Console.WriteLine(json);

            //File.Delete(outputFile);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Custom Field 3"));
        }

   


        DocProject CreateTopics()
        {
            var project = new DocProject();

            var topic = new DocTopic(null)
            {
                Title = "Markdown Monster",
                Body = "Markdown Monster is an easy to use Makrdown Editor and Web Publishing tool.",
                Keywords = "markdown, editor"
            };
            topic.Properties.Add("Custom", "Custom Field");
            topic.Properties.Add("Custom2", "Custom Field 2");

            var rootParentId = topic.Id;

            Assert.IsTrue(project.SaveTopic(topic), project.ErrorMessage);

            topic = new DocTopic(project)
            {
                Title = "Markdown Monster 2",
                Body = "Markdown Monster is an easy to use Makrdown Editor and Web Publishing tool.",
                Keywords = "markdown, editor"
            };
            topic.Properties.Add("Custom", "Custom Field");
            topic.Properties.Add("Custom2", "Custom Field 2");

            Assert.IsTrue(project.SaveTopic(topic), project.ErrorMessage);


            topic = new DocTopic(project)
            {
                ParentId = rootParentId,
                Title = "Markdown Monster 3",
                Body = "Markdown Monster is an easy to use Makrdown Editor and Web Publishing tool.",
                Keywords = "markdown, editor"
            };
            topic.Properties.Add("Custom", "Custom Field");
            topic.Properties.Add("Custom3", "Custom Field 3");

            Assert.IsTrue(project.SaveTopic(topic), project.ErrorMessage);

            return project;
        }


       


    }
}
