
//using System;
//using System.Collections.Generic;
//using System.Text;
//using DocHound.Model;
//using DocHound.Razor;
//using DocumentationMonster.Core.Tests;
//using DocHound.Utilities;
//using NUnit.Framework;

//namespace Westwind.HtmlHelpBuilder.Test
//{
//    [TestClass]
//    public class RazorRenderingTests
//    {

        
//        [TestMethod]
//        public void BasicRazorTemplate()
//        {

//            string folder = PathUtility.GetDeployPath();
//            var razor = new RazorTemplates();
//            razor.StartRazorHost("C:\\projects2010\\DocumentationMonster\\SampleProject");

//            var topic = new DocTopic(null)
//            {
//                Title = "New Topic",
//                Body = "Getting to the **point** of it all"
//            };
//            string error;
//            string output = razor.RenderTemplate("~/topic.cshtml",topic, out error);

//            if ( error != null)
//                Console.WriteLine(error);
//            Assert.IsNotNull(output);            

//            Console.WriteLine(output);

//        }

//        [TestMethod]
//        public void BasicRazorStringTemplate()
//        {
//            var razor = new RazorStringTemplates();
//            razor.StartRazorHost();

//            var topic = new DocTopic(null)
//            {
//                Title = "New Topic",
//                Body = "Getting to the **point** of it all"
//            };

//            string template = @"@Helpers.TopicLink(""displaytext"",""test"")";

//            string error;
//            string output = razor.RenderTemplate(template, topic, out error);

//            if (error != null)
//                Console.WriteLine(error);
//            Assert.IsNotNull(output);

//            Console.WriteLine(output);

//        }
//    }
//}

