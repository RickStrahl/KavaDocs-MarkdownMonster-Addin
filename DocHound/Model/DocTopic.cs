using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocHound.Annotations;
using DocHound.Utilities;
using MarkdownMonster;
using Newtonsoft.Json;
using Westwind.Utilities;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MarkdownParserFactory = DocHound.MarkdownParser.MarkdownParserFactory;

namespace DocHound.Model
{
    public class DocTopic : INotifyPropertyChanged
    {
        
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
        /// </summary>                
        public string Id
        {
            get { return _Id; }
            set
            {
                if (value == _Id) return;
                _Id = value;
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



        private DocTopic _parent;

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
                if (Slug == null)
                    Slug = CreateSlug(_title);
            }
        }
        private string _title;


        

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

                return Path.Combine(Project.OutputDirectory, "_" + Slug + ".html");
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
                if (string.IsNullOrEmpty(type))
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
                if (string.IsNullOrEmpty(value))
                {
                    if (Topics != null && Topics.Count > 0)
                        value= "header";
                    else
                        value= "topic";                    
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
                if (string.IsNullOrEmpty(_body))
                    LoadTopicFile();

                return _body;
            }
            set
            {
                if (value.Equals(_body)) return;
                _body = value;
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
        public DateTime Updated { get; set; }


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

        
        public ClassInfo ClassInfo { get; set; }

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

               
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();


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
        }

        public DocTopic(DocProject project)  
        {
            Id = DataUtils.GenerateUniqueId(10);
            TopicState = new TopicState(this);
            Project = project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addPragmaLines"></param>
        /// <param name="renderExternalLinks"></param>
        /// <returns></returns>
        public string RenderTopic(bool addPragmaLines = false, bool renderExternalLinks = false)
        {
            string error;
            string html = Project.TemplateRenderer.RenderTemplate(DisplayType + ".cshtml", this, out error);
            
            if (string.IsNullOrEmpty(html))
            {
                SetError(error);
                return null;
            }

            return html;
        }

        /// <summary>
        /// Renders a topic to a specified file. If no filename
        /// is specified the default location is used in the 
        /// wwwroot folder with '_slug.htm' as the filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="addPragmaLines"></param>
        /// <returns></returns>
        public string RenderTopicToFile(string filename = null, bool addPragmaLines = false)
        {
            var html = RenderTopic(addPragmaLines);
            if (html==null)
                return null;

            int written = 0;
            while (written < 4)
            {
                if (filename == null)
                    filename = RenderTopicFilename;

                try
                {                    
                    File.WriteAllText(filename, html, Encoding.UTF8);
                    written = 10;
                }
                catch(Exception ex)
                {                    
                    Thread.Sleep(50);
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
        /// Returns a Markdown String
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="usePragmaLines"></param>
        /// <returns></returns>
        public string Markdown(string markdown)
        {
            var parser = MarkdownParserFactory.GetParser(usePragmaLines: this.TopicState.IsPreview, forceLoad: true);
            var html = parser.Parse(markdown);
            return html;
        }

        public Westwind.RazorHosting.RawString MarkdownRaw(string markdown)
        {            
            return new Westwind.RazorHosting.RawString(Markdown(markdown));            
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
            foreach (char ch in titleText.Trim(' ','*','-','.','!','?'))
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
        /// Returns a fully qualified path for the saved Topic Filename on disk
        /// which is the Slug.md or Slug.html
        /// </summary>
        /// <returns>Filename or null if topic file doesn't exist or topic filename is a URL or other format</returns>
        public string GetTopicFileName()
        {
            if (Link != null && ( Link.StartsWith("http://") || Link.StartsWith("https://")))
                return null;

            if (string.IsNullOrEmpty(Project?.ProjectDirectory))
                return null;

            if (!string.IsNullOrEmpty(Link))
            {
                
                string file = Path.Combine(Project.ProjectDirectory, Link);
                if (File.Exists(file))
                    return file;
            }

            return Path.Combine(Project.ProjectDirectory,
                Slug + (Type == TopicBodyFormats.Html ? ".html" : ".md"));
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
                        Task.Delay(50);
                        if (i > 2)
                            return false;
                    }
                }

                // normalize line feeds
                _body = _body.Replace("\r\n", "\n");
                
                return UpdateTopicFromYaml(_body,this);
            }

            return false;
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
                    .WithNamingConvention(new CamelCaseNamingConvention())
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
        /// Saves body field content to a Markdown file with the slug as a name
        /// </summary>
        /// <param name="markdownText"></param>
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

            var serializer = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())                   
                .Build();
             
            
            markdownText = StripYaml(markdownText);

            if (string.IsNullOrEmpty(Title))
                Title = GetTitleHeader(markdownText);

            if (Project != null && Project.StoreYamlInTopics)
            {
                string yaml = serializer.Serialize(this);
                markdownText = $"---\n{yaml}---\n{markdownText}";
            }

            string file = null;
            if (!string.IsNullOrEmpty(Project?.ProjectDirectory))
            {
                file = GetExternalFilename();

                if (string.IsNullOrEmpty(markdownText))
                    try
                    {                        
                        File.Delete(file);
                    }
                    catch { }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            File.WriteAllText(file, markdownText, Encoding.UTF8);
                            break;
                        }
                        catch
                        {
                            _body = markdownText;
                            Task.Delay(50);
                            if (i > 3)
                                return false;
                        }
                    }
                }
            }
            else
                return false;

