using System;
using System.IO;
using DocHound;
using DocHound.Model;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    [TestFixture]
    public class BasicHelpProjectTests
    {
        
        string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");

        

        [Test]
        public void CreateTopicsTest()
        {
            var project = CreateTopics();
                
            Assert.True(project.Topics.Count == 3, "Should have 3 topics.");
        }

        [Test]
        public void CreateTopicsAndSaveTest()
        {
            string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");

            var project = CreateTopics();
            Assert.True(project.Topics.Count == 3, "Should have 3 topics.");
            
            project.SaveProject(outputFile);

            Assert.True(File.Exists(outputFile));

            string json = File.ReadAllText(outputFile);
            //File.Delete(outputFile);
            Assert.IsNotEmpty(json);
            Assert.True(json.Contains("Custom Field 3"));
        }

        [Test]
        public void LoadTopicsTest()
        {
            CreateTopicsAndSaveTest();
            
            var project = DocProjectManager.Current.LoadProject(outputFile);
            
            Assert.True(project.Topics.Count > 2, "Should have 3 topics.");
            
            string json = File.ReadAllText(outputFile);

            Console.WriteLine(json);

            //File.Delete(outputFile);
            Assert.IsNotEmpty(json);
            Assert.True(json.Contains("Custom Field 3"));
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

            Assert.True(project.SaveTopic(topic), project.ErrorMessage);

            topic = new DocTopic(project)
            {
                Title = "Markdown Monster 2",
                Body = "Markdown Monster is an easy to use Makrdown Editor and Web Publishing tool.",
                Keywords = "markdown, editor"
            };
            topic.Properties.Add("Custom", "Custom Field");
            topic.Properties.Add("Custom2", "Custom Field 2");

            Assert.True(project.SaveTopic(topic), project.ErrorMessage);


            topic = new DocTopic(project)
            {
                ParentId = rootParentId,
                Title = "Markdown Monster 3",
                Body = "Markdown Monster is an easy to use Makrdown Editor and Web Publishing tool.",
                Keywords = "markdown, editor"
            };
            topic.Properties.Add("Custom", "Custom Field");
            topic.Properties.Add("Custom3", "Custom Field 3");

            Assert.True(project.SaveTopic(topic), project.ErrorMessage);

            return project;
        }



    }
}
