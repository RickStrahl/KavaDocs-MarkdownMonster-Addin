using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using DocMonster.Annotations;
using DocMonster.Configuration;
using DocMonster.Templates;
using HtmlAgilityPack;
using MarkdownMonster;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using Westwind.Scripting;
using Westwind.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DocMonster.Model
{
    public class DocTopic : INotifyPropertyChanged
    {

        private void Test()
        {

        }

        /// <summary>
        /// References the the containing project that this 
        /// topic belongs to.
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public DocProject Project
        {
            get
            {
                if (_project == null)
                    return new DocProject();
                return _project;
            }
            set
            {
                if (Equals(value, _project)) return;
                _project = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RenderTopicFilename));
            }
        }
        private DocProject _project;


        /// <summary>
        /// The generated id for this topic - ids start with an underscore
        ///
        /// Id's always start with an underscore so they are easy to identify
        /// as ID values for multiformat Id parsing (Id, slug, title)
        /// </summary>                
        public string Id
        {
            get { return _Id; }
            set
            {
                if (value == _Id) return;
                _Id = value;
                if (!_Id.StartsWith("_"))
                    _Id = $"_{_Id}";

                OnPropertyChanged(nameof(Id));
            }
        }
        private string _Id;



        /// <summary>
        /// Parent Id for any parent topics.
        /// The parent id allows for flat queries
        /// of items so we can do things like delete
        /// children easily.
        /// </summary>
        [YamlIgnore]
        public string ParentId
        {
            get { return _ParentId; }
            set
            {
                if (value == _ParentId) return;
                _ParentId = value;
                OnPropertyChanged(nameof(ParentId));
            }
        }
        private string _ParentId;


        [YamlIgnore]
        [JsonIgnore]
        public DocTopic Parent { get; set; }


        /// <summary>
        /// The display title for this topic
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ListTitle));
                if (Slug == null)
                    Slug = CreateSlug(_title);
            }
        }
        private string _title;



        /// <summary>
        /// The title displayed for this topic in the topic
        /// list which may be truncated
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public string ListTitle
        {
            get
            {
                if (!DisplayType.StartsWith("class"))
                    return Title;


                if (DisplayType == "classheader")
                    return ClassInfo?.Classname ?? Title;

                if (DisplayType == "classproperty" || DisplayType == "classmethod" || DisplayType == "classevent")
                    return ClassInfo?.MemberName ?? Title;

                return Title;
            }
        }


        /// <summary>
        /// The Title slug used for the filename. Filenames are rendered 
        /// with an _ prefix to identify topics as topics
        /// </summary>        
        public string Slug
        {
            get { return _slug; }
            set
            {
                if (value == _slug) return;
                TopicState.OldSlug = _slug;
                _slug = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RenderTopicFilename));
            }
        }
        private string _slug;


        /// <summary>
        /// Optional link if it's different than the RenderTopicFilename
        /// </summary>

        public string Link
        {
            get { return _Link; }
            set
            {
                if (value == _Link) return;
                TopicState.OldLink = _Link;
                _Link = value;
                OnPropertyChanged(nameof(Link));

            }
        }
        private string _Link;



        [YamlIgnore]
        [JsonIgnore]
        public string RenderTopicFilename
        {
            get
            {
                if (Project == null || string.IsNullOrEmpty(Project.OutputDirectory))
                    return null;

                return FileUtils.NormalizePath(Path.Combine(Project.OutputDirectory, Slug + ".html"));
            }
        }



        /// <summary>
        /// The Topic type (ie. topic,header,classheader,classproperty etc.)
        /// </summary>
        public string DisplayType
        {
            get
            {
                var type = _displayType?.ToLower();
                if (type == null)
                {
                    if (Topics != null && Topics.Count > 0)
                        type = "header";
                    else
                        type = "topic";
                }

                return type;
            }
            set
            {
                if (value == _displayType) return;

                // don't empty null types
                if (value == null)
                {
                    if (Topics != null && Topics.Count > 0)
                        value = "header";
                    else
                        value = "topic";
                }
                _displayType = value.ToLower();
                OnPropertyChanged(nameof(DisplayType));
                TopicState.OnPropertyChanged(nameof(TopicState.ImageFilename));
            }
        }
        private string _displayType;


        /// <summary>
        /// Body text of this help topic.
        /// 
        /// This topic content is stored externally.
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public string Body
        {
            get
            {
                if (_body == null)
                    LoadTopicFile();

                return _body;
            }
            set
            {
                if (value == _body) return;
                _body = value;

                Updated = DateTime.Now;
                if (!TopicState.NoAutoSave)
                    SaveTopicFile();

                OnPropertyChanged();                
            }
        }
        private string _body;

        public string Type { get; set; } = TopicBodyFormats.Markdown;

        public string Keywords
        {
            get { return _keywords; }
            set
            {
                if (value == _keywords) return;
                _keywords = value;
                OnPropertyChanged();
            }
        }
        private string _keywords;

        public string SeeAlso { get; set; }

        [YamlIgnore]
        public string Remarks
        {
            get { return _remarks; }
            set
            {
                if (value == _remarks) return;
                _remarks = value;
                OnPropertyChanged();
            }
        }
        private string _remarks;

        [YamlIgnore]

        public string Example
        {
            get { return _Example; }
            set
            {
                if (value == _Example) return;
                _Example = value;
                OnPropertyChanged(nameof(Example));
            }
        }
        private string _Example;


        public bool IsLink
        {
            get { return _IsLink; }
            set
            {
                if (value == _IsLink) return;
                _IsLink = value;
                OnPropertyChanged(nameof(IsLink));
            }
        }
        private bool _IsLink;


        public int SortOrder
        {
            get { return _SortOrder; }
            set
            {
                if (value == _SortOrder) return;
                _SortOrder = value;
                OnPropertyChanged(nameof(SortOrder));
            }
        }
        private int _SortOrder;


        public bool Incomplete
        {
            get { return _Incomplete; }
            set
            {
                if (value == _Incomplete) return;
                _Incomplete = value;
                OnPropertyChanged(nameof(Incomplete));
            }
        }
        private bool _Incomplete;


        [YamlIgnore]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == _isExpanded) return;
                _isExpanded = value;
                OnPropertyChanged();
                if (_isExpanded)
                    TopicState.IsHidden = false;
            }
        }
        private bool _isExpanded;


        [YamlIgnore]
        public DateTime Updated { get; set; } = DateTime.UtcNow.Date;


        [YamlIgnore]
        public string HelpId
        {
            get { return _HelpId; }
            set
            {
                if (value == _HelpId) return;
                _HelpId = value;
                OnPropertyChanged(nameof(HelpId));
            }
        }
        private string _HelpId;


        /// <summary>
        /// Optional class information for this topic
        /// Anything related to code based topics
        /// </summary>
        public ClassInfo ClassInfo { get; set; } = new ClassInfo();
        //{
        //    get => _classInfo ?? new ClassInfo();                                
        //    set { _classInfo = value; }
        //}
        //private ClassInfo _classInfo;

        /// <summary>
        /// Contains various state settings for this topic
        /// when it's rendered in the UI. Internally used
        /// and not persisted.
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public TopicState TopicState
        {
            get { return _topicState; }
            set
            {
                if (Equals(value, _topicState)) return;
                _topicState = value;
                // OnPropertyChanged();
            }
        }
        private TopicState _topicState;

        /// <summary>
        /// Child Topics collection for this topic
        /// </summary>
        [YamlIgnore]
        public ObservableCollection<DocTopic> Topics
        {
            get { return _topics; }
            set
            {
                if (Equals(value, _topics)) return;
                _topics = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<DocTopic> _topics;



        /// <summary>
        /// Hierarchical configuration settings that can be set on
        /// the topic level and are overridden by:
        ///
        /// topic -> parent topic(s) -> root -> project
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Additional User defined properties that a user can add to a topic
        /// that are persisted and can be rendered
        /// </summary>
        public Dictionary<string, string> Properties
        {
            get
            {
                if (_properties == null)
                    _properties = new Dictionary<string, string>();

                return _properties;
            }
            set => _properties = value;
        }

        private Dictionary<string, string> _properties;

        #region Statics

        /// <summary>
        /// The filename used for editing KavaDocs files in the editor.
        /// This file is used to edit the active topics
        /// </summary>
        public static string KavaDocsEditorFilename = "KavaDocsTopic.md";

        #endregion

        #region Topic Rendering

        public DocTopic()
        {
            TopicState = new TopicState(this);
            Topics = new ObservableCollection<DocTopic>();
        }

        public DocTopic(DocProject project)
        {
            Id =  DataUtils.GenerateUniqueId(10);
            TopicState = new TopicState(this);
            Project = project;
            Topics = new ObservableCollection<DocTopic>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addPragmaLines"></param>        
        /// <returns></returns>
        public string RenderTopic(TopicRenderModes renderMode = TopicRenderModes.Html)
        {

            // save body in case something modifies it
            var topic = this; // Copy();           
            topic.TopicState.NoAutoSave = true;
            topic.TopicState.IsPreview = (renderMode == TopicRenderModes.Preview);

            OnPreRender(topic, renderMode);

            var model = new RenderTemplateModel(topic);

            string templateFile = null;
            if (TopicState.IsToc)
                templateFile = Path.Combine(Project.ProjectDirectory, "_kavadocs\\Themes\\TableOfContents.html");
            else
                templateFile = Path.Combine(Project.ProjectDirectory, "_kavadocs\\Themes\\" + DisplayType + ".html");

            string error;
            string html = Project.TemplateHost.RenderTemplateFile(templateFile, model, out error);

            if (TemplateHost.Script.Error)
            {
                SetError(error + "\n\n" + TemplateHost.Script.GeneratedClassCodeWithLineNumbers);
                return html;
            }

            // Fix up any locally linked .md extensions to .html
            string basePath = null;
            if (renderMode == TopicRenderModes.Html)
            {
                FixupHtmlLinks(ref html);  // external link formatting

                // returns a root relative path with a trailing '/' or empty for the root
                // Example:  `../` or `../../` or `` (root)
                basePath = GetRelativeRootBasePath();
            }
            if (renderMode == TopicRenderModes.Preview)
            {
                basePath = Path.TrimEndingDirectorySeparator(Project.ProjectDirectory).Replace("\\", "//") + "/";                
            }

            ScriptEvaluator script = null;
            if (!model.Project.Settings.DontAllowNestedTopicBodyScripts &&
                (html.Contains("{{") || html.Contains("<%")))
            {
                script = new ScriptEvaluator();
                script.AllowedInstances.Add("Topic", topic);
                script.AllowedInstances.Add("Project", topic.Project);
                script.AllowedInstances.Add("Helpers", model.Helpers);
                script.AllowedInstances.Add("Configuration", model.Configuration);


                if (html.Contains("{{"))
                    html = script.Evaluate(html, true);

                if (html.Contains("&lt;%"))
                {
                    script.Delimiters.StartDelim = "&lt;%"; // will be encoded
                    script.Delimiters.EndDelim = "%&gt;";
                    html = script.Evaluate(html, true);
                }
            }

            html = html
                        .Replace("=\"/", "=\"" + basePath)

                        .Replace("=\"~/", "=\"" + basePath)
                       // UrlEncoded
                       .Replace("=\"%7E/", "=\"" + basePath)
                       // ="/root"
                       
                       // Escaped
                       .Replace("=\"\\/", "=\"/")
                       .Replace("=\"~\\/", "=\"/");



           


            OnAfterRender(html, renderMode);

            return html;
        }


        /// <summary>
        /// Renders a topic to a specified file. If no filename
        /// is specified the default location is used in the 
        /// wwwroot folder with '_slug.htm' as the filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="addPragmaLines"></param>
        /// <param name="renderMode">Specify preview or html output mode</param>
        /// <returns></returns>
        public string RenderTopicToFile(string filename = null, TopicRenderModes renderMode = TopicRenderModes.Html)
        {
            var html = RenderTopic(renderMode);
            if (html == null)
                return null;

            if (filename == null)
                filename = RenderTopicFilename;

            //string relRootPath = FileUtils.GetRelativePath(filename, Project.OutputDirectory);
            //relRootPath = Path.GetDirectoryName(relRootPath);
            //if (!string.IsNullOrEmpty(relRootPath))
            //{
            //    int length = relRootPath.Split('\\').Length;
            //    relRootPath = StringUtils.Replicate("../", length);
            //    relRootPath = StringUtils.TerminateString(relRootPath, "/");
            //}
            //else
            //    relRootPath = string.Empty;
            //var relRootPath = GetRelativeRootBasePath(filename);
            //html = html.Replace("=\"~/", "=\"" + relRootPath);

            int written = 0; // try to write 4 times
            while (written < 4)
            {
                try
                {
                    File.WriteAllText(filename, html, Encoding.UTF8);
                    written = 10;  // done
                }
                catch (DirectoryNotFoundException)
                {
                    try
                    {
                        // Create the path and try again
                        var path = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        // and just retry
                    }
                    catch (Exception ex)
                    {
                        mmApp.Log("Warning: Unable to create output folder for topic file: " + filename + "\r\n" + ex.Message);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    written++;
                    if (written == 4)
                    {
                        mmApp.Log("Warning: Unable to write output file: " + filename + "\r\n" + ex.Message);
                        return null;
                    }
                }
            }

            return html;
        }

        /// <summary>
        /// Returns the relative rootpath for a topic (or filename) so
        /// that it can be used for linking back to the root using
        /// relative paths
        /// </summary>
        /// <param name="fileName">Optional - if not passed uses the topic render filename</param>
        /// <param name="useRelativePath">If true uses relative path syntax ../ rather than the Project Setting RelativeBaseUrl</param>
        /// <returns></returns>
        public string GetRelativeRootBasePath(string fileName = null, bool useRelativePath = false)
        {
            if (fileName == null)
                fileName = RenderTopicFilename;

            string relRootPath = string.Empty;
            if (!TopicState.IsPreview)
            {
                if (useRelativePath)
                {
                    relRootPath = FileUtils.GetRelativePath(fileName, Project.OutputDirectory);
                    relRootPath = Path.GetDirectoryName(relRootPath);
                    if (!string.IsNullOrEmpty(relRootPath))
                    {
                        int length = relRootPath.Split('\\').Length;
                        relRootPath = StringUtils.Replicate("../", length);
                        relRootPath = StringUtils.TerminateString(relRootPath, "/");
                    }
                    else
                        relRootPath = string.Empty;
                }
                else
                    relRootPath = Project.Settings.RelativeBaseUrl;
            }
            else
            {
                relRootPath = Project.ProjectDirectory?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(relRootPath))
                    relRootPath = StringUtils.TerminateString(relRootPath, "/");
                else
                    relRootPath = string.Empty;
            }

            return relRootPath;
        }

        /// <summary>
        /// Fixes up any non-Web .md file links to point to an .html
        /// file instead
        /// </summary>
        /// <param name="html"></param>
        private void FixupHtmlLinks(ref string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.SelectNodes("//a");
            if (links is { Count: > 0 })
            {
                bool updated = false;
                foreach (var link in links)
                {
                    var href = link.Attributes["href"]?.Value;
                    if (href == null)
                        continue;

                    if (string.IsNullOrEmpty(href) || href.StartsWith("http://") || href.StartsWith("https://"))                    
                        continue;

                    if (href.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase))
                    {
                        link.Attributes["href"].Value = href.Replace(".md", ".html");
                        updated = true;
                    }

                    if((href.StartsWith("dm-") && href.Contains("://"))||
                        href.StartsWith("vfps://",StringComparison.OrdinalIgnoreCase))
                    {
                        var topic = Project.LoadTopic(href);

                        string hreflink;
                        if (topic == null)
                            hreflink = "#__0";
                        else
                            hreflink =   "~/" + topic.Slug.TrimStart('/') + ".html";

                        link.Attributes["href"].Value = hreflink;
                        updated = true;
                    }
                }
                if (updated)
                    html = doc.DocumentNode.OuterHtml;
            }
        }

        /// <summary>
        /// Handler that allows you manipulate the topic before rendering.
        /// </summary>
        public Action<DocTopic, TopicRenderModes> PreRenderAction;

        /// <summary>
        /// Fired before rendering.
        ///
        /// You are allowed to make changes to the Object that is rendered
        /// </summary>
        /// <param name="renderMode"></param>
        private void OnPreRender(DocTopic topic, TopicRenderModes renderMode)
        {
            PreRenderAction?.Invoke(topic, renderMode);

            //if (string.IsNullOrEmpty(topic.Body))
            //{
            //    if (topic.Topics != null && topic.Topics.Count > 0)
            //    {
            //        var sb = new StringBuilder();
            //        sb.AppendLine("\n<div class='child-topics-list'>\n\n");

            //        foreach (var subTopic in topic.Topics)
            //        {
            //            sb.AppendLine(
            //                $"* <img src=\"~/_kavadocs/icons/{subTopic.DisplayType}.png\" /> [{subTopic.Title}]({subTopic.Link})");
            //        }

            //        sb.AppendLine("\n</div>\n\n");
            //        topic.Body = sb.ToString();
            //    }
            //}

            ProcessRenderDirectives(topic, renderMode);
        }

        /// <summary>
        /// Action that can be fired after a topic has rendered to HTML.
        /// Method gets passed the HTML string as input.
        /// </summary>
        public Action<string, TopicRenderModes> AfterRenderAction;


        /// <summary>
        /// Fired after the Topic rendering has created HTML for post processing
        /// operations on the HTML output
        /// </summary>
        /// <param name="html"></param>
        /// <param name="mode"></param>
        private void OnAfterRender(string html, TopicRenderModes mode)
        {
            AfterRenderAction?.Invoke(html, mode);
        }

        private static Regex DirectiveRegex = new Regex(@"<kavadocs:.*?\s/>", RegexOptions.Multiline);


        /// <summary>
        /// Parses `<kavadocs:directive />` commands.
        /// Examples:
        /// `<kavadocs:child-topics-list />`
        /// `<kavadocs:msdn-class-link &quoteSystem.Environment&quote />`
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="renderMode"></param>
        /// <returns></returns>
        private List<ProcessDirective> ParseRenderDirectives(DocTopic topic, TopicRenderModes renderMode)
        {
            var list = new List<ProcessDirective>();

            if (string.IsNullOrEmpty(topic.Body))
                return list;

            var matches = DirectiveRegex.Matches(topic.Body);
            foreach (Match match in matches)
            {
                var text = match.Value;

                var dir = new ProcessDirective
                {
                    DirectiveText = text,
                    Directive = StringUtils.ExtractString(text, "<kavadocs:", " ")
                };

                if (string.IsNullOrEmpty(dir.Directive))
                    continue;

                text = StringUtils.ExtractString(text, " ", " />");
                if (!string.IsNullOrEmpty(text))
                {

                    dir.Arguments = text.Split(new[] { "\",", "\" />" }, StringSplitOptions.None);
                    for (int i = 0; i < dir.Arguments.Length; i++)
                    {
                        dir.Arguments[i] = dir.Arguments[i].Trim(' ', '"');
                    }
                }

                list.Add(dir);
            }

            return list;
        }

        private void ProcessRenderDirectives(DocTopic topic, TopicRenderModes renderMode)
        {
            var directives = ParseRenderDirectives(topic, renderMode);

            foreach (var dir in directives)
            {
                // <kavadocs:child-topics-list "no-icons" />
                if (dir.Directive == "child-topics-list" || dir.Directive == "ChildTopicsList")
                {
                    if (topic.Topics != null)
                    {
                        var sb = new StringBuilder();

                        foreach (var top in topic.Topics)
                        {
                            if (dir.Arguments.Any(s => s == "no-icons"))
                                sb.AppendLine($"* [{top.Title}]({top.Link})");
                            else
                                sb.AppendLine($"* <img style='' src=\"~/_kavadocs/icons/{top.DisplayType}.png\" /> [{top.Title}]({top.Link})");
                        }

                        if (sb.Length > 0)
                        {
                            sb.Insert(0, "\n<div class='child-topics-list'>\n\n");
                            sb.AppendLine("\n\n</div>\n");
                        }

                        topic.Body = topic.Body.Replace(dir.DirectiveText, sb.ToString());
                    }
                }


            }


        }

        public class ProcessDirective
        {
            public string Directive { get; set; }
            public string[] Arguments { get; set; } = { };

            public string DirectiveText { get; set; }
        }


        /// <summary>
        /// Returns a Markdown String
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="usePragmaLines"></param>
        /// <returns></returns>
        public string Markdown(string markdown)
        {

            var args = new DocMonster.MarkdownParser.ModifyMarkdownArguments(markdown, null)
            {
                IsPreview = TopicState.IsPreview
            };
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.ProcessAllBeforeMarkdownRenderedHooks(args);

            markdown = args.Markdown;

            var parser = DocMonster.MarkdownParser.MarkdownParserFactory.GetParser(usePragmaLines: TopicState.IsPreview, forceLoad: true);
            var html = parser.Parse(markdown);



            var arg2 = new DocMonster.MarkdownParser.ModifyHtmlAndHeadersArguments(html, markdown, null)
            {
                IsPreview = TopicState.IsPreview

            };
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.ProcessAllAfterMarkdownRenderedHooks(arg2);

            return html;
        }

        public RawString MarkdownRaw(string markdown)
        {
            return new RawString(Markdown(markdown));
        }



        #endregion

        #region Topic Helpers

        public string GetFormattedHtml(string text, TopicBodyFormats format)
        {
            return text;
        }


        /// <summary>
        /// Creates a unqique slug
        /// </summary>
        /// <param name="titleText"></param>
        /// <returns></returns>
        public string CreateSlug(string titleText = null)
        {
            if (titleText == null)
                titleText = Title;
            if (string.IsNullOrEmpty(titleText))
            {
                Slug = null;
                return null;
            }

            StringBuilder sb = new StringBuilder();

            bool isInvalidBreakChar = true;
            foreach (char ch in titleText.Trim(' ', '*', '-', '.', '!', '?'))
            {
                if (ch == ' ' || ch == '.' || ch == ',')
                {
                    if (!isInvalidBreakChar)
                        sb.Append("-");
                    isInvalidBreakChar = true;
                }
                else if (ch == '-')
                {
                    if (!isInvalidBreakChar)
                        sb.Append("--");
                    isInvalidBreakChar = true;
                }
                else if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    isInvalidBreakChar = false;
                }
            }

            string slug = sb.ToString();
            slug = slug.TrimEnd('-');

            if (Project != null)
            {
                int postFix = 1;
                string origSlug = slug;

                while (Project.Topics.Any(t => t.Slug == slug))
                {
                    slug = origSlug + "-" + postFix;
                    postFix++;
                }
            }

            Slug = slug;
            return Slug;
        }


        /// <summary>
        /// Creates or updates a slug based on the current topic
        /// and parent topic. The slug and link are project
        /// relative.
        /// </summary>
        /// <param name="topic"></param>
        public void CreateRelativeSlugAndLink(DocTopic topic = null)
        {
            if (topic == null)
                topic = this;

            var slug = CreateSlug(topic.Title);
            string baseSlug = topic.Parent?.Slug;
            if (!string.IsNullOrEmpty(baseSlug))
                baseSlug += "/";

            topic.Slug = baseSlug + slug;

            // update file links
            if (topic.Link == null ||
                !(topic.Link.StartsWith("http://") || topic.Link.StartsWith("https://") ||
                topic.Link.StartsWith("vsts:") || topic.Link.StartsWith("git:")))
                topic.Link = topic.Slug + ".md";
        }



        /// <summary>
        /// Updates
        /// </summary>
        /// <param name="oldTopic"></param>
        /// <param name="newTopic"></param>
        public void UpdateRelativeSlugAndLink(DocTopic oldTopic, DocTopic newTopic = null)
        {
            // TODO: Create Update Relative Link that tries to fixes up any links in other topics

            // this creates new links appropriate for a new location
            CreateRelativeSlugAndLink(newTopic);
        }

        /// <summary>
        /// Returns a fully qualified path for the saved Topic Filename on disk
        /// which is the Slug.md or Slug.html
        /// </summary>
        /// <returns>Filename or null if topic file doesn't exist or topic filename is a URL or other format</returns>
        public string GetTopicFileName(string link = null, bool force = false)
        {
            if (string.IsNullOrEmpty(link))
                link = Link;

            if (!force && link != null && (link.StartsWith("http://") || link.StartsWith("https://")))
                return null;

            if (string.IsNullOrEmpty(Project?.ProjectDirectory))
                return null;

            string file;
            if (!string.IsNullOrEmpty(link))
            {
                file = FileUtils.NormalizePath(Path.Combine(Project.ProjectDirectory, link));
                if (File.Exists(file))
                    return file;
            }

            file = Path.Combine(Project.ProjectDirectory,
                Slug + (Type == TopicBodyFormats.Html ? ".html" : ".md"));

            return FileUtils.NormalizePath(file);
        }

        /// <summary>
        /// The full path to the KavaDocs editor filename
        /// </summary>
        public string GetKavaDocsEditorFilePath()
        {
            if (Project.ProjectDirectory == null)
                return null;
            return Path.Combine(Project.ProjectDirectory, KavaDocsEditorFilename);
        }





        /// <summary>
        /// Loads a topic file from disk and loads it into the topic body
        /// and other fields that are managed externally
        /// </summary>
        /// <returns></returns>
        public bool LoadTopicFile(string file = null)
        {
            if (!string.IsNullOrEmpty(Project?.ProjectDirectory))
            {
                if (file == null)
                    file = GetExternalFilename();

                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        _body = File.ReadAllText(file);
                        break;
                    }
                    catch
                    {
                        if (i > 3)
                            return false;
                    }
                }

                if (string.IsNullOrEmpty(_body))
                    return false;

                // normalize line feeds
                _body = _body.Replace("\r\n", "\n");

                return UpdateTopicFromYaml(_body, this);
            }

            return false;
        }


        /// <summary>
        /// Saves body field content to a Markdown file with the slug as a name
        /// </summary>
        /// <param name="markdownText">optionally provide the markdown text for the Body text.
        /// otherwise .Body is used</param>
        /// <returns></returns>
        public bool SaveTopicFile(string markdownText = null)
        {
            if (markdownText == null)
            {
                markdownText = Body;
                if (string.IsNullOrEmpty(markdownText))
                {
                    if (LoadTopicFile())
                        markdownText = Body;
                }
            }
            markdownText = StripYaml(markdownText?.Trim());

            if (string.IsNullOrEmpty(Title))
                Title = GetTitleHeader(markdownText);

            if (Project != null && Project.Settings.StoreYamlInTopics)
            {
                // Yaml serialization
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                string yaml = serializer.Serialize(this);
                markdownText = $"---\n{yaml}---\n{markdownText}";
            }

            string file = null;
            if (!string.IsNullOrEmpty(Project?.ProjectDirectory))
            {
                file = GetExternalFilename();

                // write empty file - so we always have accurate data
                if (markdownText == null)
                    markdownText = string.Empty;

                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        var path = Path.GetDirectoryName(file);
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        File.WriteAllText(file, markdownText, Encoding.UTF8);
                        break;
                    }
                    catch
                    {
                        _body = markdownText;                      
                    }
                }

            }
            else
                return false;

            return true;
        }


        /// <summary>
        /// Deletes the underlying topic markdown file if any.
        /// </summary>
        /// <param name="file">Optional filename</param>
        /// <returns></returns>
        public bool DeleteTopicFile(string file = null)
        {
            if (file == null)
                file = GetExternalFilename();

            try
            {
                File.Delete(file);

                var folder = Path.GetDirectoryName(file);
                if (!Directory.GetFiles(folder).Any())
                    Directory.Delete(folder);
            }
            catch (Exception ex)
            {
                string msg = $"Couldn't delete topic detail: {ex.Message}";
                SetError(msg);
                mmApp.Log($"KavaDocs: {msg}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the provided topic with the Yaml that is embedded
        /// in the markdown. Also updates the _body field with just the
        /// raw markdown text stripping off the yaml.
        /// </summary>
        /// <param name="markdown">markdown that may or may not contain yaml</param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public bool UpdateTopicFromYaml(string markdown, DocTopic topic = null)
        {
            if (topic == null)
                topic = this;

            string extractedYaml = GetYaml(markdown);

            if (!string.IsNullOrEmpty(extractedYaml))
            {
                var yaml = StringUtils.ExtractString(extractedYaml, "---", "\n---", returnDelimiters: false).Trim();
                var sr = new StringReader(yaml);
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                try
                {
                    // TODO: Better parsing of YAML data
                    var yamlObject = deserializer.Deserialize<DocTopic>(sr);

                    if (string.IsNullOrEmpty(Id))
                        Id = yamlObject.Id;

                    if (!string.IsNullOrEmpty(yamlObject.Title))
                        Title = yamlObject.Title;

                    if (!string.IsNullOrEmpty(yamlObject.DisplayType))
                        DisplayType = yamlObject.DisplayType;
                    if (!string.IsNullOrEmpty(yamlObject.Slug))
                        Slug = yamlObject.Slug;
                    if (!string.IsNullOrEmpty(yamlObject.Link))
                        Link = yamlObject.Link;
                    if (!string.IsNullOrEmpty(yamlObject.Keywords))
                        Keywords = yamlObject.Keywords;
                    if (!string.IsNullOrEmpty(yamlObject.SeeAlso))
                        SeeAlso = yamlObject.SeeAlso;
                    if (yamlObject.SortOrder > 0 || SortOrder == 0)
                        SortOrder = yamlObject.SortOrder;

                    if (Properties.Count < 1 || yamlObject.Properties.Count > 0)
                        Properties = yamlObject.Properties;

                    ClassInfo = yamlObject.ClassInfo;
                }
                catch
                {
                    return false;
                }

                _body = _body.Replace(extractedYaml, "");

                if (string.IsNullOrEmpty(Title))
                    Title = GetTitleHeader(_body);
            }

            return true;
        }

        /// <summary>
        /// Makes a separate copy of a topic
        /// that can be changed
        /// </summary>
        /// <returns></returns>
        public DocTopic Copy()
        {
            var topic = new DocTopic();
            DataUtils.CopyObjectData(this, topic);
            return topic;
        }

        #endregion

        #region Topic Body Helpers

        internal static readonly Regex YamlExtractionRegex = new Regex(@"\A---[ \t]*\r?\n[\s\S]+?\r?\n(---|\.\.\.)[ \t]*\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);
        internal static readonly Regex YamlExtractionTextOnlyRegex = new Regex(@"\A---[ \t]*\r?\n[\s\S]+?\r?\n(---|\.\.\.)[ \t]*\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Extracts Yaml as a string from a markdown block
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public string GetYaml(string markdown, bool noDelimiters = false)
        {
            if (!markdown.StartsWith("---\n") && !markdown.StartsWith("---\r"))
                return null;

            string extractedYaml = null;

            var match = MarkdownUtilities.YamlExtractionRegex.Match(markdown);
            if (match.Success)
                extractedYaml = match.Value;

            if (noDelimiters)
                return StringUtils.ExtractString(extractedYaml, "---", "\n---")?.Trim();

            return extractedYaml;
        }

        /// <summary>
        /// Strips a Yaml Block from markdown and returns the
        /// markdown without Yaml.
        /// </summary>
        /// <param name="markdownText"></param>
        /// <returns></returns>
        public string StripYaml(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
                return markdownText;

            string extractedYaml = GetYaml(markdownText);
            if (!string.IsNullOrEmpty(extractedYaml))
            {
                extractedYaml = StringUtils.ExtractString(extractedYaml, "---", "\n---", returnDelimiters: true);

                if (!string.IsNullOrEmpty(extractedYaml))
                    markdownText = markdownText.Replace(extractedYaml, "").Trim();
            }

            return markdownText;
        }

        public string GetTitleHeader(string markdown)
        {
            string title = null;

            // Read the title out of the MD body
            if (string.IsNullOrEmpty(markdown))
            {
                var lines = StringUtils.GetLines(markdown);
                var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                if (!string.IsNullOrEmpty(titleLine) && titleLine.Length > 2)
                    title = titleLine.Trim().Substring(2);
            }
            return title;
        }

        private string GetExternalFilename()
        {
            string file;
            if (!string.IsNullOrEmpty(Link) && !(Link.StartsWith("http://") || Link.StartsWith("https://")))
            {
                file = FileUtils.NormalizePath(Path.Combine(Project.ProjectDirectory, Link));
            }
            else
            {
                string filename = Slug;
                if (string.IsNullOrEmpty(filename))
                    filename = "_" + Id;

                file = Path.Combine(Project.ProjectDirectory,
                    filename + (Type == TopicBodyFormats.Html ? ".html" : ".md"));
            }

            return file;
        }

        /// <summary>
        /// Determines whether a display is a header topic
        /// </summary>
        /// <param name="displayType"></param>
        /// <returns></returns>
        public bool IsHeaderTopic(string displayType = null)
        {
            if (string.IsNullOrEmpty(displayType))
                displayType = DisplayType;

            if (displayType == "header" ||
                displayType == "index" ||
                displayType == "database" ||
                displayType == "webservice" ||
                displayType == "dataview" ||
                displayType == "datatable" ||
                displayType == "enum" ||
                displayType == "delegate")
                return true;

            return false;
        }

        /// <summary>
        /// Allows setting the body of the topic without forcing a file write
        /// operation in the setter. Useful for initialization.
        /// </summary>
        /// <param name="bodyText"></param>
        public void SetBodyWithoutSavingTopicFile(string bodyText)
        {
            _body = bodyText;
        }
        #endregion


        #region Topics

        /// <summary>
        /// Retrieves a topic Anchor HTML link for the current topic
        /// </summary>
        /// <param name="displayText">Text to show for the link</param>
        /// <param name="anchor"></param>
        /// <param name="attributes">optional attributes for the link</param>
        /// <param name="mode"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        public string GetTopicLink(string displayText=null,
            string anchor = null,
            string attributes = null,
            HtmlRenderModes mode = HtmlRenderModes.None,
            string link = null)
        {
            if (string.IsNullOrEmpty(displayText))
                displayText = Title;

            string anchorString = (string.IsNullOrEmpty(anchor) ? "" : "#" + anchor);
            string linkText = WebUtility.HtmlEncode(displayText);

            if (mode == HtmlRenderModes.None)
                mode = TopicState.IsPreview ? HtmlRenderModes.Preview : HtmlRenderModes.Html;

            var relBasePath = GetRelativeRootBasePath();
            
            if (link == null)
                link = relBasePath + Slug.TrimEnd('/') + ".html";                         
                    

            // Plain HTML
            if (mode == HtmlRenderModes.Html || mode == HtmlRenderModes.Preview)
                link = $"<a href=\"{link}{anchorString}\" {attributes}>{linkText}</a>";
            // Preview Mode
            else if (mode == HtmlRenderModes.Preview)
                link = $"<a href=\"dm://Topic/{link}{anchorString}\" {attributes}>{linkText}</a>";
            if (mode == HtmlRenderModes.Print)
                link = $"<a href=\"#{link}{anchorString}' {attributes}>{linkText}</a>";

            return link;
        }
        #endregion

        #region Custom Property Management
        /// <summary>
        /// Adds a new property to the active topic
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="caption"></param>
        public void AddProperty(string name, string value, string caption = null)
        {
            Properties.Add(name, value);
        }

        /// <summary>
        /// Updates a property and optionally adds it if it doesn't exist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="autoAdd"></param>
        /// <param name="caption"></param>
        public void UpdateProperty(string name, string value, bool autoAdd = true, string caption = null)
        {
            if (autoAdd)
                Properties[name] = value;
            else if (Properties.ContainsKey(name))
                Properties[name] = value;
        }

        public void RemoveProperty(string name)
        {
            try
            {
                Properties.Remove(name);
            }
            catch { }
        }
        #endregion

        #region Error Handling
        [YamlIgnore]
        [JsonIgnore]
        public string ErrorMessage { get; set; }



        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                this.ErrorMessage = string.Empty;
                return;
            }
            this.ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                this.ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }
        #endregion



        public override string ToString()
        {
            return $"{Id} - {Title} - {Topics.Count}";
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (TopicState != null)
                TopicState.IsDirty = true;
        }

        #endregion

    }

    public class ClassInfo
    {

        public string Classname { get; set; }

        public string MemberName { get; set; }

        public string Scope { get; set; }

        public string Parameters { get; set; }

        public string Returns { get; set; }

        public string Syntax { get; set; }

        public string Signature { get; set; }

        public string RawSignature { get; set; }


        public string Inherits { get; set; }
        public string Implements { get; set; }

        public string InheritanceTree { get; set; }

        public bool IsInherited { get; set; }
        public bool IsConstructor { get; set; }

        public string Assembly { get; set; }
        public string Contract { get; set; }
        public string Namespace { get; set; }
        public bool IsStatic { get; set; }
        public string Exceptions { get; set; }

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Classname) && string.IsNullOrEmpty(MemberName);

        public string GetBaseMethodSignature()
        {
            string sig = Signature ?? string.Empty;
            int at = sig.IndexOf("(");
            if (at > 0)
               sig = sig.Substring(0, at);

            return sig;
        }

        public override string ToString()
        {
            return $"{Signature} {Classname} {Namespace}";
        }
    }

    public enum TopicRenderModes
    {
        Html,
        Preview
    }

    public class TopicBodyFormats
    {
        public static List<string> TopicBodyFormatsList { get; } = new List<string>();

        static TopicBodyFormats()
        {
            var pi = typeof(TopicBodyFormats).GetProperties(System.Reflection.BindingFlags.Static |
                                                            System.Reflection.BindingFlags.Public |
                                                            System.Reflection.BindingFlags.GetProperty).Where(p =>
                    p.Name != nameof(TopicBodyFormatsList))
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var p in pi)
            {
                TopicBodyFormatsList.Add(p.GetValue(null).ToString());
            }
        }

        public static string Markdown => "markdown";
        public static string Html => "html";
        public static string ImageUrl => "imageurl";
        public static string HelpBuilder => "helpbuilder";
    }
}

