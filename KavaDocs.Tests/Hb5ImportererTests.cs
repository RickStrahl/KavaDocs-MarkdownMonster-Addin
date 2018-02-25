using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using DocHound.Utilities;
using NUnit.Framework;
using Westwind.Utilities;


namespace Westwind.HtmlHelpBuilder.Tests
{

    [TestFixture]
    public class Hbp5ImporterTests
    {
        

        [Test]
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
            Assert.True(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\webconnection\wconnect_help.json", outputFolder));
        }

        [Test]
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
            Assert.True(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\markdownmonster\markdownmonster-12-28-17.json", outputFolder));
        }

        [Test]
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
            Assert.True(importer.ImportHbp(@"C:\Users\rstrahl\Documents\Html Help Builder Projects\Westwind.Toolkit\westwind.toolkit_help.json", outputFolder));
        }



        [Test]
        public void LoadTopicTest()
        {
            var project = DocProject.LoadProject(@"c:\temp\wconnect_help\wconnect_help.json");

            var topic = project.LoadByTitle("West Wind Web Connection");
            Assert.True(topic != null, project.ErrorMessage);

            Console.WriteLine(topic.Body);
            Assert.NotNull(topic.Body);
            Assert.IsTrue( topic.Body.Contains("Welcome to West Wind Web Connection"));
        }

        [Test]
        public void LoadTopicAndSaveTest()
        {
            var project = DocProject.LoadProject(@"c:\temp\wconnect_help\wconnect_help.json");

            var topic = project.LoadTopic("INDEX");
            Assert.True(topic != null, project.ErrorMessage);

            Console.WriteLine(topic.Body);
            Assert.NotNull(topic.Body);
            Assert.IsTrue(topic.Body.Contains("Welcome to West Wind Web Connection"));

            topic.Title = "West Wind Web Connection " + DataUtils.GenerateUniqueId(5);
            topic.Body = "Updated " + DataUtils.GenerateUniqueId(5) + " " + topic.Body;

            Assert.True(project.SaveTopic(topic), project.ErrorMessage);

            Assert.True(project.SaveProject());

        }


      


    }
}
