using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DocMonster.Model;
using Westwind.Scripting;
using Westwind.Utilities;
using static System.Net.WebRequestMethods;

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
<thead>
<tr><th colspan='2'>{memberLabel}</th><th>{descriptionLabel}</th></tr>
</thead>
<tbody>
            ");

            
            bool alternate = false;
            foreach (var childTopic in childTopics)
            {
                childTopic.TopicState.IsPreview = Topic.TopicState.IsPreview;

                sb.AppendLine("<tr" + (alternate ? " class='alternaterow'>" : ">"));

                string icon = childTopic.DisplayType;
                if (childTopic.ClassInfo.Scope == "protected")
                    icon += "protected";

                sb.AppendLine($"\t<td class='col-icon'><img src=\"~/_kavadocs/icons/{icon}.png\" />");
                if (childTopic.ClassInfo.IsStatic)
                    sb.Append("<img src=\"~/_kavadocs/icons/static.png\"/>");
                sb.AppendLine("\t</td>");

                string link = childTopic.GetTopicLink(WebUtility.HtmlEncode(childTopic.ClassInfo?.MemberName));

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
            sb.AppendLine("<tbody>\n</table>");


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
                //var link = relBasePath + childTopic.Slug.TrimEnd('/') + ".html";

                var link = childTopic.GetTopicLink(childTopic.Title);

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

            var topics = Topic.Parent?.Topics;
            if (topics == null)
                return RawString.Empty;

            var overloads = topics
                .Where(t => t.ClassInfo != null &&
                            (t.DisplayType == "classmethod" || t.DisplayType == "classconstructor") &&
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

        /// <summary>
        /// Returns a list of see also topics as an Html list
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public RawString FixupSeeAlsoLinks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return RawString.Empty;

            var sb = new StringBuilder();
            var lines = StringUtils.GetLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine("<div>" + ParseTopicIdIntoHref(lines[i]) + "</div>");                
            }

            return RawString.Raw(sb.ToString());
        }

        /// <summary>
        /// Parses a topic Id into a link
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public string ParseTopicIdIntoHref(string topicId)
        {
            if (string.IsNullOrEmpty(topicId))
                return topicId;

            var tcs = new System.Threading.CancellationTokenSource();
            DocTopic matchTopic = null;
            Project.WalkTopicsHierarchy(Project.Topics, (topic, level, cancel) =>
            {
                if (string.Equals(topic.Id, topicId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(topic.Slug, topicId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(topic.Title, topicId, StringComparison.OrdinalIgnoreCase))
                {
                    matchTopic = topic;
                    tcs.Cancel();
                }
            }, tcs.Token);

            if (matchTopic != null)
                return matchTopic.GetTopicLink(matchTopic.Title);

            // return as is
            return topicId;
        }

     
        #endregion

        #region Formatting Helpers

        /// <summary>
        /// Formats the Inheritance Tree with 
        /// </summary>
        /// <returns></returns>
        public RawString FormatInheritanceTree()
        {
            if (string.IsNullOrEmpty(Topic.ClassInfo.InheritanceTree))
                return RawString.Empty;


            StringBuilder sb = new StringBuilder();
            var lines = StringUtils.GetLines(Topic.ClassInfo.InheritanceTree?.Trim());
            for (int i = 0; i < lines.Length; i++)
            {
                string spaces = string.Empty;
                if (i > 0)
                    spaces = StringUtils.Replicate("&nbsp;", i * 3);
                    
                if (i == lines.Length - 1)
                {
                    sb.AppendLine(spaces + "<b>" + WebUtility.HtmlEncode(lines[i]) + "</b><br/>");
                }
                else
                {
                    string href;
                    if (lines[i].StartsWith("System.") || lines[i].StartsWith("Micosoft."))
                    {
                        href =
                            $"{spaces}<a href=\"https://learn.microsoft.com/en-us/dotnet/api/{lines[i]}\"" +
                                 "target=\"topic\">" +
                                 WebUtility.HtmlEncode(lines[i]) +
                                 "</a><br />";
                    }
                    else
                    {
                        href = $"{spaces}{WebUtility.HtmlEncode(lines[i])}<br/>";
                    }

                    sb.AppendLine(href);
                }
            }

            return RawString.Raw(sb.ToString());                
        }


        public string Json(object value)
        {
            if (value is string jsonString)
                return StringUtils.ToJsonString(jsonString);

            return JsonSerializationUtils.Serialize(value, formatJsonOutput: true);
        }
        #endregion
    }


}
