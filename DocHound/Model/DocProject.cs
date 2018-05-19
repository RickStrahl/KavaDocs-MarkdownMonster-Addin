using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using DocHound.Annotations;
using DocHound.Configuration;
using DocHound.Razor;
using MarkdownMonster;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace DocHound.Model
{


    /// <summary>
    /// A Kava Docs Project File that contains
    /// the project header and a hierarchical 
    /// Topics collection
    /// </summary>
    [DebuggerDisplay("{Title} - {Filename}")]
    public class DocProject : INotifyPropertyChanged
    {
        /// <summary>
        ///  The descriptive name of the project
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }
        private string _title;


        
        /// <summary>
        /// An optional owner/company name that owns this project
        /// </summary>
        public string Owner
        {
            get { return _owner; }
            set
            {
                if (value == _owner) return;
                _owner = value;
                OnPropertyChanged();
            }
        }
        private string _owner;


        /// <summary>
        /// The project's base URL from which files are loaded
        /// </summary>
        public string BaseUrl
        {
            get { return _baseUrl; }
            set
            {
                if (value == _baseUrl) return;
                _baseUrl = value;
                OnPropertyChanged();
            }
        }    
        private string _baseUrl = "http://markdownmonster.west-wind.com/docs/";


        /// <summary>
        /// File name of the project.
        /// </summary>
        [JsonIgnore]
        public string Filename
        {
            get { return _filename; }
            set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProjectDirectory));
                OnPropertyChanged(nameof(OutputDirectory));
            }
        }
        private string _filename;


        [JsonIgnore]
        /// <summary>
        /// The base folder where the project is located.
        /// Used as the base folder to find related Markdown content files
        /// </summary>
        public string ProjectDirectory {
            get
            {
                if (!string.IsNullOrEmpty(Filename))
                    return Path.GetDirectoryName(Filename);

                return null;
            }
        }

        /// <summary>
        /// Folder where the actual HTML is generated.
        /// </summary>
        [JsonIgnore]
        public string OutputDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(Filename))
                    return null;

                return Path.Combine(ProjectDirectory, "wwwroot");
            }
        }


        /// <summary>
        /// Kava Docs Version used to create this file
        /// </summary>
        public string Version
        {
            get { return _version; }
            set
            {
                if (value == _version) return;
                _version = value;
                OnPropertyChanged();
            }
        }
        private string _version;



        /// <summary>
        /// Language of the help file
        /// </summary>
        public string Language
        {
            get { return _language; }
            set
            {
                if (value == _language) return;
                _language = value;
                OnPropertyChanged();
            }
        }
        private string _language = "en-US";

        
        #region Related Entities

        [JsonIgnore]
        public DocTopic Topic { get; set; }

        /// <summary>
        /// Topic list
        /// </summary>
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
        private ObservableCollection<DocTopic> _topics = new ObservableCollection<DocTopic>();

        /// <summary>
        /// A list of custom topics that are available in this help file
        /// </summary>
        public Dictionary<string, string> CustomFields { get; set; }


        /// <summary>
        /// KavaDocs Project Settings - these are projected to
        /// settings from the Settings dictionary.
        /// </summary>
        [JsonIgnore]
        public DocProjectSettings ProjectSettings { get; set; }

        /// <summary>
        /// KavaDocs online processing settings
        /// </summary>
        public Dictionary<string,object> Settings { get; set; }

    

        #endregion




        [JsonIgnore]
        /// <summary>
        /// Template Renderer used to render topic templates
        /// </summary>
        internal RazorTemplates TemplateRenderer
        {
            get
            {
                if (_templateRender == null)
                {
                    _templateRender = new RazorTemplates();
                    _templateRender.StartRazorHost(ProjectDirectory);
                }
                return _templateRender;
            }
            set
            {
                if (value == null)                
                    _templateRender?.StopRazorHost();               
                _templateRender = value;
            }
        }
        private RazorTemplates _templateRender;

        //public DocProjectConfiguration Configuration { get; set; }


        public DocProject()
        {                       
            Settings = new Dictionary<string, object>();

            // Make sure this is last
            ProjectSettings = new DocProjectSettings(this);
        }

        public DocProject(string filename = null) : this()
        {
            if (filename == null)
                filename = "DocumentationProject.json";
            Filename = filename;            
        }

        #region Topic Loading

        /// <summary>
        /// Loads a topic by id
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        public DocTopic LoadTopic(string topicId)
        {
            Topic = Topics.FirstOrDefault(t => t.Id == topicId);            
            return AfterTopicLoaded(Topic);
        }


        /// <summary>
        /// Loads a topic by its topic title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public DocTopic LoadByTitle(string title)
        {
            Topic = Topics.FirstOrDefault(t => !string.IsNullOrEmpty(t.Title) && t.Title.ToLower() == title.ToLower());            
            return AfterTopicLoaded(Topic);
        }


        /// <summary>
        /// Loads a topic by its topic slug
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public DocTopic LoadBySlug(string slug)
        {
            if (slug.StartsWith("_"))
                slug = slug.Substring(1);

            Topic = Topics.FirstOrDefault(t => t.Slug.ToLower() == slug.ToLower());            
            return AfterTopicLoaded(Topic);
        }

        /// <summary>
        /// Common code that performs after topic loading logic
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        protected DocTopic AfterTopicLoaded(DocTopic topic)
        {
            if (topic == null)
            {
                Topic = null;
                SetError("Topic not found.");
            }

            topic.Project = this;
            topic.TopicState.IsDirty = false;

            if (!topic.LoadTopicFile()) // load disk content
            {
                SetError("Topic body content not found.");
                return null;
            }

            return topic;
        }
        #endregion

        #region Topic Crud
        /// <summary>
        /// Removes an item from collection
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public void DeleteTopic(DocTopic topic)
        {
            if (topic == null)
                return;

            topic.DeleteTopicFile();

            if (topic.Parent?.Topics != null)
                topic.Parent.Topics.Remove(topic);
            else
                Topics.Remove(topic);
            
            topic = null;            
        }

        /// <summary>
        /// Creates a new topic and assigns it to the Topic property.
        /// </summary>
        /// <returns></returns>
        public DocTopic NewTopic()
        {
            Topic = new DocTopic(this);
            return Topic;
        }


      

        /// <summary>
        /// Saves a topic safely into the topic list.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note: This DOES NOT save the topic to disk tree to disk.
        /// The tree has to be explicitly updated
        /// </remarks>
        public bool SaveTopic(DocTopic topic = null)
        {
            if (topic == null)
                topic = Topic;
            if (topic == null)
                return false;

            var loadTopic = FindTopicInTree(topic,Topics);
            
            if (string.IsNullOrEmpty(topic.Title))
            {
                var lines = StringUtils.GetLines(topic.Body);
                var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                if (!string.IsNullOrEmpty(titleLine) && titleLine.Length > 2)
                    topic.Title = titleLine.Trim().Substring(2);
            }

            if (loadTopic == null)
            {
                using(var updateLock = new TopicFileUpdateLock())
                {
                    if (topic.Parent != null)
                        topic.Parent.Topics.Add(topic);
                    else
                        Topics.Add(topic);

                    return topic.SaveTopicFile();                    
                }
            }
            if (loadTopic.Id == topic.Id)
            {
                var topics = loadTopic.Parent?.Topics;
                if (topic == null)
                    topics = Topics;

                // Replace topic
                for (int i = 0; i < topics.Count; i++)
                {
                    var tpc = topics[i];
                    if (tpc == null)
                    {
                        using (var updateLock = new TopicFileUpdateLock())
                        {
                            topics.RemoveAt(i);
                        }
                        continue;
                    }
                    if (topics[i].Id == topic.Id)
                    {
                        using (var updateLock = new TopicFileUpdateLock())
                        {
                            topics[i] = topic;
                            return topic.SaveTopicFile();
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves a topic but doesn't load it into Active Topic 
        /// based on a topic id, slug or html link
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns>topic or null</returns>
        public DocTopic GetTopic(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            DocTopic topic = null;
            if (id.StartsWith("_"))
            {
                string strippedSlugId = id;
                if (!string.IsNullOrEmpty(strippedSlugId) && strippedSlugId.StartsWith("_"))
                    strippedSlugId = id.Substring(1);

                topic = Topics                    
                    .FirstOrDefault(t => t.Id == id ||
                                         t.Slug == id ||
                                         t.Slug == strippedSlugId);
                return topic;
            }
            if (id.StartsWith("msdn:"))
            {
                // for now just return as is
                throw new NotSupportedException();
            }
            else if (id.StartsWith("sig:"))
            {
                throw new NotSupportedException();
            }

            // Must be topic title
            topic = Topics                
                .FirstOrDefault(t => t.Title != null && t.Title.ToLower() == id.ToLower() ||
                                     t.Slug != null && t.Slug.ToLower() == id.ToLower());


            return topic;
        }

        /// <summary>
        /// Updates a topic from a markdown document
        /// </summary>
        /// <param name="doc">document to update from (from disk)</param>
        /// <param name="topic">topic to update</param>
        public void UpdateTopicFromMarkdown(MarkdownDocument doc, DocTopic topic, bool noBody = false)
        {
            var fileTopic = new DocTopic();
            fileTopic.Project = this;
            fileTopic.LoadTopicFile(doc.Filename); // make sure we have latest


            if (!string.IsNullOrEmpty(fileTopic.Title))
                topic.Title = fileTopic.Title;
            topic.DisplayType = fileTopic.DisplayType;
            if (!string.IsNullOrEmpty(fileTopic.Slug))
                topic.Slug = fileTopic.Slug;

            if (!string.IsNullOrEmpty(fileTopic.Link))
                topic.Link = fileTopic.Link;

            if (!noBody)
                topic.Body = fileTopic.Body;
        }


        /// <summary>
        /// Sets IsHidden and IsExpanded flags on topics in the tree 
        /// depending on a search match.
        /// </summary>
        /// <param name="topics"></param>
        /// <param name="searchPhrase"></param>
        /// <param name="nonRecursive"></param>
        public void FilterTopicsInTree(ObservableCollection<DocTopic> topics, string searchPhrase, bool nonRecursive)
        {
            if (topics == null || topics.Count < 1)
                return;
            
            foreach (var topic in topics)
            {
                if (string.IsNullOrEmpty(searchPhrase))
                    topic.TopicState.IsHidden = false;
                else if (topic.Title.IndexOf(searchPhrase, StringComparison.CurrentCultureIgnoreCase) < 0)
                    topic.TopicState.IsHidden = true;
                else
                    topic.TopicState.IsHidden = false;

                // Make parent topics visible and expanded
                if (!topic.TopicState.IsHidden)
                {
                    var parent = topic.Parent;
                    while (parent != null)
                    {
                        parent.TopicState.IsHidden = false;
                        if (!string.IsNullOrEmpty(searchPhrase))
                            parent.IsExpanded = true;
                        parent = parent.Parent;
                    }
                }

                if (!nonRecursive)
                    FilterTopicsInTree(topic.Topics, searchPhrase, false);
            }
        }
        #endregion


        #region Output Generation

        public void GenerateHtmlOutput()
        {
            
        }

        public void GenerateTableOfContents()
        {
            
        }

        #endregion

        #region Topic Names and Links

        /// <summary>
        /// Returns a topic A HTML link depending on which mode you're running in
        /// </summary>
        /// <param name="displayText"></param>
        /// <param name="id"></param>
        /// <param name="anchor"></param>
        /// <param name="attributes"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public string GetTopicLink(string displayText, 
            string id,
            string anchor = null, 
            string attributes = null,             
            HtmlRenderModes mode = HtmlRenderModes.None)
        {            
            string topicFile = GetTopicFilename(id);
            if (string.IsNullOrEmpty(topicFile))
                return null;

            string anchorString = (string.IsNullOrEmpty(anchor) ? "" : "#" + anchor);
            string linkText = HtmlUtils.HtmlEncode(displayText);
            string link = null;

            if (mode == HtmlRenderModes.None)
                mode = ProjectSettings.ActiveRenderMode;

            // Plain HTML
            if (mode == HtmlRenderModes.Html )
                link = $"<a href='{StringUtils.UrlEncode(topicFile)}' {anchorString} {attributes}>{linkText}</a>";
            // Preview Mode
            else if (mode == HtmlRenderModes.Preview)
                link = $"<a href='dm://Topic/{StringUtils.UrlEncode(id)}' {anchorString} {attributes}>{linkText}</a>";
            if (mode == HtmlRenderModes.Print)
                link = $"<a href='#{StringUtils.UrlEncode(topicFile)}' {anchorString} {attributes}>{linkText}</a>";

            return link;
        }

        /// <summary>
        /// Returns the file name of a specific topic
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTopicFilename(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            
            string topicId = null;
            if (id.StartsWith("_"))
            {
                string strippedSlugId = id;
                if (!string.IsNullOrEmpty(strippedSlugId) && strippedSlugId.StartsWith("_"))
                    strippedSlugId = id.Substring(1);

                topicId = Topics
                    .Where(t => t.Id == id ||
                                t.Slug == id ||
                                t.Slug == strippedSlugId)
                    .Select(t => t.Slug)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(topicId))
                    return topicId;
            }
            else if (id.StartsWith("msdn:"))
            {
                // for now just return as is
                return id;
            }
            else if (id.StartsWith("sig:"))
            {
                return id;
            }

            // Must be topic title
            topicId = Topics
                .Where(t => t.Title != null && t.Title.ToLower() == id.ToLower())
                .Select(t => t.Slug)
                .FirstOrDefault();

            if (topicId == null)
                topicId = id;

            return "_" + topicId + ".html";
        }

        #endregion


        #region Load and Save to File

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static DocProject LoadProject(string filename)
        {
            var project = DocProjectManager.Current.LoadProject(filename);
            if (project == null)
                return null;

            foreach (var topic in project.Topics)
            {
                topic.Project = project;
            }
            
            return project;
        }

        /// <summary>
        /// Creates a new project based on given parameters
        /// </summary>
        /// <param name="creator"></param>
        /// <returns></returns>
        public static DocProject CreateProject(DocProjectCreator creator)
        {
            return DocProjectManager.CreateProject(creator);
        }

        public bool SaveProject(string filename = null)
        {
            if (string.IsNullOrEmpty(filename))
                filename = Filename;
            
            if (!DocProjectManager.Current.SaveProject(this, filename))
            {
                SetError(DocProjectManager.Current.ErrorMessage);
                return false;
            }

            return true;
        }

        public void SaveProjectAsync(string filename = null)
        {
            Task.Run(() =>
            {
                if (string.IsNullOrEmpty(filename))
                    filename = Filename;

                if (!DocProjectManager.Current.SaveProject(this, filename))                
                    Dispatcher.CurrentDispatcher.Invoke(()=> SetError(DocProjectManager.Current.ErrorMessage));                                
            });
        }

        /// <summary>
        /// Cleans up the current project
        /// </summary>
        public void CloseProject()
        {

            var config = KavaDocsConfiguration.Current;
            config.LastProjectFile = Filename;

            config.AddRecentProjectItem(Filename, Topic?.Id);

            File.Delete(Path.Combine(ProjectDirectory, DocTopic.KavaDocsEditorFilename));
        }

        #endregion

        #region Topic List from Flat List


        /// <summary>
        /// Gets a flat list of topics. This is faster and more efficient
        /// to work with.
        /// </summary>
        /// <returns></returns>
        public List<DocTopic> GetTopics()
        {            
            return Topics
                .OrderBy(t=> t.ParentId)
                .ThenByDescending(t => t.SortOrder)
                .ThenBy(t => t.DisplayType)
                .ThenBy(t => t.Title)
                .ToList();
        }



         /// <summary>
        /// Converts a flat topic list as a tree of nested topics
        /// </summary>
        /// <remarks>
        /// Assumes this is the root collection - ie. parent Id and Parent get set to empty
        /// </remarks>
        /// <param name="topicList"></param>
        /// <returns></returns>
        public void GetTopicTreeFromFlatList(ObservableCollection<DocTopic> topics)
        {
            if (topics == null)
                return;

            var list = new ObservableCollection<DocTopic>();

            // need to copy so we can clear the root collection
            var allTopics = new ObservableCollection<DocTopic>(topics);
            
            var topicsList = topics.Where(t => string.IsNullOrEmpty(t.ParentId))
                .OrderByDescending(t => t.SortOrder)
                .ThenByDescending(t => t.DisplayType)
                .ThenBy(t => t.Title)
                .ToList();

            topics.Clear();
            foreach (var top in topicsList)
            {
                GetChildTopicsForTopicFromFlatList(allTopics, top);
                
                top.ParentId = null;
                top.Parent = null;
                top.Project = this;
                topics.Add(top);                
            }
                        
        }


        /// <summary>
        /// Recursively fills all topics with subtopics
        /// </summary>
        /// <param name="topics"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public void GetChildTopicsForTopicFromFlatList(ObservableCollection<DocTopic> topics, DocTopic topic)
        {

            if (topics == null)
                topics = new ObservableCollection<DocTopic>();

            var query= topics.Where(t => t.ParentId == topic.Id);

            if (ProjectSettings.AutoSortTopics)
            {    query = query
                    .OrderByDescending(t => t.SortOrder)
                    .ThenBy(t => t.DisplayType)
                    .ThenBy(t => t.Title);
            }

            var children = query.ToList();            

            if (topic.Topics != null)
                topic.Topics.Clear();
            else
                topic.Topics = new ObservableCollection<DocTopic>();
                    
            foreach (var childTopic in children)
            {                
                childTopic.Parent = topic;
                childTopic.ParentId = topic.Id;
                childTopic.Project = this;
                topic.Topics.Add(childTopic);
            }            
        }

        #endregion

        #region Fix up Tree to ensure ids and parents are set

        /// <summary>
        /// Fixes up a topic tree so that parent, id, parent ID and all other
        /// related dependencies are fixed up properly.
        /// </summary>
        /// <param name="topicList"></param>
        /// <returns></returns>
        public void GetTopicTree(ObservableCollection<DocTopic> topics = null)
        {
            if (topics == null)
                topics = Topics;

            var list = new ObservableCollection<DocTopic>();

            var query = topics
                .OrderByDescending(t => t.SortOrder);

            if (ProjectSettings.AutoSortTopics)
                query = query.ThenBy(t => t.DisplayType).ThenBy(t => t.Title);

            var topicList = query.ToList();
                                                   

            topics.Clear();
            foreach (var top in topicList)
            {
                GetChildTopicsForTopic(top);
                top.Project = this;
                topics.Add(top);                
            }                       
        }


        /// <summary>
        /// Recursively fills all topics with subtopics 
        /// reorders them and assigns the appropriate 
        /// hierarchical ids and dependencies
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="topicList"></param>
        /// <returns></returns>
        public void GetChildTopicsForTopic(DocTopic topic)
        {
            if (topic.Topics == null)
            {
                topic.Topics = new ObservableCollection<DocTopic>();
                return;
            }

            var children = topic.Topics
                .OrderByDescending(t => t.SortOrder)
                .ThenBy(t => t.DisplayType)
                .ThenBy(t => t.Title).ToList();

            foreach (var childTopic in children)
            {
                GetChildTopicsForTopic(childTopic);
                childTopic.Parent = topic;
                childTopic.ParentId = topic.Id;
                childTopic.Project = this;
            }
            
        }

        /// <summary>
        /// Finds a topic in the tree
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="rootTopics"></param>
        /// <returns></returns>
        public DocTopic FindTopicInTree( DocTopic topic, ObservableCollection<DocTopic> rootTopics = null)
        {
            if (rootTopics == null)
                rootTopics = Topics;
            
            if (rootTopics == null)
                    return null;

            foreach (var top in rootTopics)
            {
                if (top == topic)
                    return topic;

                var foundTopic = FindTopicInTree(top, top.Topics);
                if (foundTopic != null)
                    return topic;
            }

            return null;
        }

        public void WriteTopicTree(ObservableCollection<DocTopic> topics, int level, StringBuilder sb)
        {
                if (topics == null || topics.Count < 1)
                    return;

                sb.AppendLine("    " + level);
                foreach (var topic in topics)
                {
                    sb.AppendLine(new string(' ', level * 2) +
                                      $"{topic.Title} - {topic.Id} {topic.ParentId} {topic.Project != null}");

                    WriteTopicTree(topic.Topics, level + 1,sb);
                }            
        }

        public DocTopic FindTopicInFlatTree(DocTopic topic, ObservableCollection<DocTopic> rootTopics = null)
        {
            if (rootTopics == null)
                rootTopics = Topics;

            return rootTopics?.FirstOrDefault(t => t.Id == topic.Id);
        }


        #endregion


        #region Settings Handling
       
        public string GetSetting(string key, string defaultValue = null)
        {
            if (Settings.TryGetValue(key, out object result))
                return result as string;

            return defaultValue;
        }
        public bool GetSetting(string key, bool defaultValue)
        {
            if (Settings.TryGetValue(key, out object result))
                return (bool) result;

            return defaultValue;
        }
        
        
        public T GetSetting<T>(string key, object defaultValue = null)
        {
            if (Settings.TryGetValue(key, out object result))
                return (T)result;

            if (defaultValue == null)
                return default(T);

            return (T)defaultValue;
        }

        public void SetSetting(string key, object value)
        {
            Settings[key] = value;
        }

        #endregion



        #region Error Handling
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
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                ErrorMessage = string.Empty;

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
        
        #endregion
    }


    public enum HtmlRenderModes
    {
        Html,
        Preview, 
        Print,
        None
    }

}
