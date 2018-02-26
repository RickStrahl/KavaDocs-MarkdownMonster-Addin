using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public string Id { get; set; }


        /// <summary>
        /// Parent Id for any parent topics.
        /// The parent id allows for flat queries
        /// of items so we can do things like delete
        /// children easily.
        /// </summary>
        [YamlIgnore]
        public string ParentId { get; set; }


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
            get { return _link; }
            set { _link = value; }
        }
        private string _link;


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
        public string Type
        {
            get
            {
                var type = _type?.ToLower();
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
                if (value == _type) return;

                // don't empty null types
                if (string.IsNullOrEmpty(value))
                {
                    if (Topics != null && Topics.Count > 0)
                        value= "header";
                    else
                        value= "topic";                    
                }
                _type = value.ToLower();
                OnPropertyChanged(nameof(Type));
                TopicState.OnPropertyChanged(nameof(TopicState.ImageFilename));
            }
        }
        private string _type;


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
                if (value == _body) return;
                _body = value;
                OnPropertyChanged();
            }
        }

        private string _body;

        public TopicBodyFormats BodyFormat { get; set; } = TopicBodyFormats.Markdown;

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
        public string Example { get; set; }

        public bool IsLink { get; set; }

        public int SortOrder { get; set; }

        public bool Incomplete { get; set; }

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
                    TopicState.IsHidden = true;
            }
        }
        private bool _isExpanded;

        


        [YamlIgnore]
        public DateTime Updated { get; set; }
        
        [YamlIgnore]
        public string HelpId { get; set; }

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
                OnPropertyChanged();
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
            Id = "_" + DataUtils.GenerateUniqueId(9);
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
            string html = Project.TemplateRenderer.RenderTemplate(Type + ".cshtml", this, out error);
            
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

            if (filename == null)
            {
                filename = RenderTopicFilename;
            }
            try
            {
                File.WriteAllText(filename,html);
            }
            catch
            {
                Thread.Sleep(2);
                try
                {
                    File.WriteAllText(filename,html);
                }
                catch(Exception ex)
                {
                    SetError("Failed to render topic file: " + ex.Message + "\r\n" + filename);
                    return null;
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
                Slug + (BodyFormat == TopicBodyFormats.Html ? ".html" : ".md"));
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




        public static readonly Regex YamlExtractionRegex = new Regex(@"\A---[ \t]*\r?\n[\s\S]+?\r?\n(---|\.\.\.)[ \t]*\r?\n", RegexOptions.Multiline | RegexOptions.Compiled);

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
                        Task.Delay(5);
                        if (i > 2)
                            return false;
                    }
                }


                if (_body.StartsWith("---\n") || _body.StartsWith("---\r"))
                {
                    string extractedYaml = null;

                    var match = MarkdownUtilities.YamlExtractionRegex.Match(_body);
                    if (match.Success)
                        extractedYaml = match.Value;

                    //var extractedYaml = StringUtils.ExtractString(markdown.TrimStart(), "---\n", "\n---\n",returnDelimiters: true);
                    if (string.IsNullOrEmpty(extractedYaml))
                        return true;

                    var yaml = StringUtils.ExtractString(_body, "---", "\n---", returnDelimiters: false).Trim();
                    var sr = new StringReader(yaml);
                    var deserializer = new DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .Build();

                    DocTopic imported;
                    try
                    {
                        // TODO: Better parsing of YAML data
                        var yamlObject = deserializer.Deserialize<DocTopic>(sr);

                        if (string.IsNullOrEmpty(Id))
                            Id = yamlObject.Id;

                        Title = yamlObject.Title;
                        Link = yamlObject.Link;
                        Slug = yamlObject.Slug;
                        Keywords = yamlObject.Keywords;
                        Type = yamlObject.Type;
                        SeeAlso = yamlObject.SeeAlso;
                        Properties = yamlObject.Properties;
                        ClassInfo = yamlObject.ClassInfo;
                        SortOrder = yamlObject.SortOrder;
                    }
                    catch
                    {
                        return false;
                    }
                    
                    _body  = _body.Replace(extractedYaml, "");

                    if (string.IsNullOrEmpty(Title))
                    {
                        var lines = StringUtils.GetLines(Body);
                        var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                        if (!string.IsNullOrEmpty(titleLine) && titleLine.Length > 2)
                            Title = titleLine.Trim().Substring(2);
                    }
                }

            }
            else
                return false;

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

            if (string.IsNullOrEmpty(Title))
            {
                var lines = StringUtils.GetLines(Body,100);
                var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                if (!string.IsNullOrEmpty(titleLine) && titleLine.Length > 2)
                    Title = titleLine.Trim().Substring(2);
            }

            string yaml = serializer.Serialize(this);
            markdownText = $"---\n{yaml}kavaDocs: true\n---\n{markdownText}";
            
            
            if (!string.IsNullOrEmpty(Project?.ProjectDirectory))
            {
                var file = GetExternalFilename();

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
                            File.WriteAllText(file, markdownText);
                            break;
                        }
                        catch
                        {
                            _body = markdownText;
                            Task.Delay(5);
                            if (i > 2)
                                return false;
                        }
                    }
                }
            }
            else
                return false;

            return true;
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
                    filename + (BodyFormat == TopicBodyFormats.Html ? ".html" : ".md"));
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

        
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Id} - {Title}";
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


    public enum TopicBodyFormats
    {
        Markdown,
        Html,
        HelpBuilder,
        PlainText,
        None        
    }
}