            Debug.WriteLine($"Save TopicFile done: {this} {file}");
            return true;
        }

        #endregion

        #region Topic Body Helpers

        public static readonly Regex YamlExtractionRegex = new Regex(@"\A---[ \t]*\r?\n[\s\S]+?\r?\n(---|\.\.\.)[ \t]*\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);
        public static readonly Regex YamlExtractionTextOnlyRegex = new Regex(@"\A---[ \t]*\r?\n[\s\S]+?\r?\n(---|\.\.\.)[ \t]*\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);
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
        public string GetTopicLink(string displayText,
            string anchor = null, 
            string attributes = null,             
            HtmlRenderModes mode = HtmlRenderModes.None, 
            string link = null)
        {            
            string anchorString = (string.IsNullOrEmpty(anchor) ? "" : "#" + anchor);
            string linkText = HtmlUtils.HtmlEncode(displayText);
            if (link == null)
                link = Slug;

            if (mode == HtmlRenderModes.None)
                mode = Project.ActiveRenderMode;

            // Plain HTML
            if (mode == HtmlRenderModes.Html)
                link = $"<a href='_{StringUtils.UrlEncode(link)}.html' {anchorString} {attributes}>{linkText}</a>";
            // Preview Mode
            else if (mode == HtmlRenderModes.Preview)
                link = $"<a href='dm://Topic/{StringUtils.UrlEncode(link)}' {anchorString} {attributes}>{linkText}</a>";
            if (mode == HtmlRenderModes.Print)
                link = $"<a href='#{StringUtils.UrlEncode(link)}' {anchorString} {attributes}>{linkText}</a>";

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
            return $"{Id} - {Title}";
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            TopicState.IsDirty = true;
        }
        
        #endregion

        
    }

    public class ClassInfo
    {
        public string Signature { get; set; }

        public string Classname { get; set; }

        public string MemberName { get; set; }

        public string Scope { get; set; }

        public string Parameters { get; set; }

        public string Returns { get; set; }

        public string Syntax { get; set; }

        public string Inherits { get; set; }
        public string Implements { get; set; }

        public string InheritanceTree { get; set; }

        public string Assembly { get; set; }
        public string Contract { get; set; }
        public string Namespace { get; set; }
        public bool Static { get; set; }
	    public string Exceptions { get; set; }


        public override string ToString()
        {
            return Signature ?? base.ToString();
        }
    }



    public class TopicBodyFormats
    {
        public static List<string> TopicBodyFormatsList => new List<string>()
            {
                Markdown,
                Html,
                HelpBuilder,
                ImageUrl,                
                VstsWorkItem,
                VstsWorkItemQuery,
                VstsWorkItemQueries,
                VstsWorkItemQueryToc,
                VstsWorkItemQueriesToc,
            };

        public static string Markdown => "markdown";
        public static string Html => "html";
        public static string ImageUrl => "imageurl";
        public static string HelpBuilder => "helpbuilder";
        public static string VstsWorkItemQueries => "vsts-workitem-queries";
        public static string VstsWorkItem => "vsts-workitem";
        public static string VstsWorkItemQuery => "vsts-workitem-query";
        public static string VstsWorkItemQueryToc => "vsts-workitem-query:toc";
        public static string VstsWorkItemQueriesToc => "vsts-workitem-queries:toc";
    }

    
    
}

