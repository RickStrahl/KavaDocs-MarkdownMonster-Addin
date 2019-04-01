using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound;
using DocHound.Model;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    public class TopicParserTests
    {
        [Test]
        public void TopicParserParseAssemblyTest()
        {

            var assemblyFile =
                @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net45\Westwind.Utilities.dll";

            var proj = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths
                .projectMarkdownMonsterHelpFile);

            var topic = new DocTopic(proj)
            {
                Title = "Class Reference",
                DisplayType = "header",                

            };

            topic.CreateRelativeSlugAndLink(topic);
            topic.Body = "Class Reference for " + assemblyFile;
            proj.Topics.Add(topic);
            topic.SaveTopicFile();




            var parser = new DocHound.Importer.TopicParser(proj, topic);

         
            parser.ParseAssembly(assemblyFile, topic, true);

            proj.SaveProject();


        }

    }
}
