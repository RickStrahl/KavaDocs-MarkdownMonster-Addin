using System;
using System.IO;
using DocMonster.Model;
using DocMonster.Utilities;
using MarkdownMonster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Utilities;


namespace Westwind.HtmlHelpBuilder.Tests
{

    [TestClass]
    public class Hbp5ImporterTests
    {
        //private string KavaDocsAddinPath = FileUtils.ExpandPathEnvironmentVariables("%appdata%\\Markdown Monster\\Addins\\kavadocs");
        private string KavaDocsAddinPath = FileUtils.ExpandPathEnvironmentVariables("~\\Dropbox\\Markdown Monster\\Addins\\kavadocs");


        public Hbp5ImporterTests()
        {
        }

        [TestMethod]
        public void ImportWconnectHb5()
        {
            string outputFolder = @"c:\temp\wconnect_help";
            if (Directory.Exists(outputFolder))
                try
                {
                    Directory.Delete(outputFolder);
                }
                catch { }

            var importer = new HelpBuilder5JsonImporter();
            Assert.IsTrue(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\webconnection\wconnect_help.json",
                outputFolder, KavaDocsAddinPath)); 
        }

        [TestMethod]
        public void ImportWebSurgeHb5()
        {
            string exportJsonFile = @"C:\Users\rstrahl\Documents\Html Help Builder Projects\west wind websurge\west wind websurge.json";
            string outputFolder = @"d:\temp\websurge3.1_project";
            if (Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.Delete(outputFolder);
                }
                catch { }
            }
            
            var importer = new HelpBuilder5JsonImporter();
            bool result = importer.ImportHbp(exportJsonFile, outputFolder, KavaDocsAddinPath);
            Assert.IsTrue(result);

            var project = DocProject.LoadProject(Path.Combine(outputFolder, "_toc.json"));
            project.WalkTopicsHierarchy(project.Topics, (topic, project) =>
            {
                
                Console.WriteLine($" {topic.Title}");
            });


        }

        [TestMethod]
        public void ImportMarkdownMonsterHb5()
        {
            string outputFolder = @"c:\temp\markdownmonster_help";
            if (Directory.Exists(outputFolder))
                try
                {
                    Directory.Delete(outputFolder);
                }
                catch { }

            var importer = new HelpBuilder5JsonImporter();
            Assert.IsTrue(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\markdownmonster\markdownmonster-10-01-18.json", outputFolder));
        }

        [TestMethod]
        public void ImportHb5WestwindUtilities()
        {
            string outputFolder = @"C:\temp\Westwind.Utilities_help";
            if (Directory.Exists(outputFolder))
                try
                {
                    Directory.Delete(outputFolder);
                }
                catch { }

            var importer = new HelpBuilder5JsonImporter();
            Assert.IsTrue(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\Westwind.Toolkit\westwind.toolkit_help.json", outputFolder));
        }



        [TestMethod]
        public void LoadTopicTest()
        {
            var project = DocProject.LoadProject(@"c:\temp\wconnect_help\wconnect_help.json");

            var topic = project.LoadTopicByTitle("West Wind Web Connection");
            Assert.IsTrue(topic != null, project.ErrorMessage);

            Console.WriteLine(topic.Body);
            Assert.IsNotNull(topic.Body);
            Assert.IsTrue( topic.Body.Contains("Welcome to West Wind Web Connection"));
        }

        [TestMethod]
        public void LoadTopicAndSaveTest()
        {
            var project = DocProject.LoadProject(@"c:\temp\wconnect_help\wconnect_help.json");

            var topic = project.LoadTopic("INDEX");
            Assert.IsTrue(topic != null, project.ErrorMessage);

            Console.WriteLine(topic.Body);
            Assert.IsNotNull(topic.Body);
            Assert.IsTrue(topic.Body.Contains("Welcome to West Wind Web Connection"));

            topic.Title = "West Wind Web Connection " + DataUtils.GenerateUniqueId(5);
            topic.Body = "Updated " + DataUtils.GenerateUniqueId(5) + " " + topic.Body;

            Assert.IsTrue(project.SaveTopic(topic), project.ErrorMessage);

            Assert.IsTrue(project.SaveProject());

        }


      


    }
}
