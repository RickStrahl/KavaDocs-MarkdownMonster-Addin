using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DocMonster.Model;
using Westwind.Scripting;
using Westwind.Utilities;

namespace DocMonster.Templates
{
    /// <summary>
    /// Provides various helper functions for displaying common HTML features on the page
    /// </summary>
    public class TemplateHelpers
    {

        public TemplateHelpers(RenderTemplateModel model)
        {            
            Model = model;
            Topic = model.Topic;
            Project = Topic?.Project;
        }

        /// <summary>
        /// Active Project for this page rendered
        /// </summary>
        public DocTopic Topic { get; set; }


        /// <summary>
        /// Active Project for this page rendered
        /// </summary>
        public DocProject Project { get; set; }

        /// <summary>
        ///  For good measure the entire model is here
        /// </summary>
        public RenderTemplateModel Model { get; }



        // public TemplateHost Template { get; set; }


        #region Links

        public RawString Raw(string value) => RawString.Raw(value);   // RawString implements IRawString

        public RawString Raw(object value) => RawString.Raw(value);
      

        /// <summary>
        /// Returns a topic link
        /// </summary>
        /// <param name="display"></param>
        /// <param name="id"></param>
        /// <param name="anchor"></param>
        /// <returns></returns>
        public RawString TopicLink(string display, string id, string anchor = null)
        {
            return new RawString(Project.GetTopicLink(display, id));
            //RawString.Raw(Project.GetTopicLink(display, id));
        }
        #endregion


        #region Lists
        public RawString ClassMemberTableHtml()=> ClassMemberTableHtml(null, "Member", "Description");
        
        /// <summary>
        /// Renders an Html table for Class members showing
        /// </summary>
        public RawString ClassMemberTableHtml(
            string tableAttributes = null,
            string memberLabel = "Member",
            string descriptionLabel = "Description"
            )
        {
          
            var childTopics = Topic.Topics.Where(t => GenericUtils.Inlist(t.DisplayType, "classproperty", "classmethod",
                "classevent", "classfield", "classconstructor")).ToList();


            if (childTopics.Count < 1)
                return new RawString(string.Empty);
            

            StringBuilder sb = new StringBuilder();
            sb.Append($@"
<table class='detailtable' {tableAttributes}>
<tr><th colspan='2'>{memberLabel}</th><th>{descriptionLabel}</th></tr>
            ");

            
            bool alternate = false;
            foreach (var childTopic in childTopics)
            {
                

                sb.AppendLine("<tr" + (alternate ? " class='alternaterow'>" : ">"));

                string icon = childTopic.DisplayType;
                if (childTopic.ClassInfo.Scope == "protected")
                    icon += "protected";

                sb.AppendLine($"\t<td class='col-icon'><img src='~/kavadocs/icons/{icon}.png' />");
                if (childTopic.ClassInfo.IsStatic)
                    sb.Append("<img src='~/_kavadocs/icons/static.gif'/>");
                sb.AppendLine("\t</td>");

                string link = childTopic.GetTopicLink(WebUtility.HtmlEncode(childTopic.Title));

                sb.AppendLine($"\t<td>{link}</td>");
                sb.AppendLine($"\t<td class='col-detail'>{HtmlUtils.HtmlAbstract(childTopic.Body, 200)}");

                if (childTopic.DisplayType == "classmethod")
                {
                    sb.AppendLine($"\t\t<div><small><b>{childTopic.ClassInfo.Syntax}</b></small></div>");

                    // check for overloads
                    var overloads =
                        childTopic.Topics
                            .Where(t => t.ClassInfo.MemberName == childTopic.ClassInfo.MemberName &&
                                        t.Id != childTopic.Id);
                    foreach (var overload in overloads)
                    {
                        sb.AppendLine($"\t\t<div class='syntaxoverloads'>{overload.ClassInfo.Syntax}</div>");
                    }
                }
                sb.AppendLine("\t</td>");
                sb.AppendLine("</tr>");

                alternate = !alternate;
            }
            sb.AppendLine("</table>");


            return new RawString(sb.ToString());
        }


        public string GetNameTest(bool includeLast)
        {
            if (includeLast)
                return "Hello Rick Strahl";

            return "Rick";
        }


        public RawString ChildTopicsList()
        {
            return ChildTopicsList(null);
        }

        /// <summary>
        /// Display a list of child topics
        /// </summary>
        public RawString ChildTopicsList(string topicTypesList)
        {
            var sb = new StringBuilder();            

            topicTypesList = topicTypesList ?? string.Empty;

            var topicTypes = topicTypesList.Split(',' , StringSplitOptions.RemoveEmptyEntries);
            List<DocTopic> childTopics;
            if (topicTypes.Length > 0)
                childTopics = Topic.Topics.Where(t => GenericUtils.Inlist<string>(t.DisplayType, topicTypes)).ToList();
            else
                childTopics = Topic.Topics.ToList();

            if (childTopics.Count < 1)
                return new RawString();

            sb.AppendLine("<ul style='list-style-type:none'>");

            foreach (var childTopic in childTopics)
            {
                childTopic.TopicState.IsPreview = this.Topic.TopicState.IsPreview;
                var relBasePath = Topic.GetRelativeRootBasePath();
                var link = relBasePath + childTopic.Slug.TrimEnd('/') + ".html";

                link = childTopic.GetTopicLink(childTopic.Title);

                 sb.AppendLine($"""
<li><img src='{relBasePath}_kavadocs/icons/{childTopic.DisplayType}.png' />
{link}
</li>
""");
            }
            sb.AppendLine("</ul>");

            return new RawString(sb.ToString());
        }

        /// <summary>
        /// Inserts method overloads into a topic
        /// </summary>
        /// <returns></returns>
        public RawString InsertMethodOverloads()
        {
            if (string.IsNullOrEmpty(Topic.ClassInfo.Signature))
                return RawString.Empty;

            string signature = Topic.ClassInfo.Signature;
            int at = signature.IndexOf("(");
            if (at > 0)            
                signature = signature.Substring(0, at);

            var overloads = Project.GetTopics()
                .Where(t => t.ClassInfo != null &&
                            t.DisplayType == "classmethod" &&
                            t.ClassInfo.Signature != null &&
                            t.ClassInfo.Signature.StartsWith(signature) &&
                            t.Id != Topic.Id)
                .ToList();
            if (overloads.Count < 1)
                return RawString.Empty;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<h3 class=\"outdent\" id=\"overloads\">Overloads</h3>");
            sb.AppendLine("<div style='margin-left: 10px;'>");

            foreach (var overload in overloads)
            {
                string syntax = overload.ClassInfo.Syntax;
                sb.AppendLine( "<p class='syntaxoverloads'>" + overload.GetTopicLink(syntax) + "</p>");
            }
            sb.AppendLine("</div>");

            return new RawString(sb);
        }
        #endregion
    }


}
