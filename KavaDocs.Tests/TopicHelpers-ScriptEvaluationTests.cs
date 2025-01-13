using System;
using DocMonster;
using DocMonster.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DocumentationMonster.Core.Tests
{
    [TestClass]
    public class TopicHelpersScriptEvaluationTests
    {
        

        [TestMethod]
        public void BasicEvaluationTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths.WebSurgeProjectFile);

            var topic = project.LoadTopic("INDEX");
 
            var model = new RenderTemplateModel(topic);
            var helpers = new TemplateHelpers(model);

            string content = """
                Method with string parameter:
                {{! Helpers.ChildTopicsList("topic " + "1") }}

                Method with Instance Property and nest expression:
                {{! Topic.MarkdownRaw(Topic.Body.Replace("<img","<IMG")) }}

                Method with Instance Property:
                {{! Topic.MarkdownRaw(Topic.Body) }}

                Child Topic List:
                {{! Helpers.ChildTopicsList() }}
                
                Topic Title:
                {{ Topic.Title }}
                
                Markdown Body:
                {{ Topic.Body }}

                Unencoded Body:
                {{! Topic.Body }}
                """;

            var script = new ScriptEvaluator();
            script.AllowedInstances.Add("Topic", topic);
            script.AllowedInstances.Add("Project", topic.Project);
            script.AllowedInstances.Add("Helpers", helpers);

            var result = script.Evaluate(content);

            Console.WriteLine(result);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AspTagEvaluationTest()
        {
            var project = DocProjectManager.Current.LoadProject(TestConfiguration.Current.Paths.WebSurgeProjectFile);

            var topic = project.LoadTopic("INDEX");

            var model = new RenderTemplateModel(topic);
            var helpers = new TemplateHelpers(model);

            string content = """                
                             Child Topic List:
                             <%! Helpers.ChildTopicsList() %>

                             <%! Helpers.ChildTopicsList("topic") %>

                             Topic Title:
                             <%=  Topic.Title %>

                             Markdown Body:
                             <%= Topic.Body %>

                             Encoded:
                             <%: "Test & Run" %>

                             Unencoded Body:
                             <%! Topic.Body %>

                             No workey yet:
                             <%= Topic.MarkdownRaw(Topic.Body) %>
                             """;

            var script = new ScriptEvaluator();
            script.Delimiters.StartDelim = "<%";
            script.Delimiters.EndDelim = "%>";
            script.AllowedInstances.Add("Topic", topic);
            script.AllowedInstances.Add("Project", topic.Project);
            script.AllowedInstances.Add("Helpers", helpers);

            var result = script.Evaluate(content);

            Console.WriteLine(result);
            Assert.IsNotNull(result);
        }
    }
}
