using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocHound.Model;
using Westwind.RazorHosting;
using Westwind.Utilities;

namespace DocHound.Razor
{
    /// <summary>
    /// Provides various helper functions for displaying common HTML features on the page
    /// </summary>
    public class TemplateHelpers
    {

        public TemplateHelpers(DocTopic topic,RazorTemplateBase template)
        {
            Topic = topic;
            Project = Topic?.Project;
            Template = template as RazorTemplateFolderHost<DocTopic>;
        }

        /// <summary>
        /// Active Project for this page rendered
        /// </summary>
        public DocTopic Topic { get; set; }

        /// <summary>
        /// Active Project for this page rendered
        /// </summary>
        public DocProject Project { get; set; }
        

        public RazorTemplateFolderHost<DocTopic> Template { get; set; }


        #region Links
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
        }
        #endregion


        #region Lists
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
                "classevent", "classfield", "classconstructor"));


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

                sb.AppendLine($"\t<td class='col-icon'><img src='icons/{icon}.png' />");
                if (childTopic.ClassInfo.Static)
                    sb.Append("<img src='bmp/static.gif'/>");
                sb.AppendLine("\t</td>");

                string link = childTopic.GetTopicLink(HtmlUtils.HtmlEncode(childTopic.Title));
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


        /// <summary>
        /// Display a list of child topics
        /// </summary>
        public RawString ChildTopicsList(string topicTypesList)
        {
            StringBuilder sb = new StringBuilder();
            
            var topicTypes = topicTypesList.Split();
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
                sb.AppendLine($@"<li><img src='icons/{childTopic.DisplayType}.png' /> {HtmlUtils.HtmlEncode(childTopic.Title)}</li>");
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