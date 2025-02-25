using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using DocMonster.Annotations;
using DocMonster.Configuration;
using DocMonster.Templates;
using Newtonsoft.Json;
using Westwind.Utilities;
using MarkdownMonster;
using System.Text.RegularExpressions;
using System.Threading;
using Westwind.AI.Chat;
using System.Linq.Expressions;

namespace DocMonster.Model
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


        public string Byline
        {
            get { return _Byline; }
            set
            {
                if (value == _Byline) return;
                _Byline = value;
                OnPropertyChanged(nameof(Byline));
            }
        }
        private string _Byline;




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

        /// <summary>
        /// Online project's menu options
        /// </summary>
        public List<MenuLink> Menu { get; set; } = new List<MenuLink>();


        #region Related Entities

        
        [JsonIgnore]
        public DocTopic Topic
        {
            get => _topic;
            set { _topic = value;  }
        }
        private DocTopic _topic;


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
        public DocProjectSettings Settings { get; set; }

        #endregion

        #region Internal State


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
        public string ProjectDirectory
        {
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
        


        [JsonIgnore]
        /// <summary>
        /// Template Renderer used to render topic templates
        /// </summary>
        internal TemplateHost TemplateHost
        {
            get
            {
                if (_templateHost == null)
                {
                    _templateHost = new TemplateHost();
                }

                return _templateHost;
            }
            set
            {                    
                _templateHost = value;
            }
        }
        private TemplateHost _templateHost;

        //public DocProjectConfiguration Configuration { get; set; }

        #endregion


        #region Topic Loading

        public DocProject()
        {            
            // Make sure this is last
            Settings = new DocProjectSettings(this);
        }

        public DocProject(string filename = null) : this()
        {
            if (filename == null)
                filename = "DocumentationProject.json";
            Filename = filename;
        }


        /// <summary>
        /// Generic Topic loader that works with 
        /// Loads a topic by topic id, or by dm-topic://, dm-slug:// or dm-title://
        /// link.
        /// </summary>
        /// <param name="topicId">Topic Id or dm:// link</param>
        /// <param name="dontAssignProjectTopic">If true doesn't assign the Topic property</param>
        /// <returns>Topic or null</returns>
        public DocTopic LoadTopic(string topicId, bool dontAssignProjectTopic = false)
        {
            if (string.IsNullOrEmpty(topicId))
                return null;

            topicId = WebUtility.UrlDecode(topicId).Trim();

            DocTopic topic = null;
            if (topicId.StartsWith('_') || topicId.Equals("index",StringComparison.OrdinalIgnoreCase))
                topic = LoadTopicById(topicId);

            else if (topicId.StartsWith("dm-topic://", StringComparison.OrdinalIgnoreCase))
                topic = LoadTopicById(topicId.Replace("dm-topic://", string.Empty));

            else if (topicId.StartsWith("dm-slug://", StringComparison.OrdinalIgnoreCase))
                topic = LoadTopicBySlug(topicId.Replace("dm-slug://", string.Empty));

            else if (topicId.StartsWith("dm-title://", StringComparison.OrdinalIgnoreCase))
                topic = LoadTopicByTitle(topicId.Replace("dm-title://", string.Empty));

            else if (topicId.StartsWith("vfps://", StringComparison.OrdinalIgnoreCase))
            {
                if (topicId.Contains("/topic/")) 
                {
                    topicId = topicId.Replace("vfps://topic/", string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (topicId.StartsWith('_'))
                        topic = LoadTopicById(topicId);
                    else
                        topic = LoadTopicByTitle(topicId);
                }
            }

            // fallback to slug and/title
            if (topic == null)
            {
                topic = LoadTopicBySlug(topicId, true);
            }

            if (!dontAssignProjectTopic)
                Topic = topic;

            if (topic == null)
            {
                SetError("Topic not found.");
                return null;
            }

            return AfterTopicLoaded(topic);
        }

        public DocTopic LoadTopicById(string topicId)
        {
            DocTopic match = null;

            using var tokenSource = new CancellationTokenSource();

            WalkTopicsHierarchy(Topics, (topic, project, token) => {
                if (match is not null || string.IsNullOrEmpty(topic.Id)) return;

                if (topic.Id.Equals(topicId, StringComparison.OrdinalIgnoreCase))
                {
                    match = topic;
                    tokenSource.Cancel();
                }                
            }, tokenSource.Token);

            match = AfterTopicLoaded(match);
            return match;
        }


        /// <summary>
        /// Loads a topic by its topic title and also searches by slug optionally
        /// </summary>
        /// <param name="title">Title to search for</param>
        /// <param name="alsoSearchBySlug">Search for title in slug also. Title first</param>
        /// <returns></returns>
        public DocTopic LoadTopicByTitle(string title, bool alsoSearchBySlug = false)
        {
            DocTopic match = null;
            using var tokenSource = new CancellationTokenSource();

            WalkTopicsHierarchy(Topics, (topic, project, token) => {
                if (match is not null || string.IsNullOrEmpty(topic.Title)) return;

                if (topic.Title.Trim().Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    match = topic;                    
                }
                else if (alsoSearchBySlug && topic.Slug is not null && topic.Slug.Trim().Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    match = topic;                    
                }
            },tokenSource.Token);
         
            match =  AfterTopicLoaded(match);
            return match;
        }


        /// <summary>
        /// Loads a topic by its topic slug (or title if alsoSearchByTitle is true)
        /// </summary>
        /// <param name="slug">Slug to search for</param>
        /// <param name="alsoSearchByTitle">If true searches both by slug *and* title - slug searched first</param>
        /// <returns>topic or null</returns>
        public DocTopic LoadTopicBySlug(string slug, bool alsoSearchByTitle = false)
        {
            DocTopic match = null;

            WalkTopicsHierarchy(Topics, (topic, project) => {
                if (match is not null || string.IsNullOrEmpty(topic.Title)) return;

                if (topic.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
                {
                    match = topic;
                    return;
                }
                if (alsoSearchByTitle && topic.Title.Equals(slug, StringComparison.OrdinalIgnoreCase))
                {
                    match = topic;
                    return;
                }
            });

            match = AfterTopicLoaded(match);
            return match;
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
                SetError("Topic not found.");
                return null;
            }

            topic.Project = this;

            topic.TopicState.IsDirty = false;
            topic.TopicState.OldLink = null;
            topic.TopicState.OldSlug = null;

            topic.LoadTopicFile(); // load disk content
            //if (!topic.LoadTopicFile()) // load disk content
            //{
            //    SetError("Topic body content not found.");
            //    return null;
            //}

            return topic;
        }

        #endregion

        #region Topic Crud

        /// <summary>
        /// Deletes a topic and its child topics. Optionally allows deleting
        /// just the child topics.
        /// </summary>
        /// <param name="topic">Topic to delete with child topics</param>
        /// <param name="childTopicsOnly">If true only delete child topics</param>
        /// <returns></returns>
        public void DeleteTopic(DocTopic topic, bool childTopicsOnly = false)
        {
            if (topic == null)
                return;

            if (topic.Topics.Count > 0)
            {
                var childTopics = topic.Topics.ToList();
                foreach (var childTopic in childTopics)
                {
                    DeleteTopic(childTopic);
                }
            }

            Topic.IsExpanded = false;
            
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
        /// The project  has to be explicitly saved to save topics to file
        /// </remarks>
        public bool SaveTopic(DocTopic topic = null)
        {
            if (topic == null)
                topic = Topic;
            if (topic == null)
                return false;

            var loadTopic = FindTopicInTree(topic, Topics);

            if (string.IsNullOrEmpty(topic.Title))
            {
                var lines = StringUtils.GetLines(topic.Body);
                var titleLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                if (!string.IsNullOrEmpty(titleLine) && titleLine.Length > 2)
                    topic.Title = titleLine.Trim().Substring(2);
            }

            if (topic.TopicState.IsDirty)
                topic.Updated = DateTime.UtcNow.Date;

            if (loadTopic == null)
            {
                using (var updateLock = new TopicFileUpdateLock())
                {
                    if (topic.Parent != null)
                        topic.Parent.Topics.Add(topic);
                    else
                        Topics.Add(topic);


                    if (topic.SaveTopicFile())
                    {
                        topic.TopicState.IsDirty = false;
                        topic.TopicState.OldLink = null;
                        topic.TopicState.OldSlug = null;
                        return true;
                    }
                    return false;
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
                            if (topic.SaveTopicFile())
                            {
                                topic.TopicState.IsDirty = false;
                                topic.TopicState.OldLink = null;
                                topic.TopicState.OldSlug = null;
                                return true;
                            }
                            return false;
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
        public void UpdateTopicFromMarkdown(MarkdownMonster.MarkdownDocument doc, DocTopic topic, bool noBody = false)
        {
            var fileTopic = new DocTopic();
            fileTopic.Project = this;
            fileTopic.DisplayType = string.Empty;
            fileTopic.Type = null;
            fileTopic.LoadTopicFile(doc.Filename); // make sure we have latest


            if (!string.IsNullOrEmpty(fileTopic.Title))
                topic.Title = fileTopic.Title;
            
            if (!string.IsNullOrEmpty(fileTopic.Slug))
                topic.Slug = fileTopic.Slug;

            if (!string.IsNullOrEmpty(fileTopic.Link))
                topic.Link = fileTopic.Link;

            // TODO: Figure out how we can detect whether this has changed or is the default
            //       Problem is that DisplayType gets set to a default if n
            if (!string.IsNullOrEmpty(fileTopic.DisplayType))
                topic.DisplayType = fileTopic.DisplayType;


            if (!string.IsNullOrEmpty(fileTopic.Type))
                topic.Type = fileTopic.Type;



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

            var lastBaseSignature = "xxx";
            DocTopic lastMethodTopic = null;
            Dictionary<DocTopic, DocTopic> topicsToRemove = new();
            foreach (var topic in topics)
            {
                topic.TopicState.IsHidden = false;

                // Method/ctor overloads are appended below
                if (!topic.ClassInfo.IsEmpty)
                {         
                    var baseSignature = topic.ClassInfo.GetBaseMethodSignature();
                    if ((topic.DisplayType == "classmethod" || topic.DisplayType == "classconstructor") &&
                        lastBaseSignature == baseSignature)
                    {
                        topicsToRemove.Add(topic, topic.Parent);
                        lastMethodTopic.Topics.Add(topic);
                        topic.Parent = lastMethodTopic;                       
                        continue;
                    }
                    lastBaseSignature = baseSignature;
                    lastMethodTopic = topic;                  
                }

                    // Search handling
                    if (string.IsNullOrEmpty(searchPhrase))
                        topic.TopicState.IsHidden = false;
                    else if (!topic.Title.Contains(searchPhrase, StringComparison.OrdinalIgnoreCase))
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
                              

                if (!nonRecursive && topic.Topics.Count > 0)
                    FilterTopicsInTree(topic.Topics, searchPhrase, false);
            }

            foreach(var tr in topicsToRemove)
            {
                tr.Value.Topics.Remove(tr.Key);
            }    
        }


        /// <summary>
        /// This routine walks a topics tree and visits each topic and
        /// allows you to inspect each topic.
        /// </summary>
        /// <param name="topics"></param>
        /// <param name="onTopicHandler"></param>
        public void WalkTopicsHierarchy(ObservableCollection<DocTopic> topics, Action<DocTopic,DocProject> onTopicHandler)
        {
            if (topics == null)
                topics = Topics;

            foreach (var topic in topics)
            {
                onTopicHandler?.Invoke(topic, this);
                if(topic.Topics != null && topic.Topics.Count > 0)
                    WalkTopicsHierarchy(topic.Topics, onTopicHandler);                    
            }
        }
        public void WalkTopicsHierarchy(ObservableCollection<DocTopic> topics, Action<DocTopic, DocProject, CancellationToken> onTopicHandler, CancellationToken token)
        {
            if (topics == null)
                topics = Topics;

            foreach (var topic in topics)
            {
                if (token.IsCancellationRequested)
                    break;

                onTopicHandler?.Invoke(topic, this, token);
                if (topic.Topics != null && topic.Topics.Count > 0)
                {
                    if (token.IsCancellationRequested)
                        break;

                    WalkTopicsHierarchy(topic.Topics, onTopicHandler, token);
                }
            }
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

            string anchorString = (string.IsNullOrEmpty(anchor) ? string.Empty : "#" + anchor);
            string linkText = WebUtility.HtmlEncode(displayText);
            string link = null;

            if (mode == HtmlRenderModes.None)
                mode = Settings.ActiveRenderMode;

            // Plain HTML
            if (mode == HtmlRenderModes.Html)
                link = $"<a href='{StringUtils.UrlEncode(topicFile)}{anchorString}' {attributes}>{linkText}</a>";
            // Preview Mode
            else if (mode == HtmlRenderModes.Preview)
                link = $"<a href='dm-topic://{StringUtils.UrlEncode(id)}{anchorString}' {attributes}>{linkText}</a>";
            if (mode == HtmlRenderModes.Print)
                link = $"<a href='#{StringUtils.UrlEncode(topicFile)}{anchorString}' {attributes}>{linkText}</a>";

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
                    Dispatcher.CurrentDispatcher.Invoke(() => SetError(DocProjectManager.Current.ErrorMessage));
            });
        }

        /// <summary>
        /// Cleans up the current project
        /// </summary>
        public void CloseProject()
        {

            var config = DocMonsterConfiguration.Current;
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
                .OrderBy(t => t.ParentId)
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
                if (top.DisplayType == "config")
                    continue;

                GetChildTopicsForTopicFromFlatList(allTopics, top);

                top.ParentId = null;
                top.Parent = null;
                top.Project = this;

                topics.Add(top);

                top.SaveTopicFile();
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

            string topicId = topic.Id;
            if (topicId == "_INDEX")
                topicId = "INDEX";

            var query = topics.Where(t => t.ParentId == topicId);

            if (!Settings.AutoSortTopics)
            {
                query = query
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

                string slug = childTopic.CreateSlug();
                string baseSlug = topic.Slug;
                if (!string.IsNullOrEmpty(baseSlug))
                    baseSlug += "/";

                childTopic.Slug = baseSlug + slug;

                if (childTopic.IsHeaderTopic())
                    childTopic.Link = baseSlug + slug + "/" + slug + ".md";
                else
                    childTopic.Link = baseSlug + slug + ".md";

                topic.Topics.Add(childTopic);
                childTopic.SaveTopicFile();

                // check for additional children recursively
                GetChildTopicsForTopicFromFlatList(topics, childTopic);
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

            if (Settings.AutoSortTopics)
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
        public DocTopic FindTopicInTree(DocTopic topic, ObservableCollection<DocTopic> rootTopics = null)
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

        /// <summary>
        /// Finds a topic in the tree by comparing the link
        /// rather than an object reference. This allows finding
        /// topics that were not in the original project references        
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="rootTopics"></param>
        /// <returns></returns>
        public DocTopic FindTopicInTreeByValue(DocTopic topic, ObservableCollection<DocTopic> rootTopics = null)
        {
            if (rootTopics == null)
                rootTopics = Topics;

            if (rootTopics == null)
                return null;

            foreach (var top in rootTopics)
            {
                if (top.Link == topic.Link)
                    return top;

                var foundTopic = FindTopicInTree(top, top.Topics);
                if (foundTopic != null)
                    return foundTopic;
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

                WriteTopicTree(topic.Topics, level + 1, sb);
            }
        }

        public DocTopic FindTopicInFlatTree(DocTopic topic, ObservableCollection<DocTopic> rootTopics = null)
        {
            if (rootTopics == null)
                rootTopics = Topics;

            return rootTopics?.FirstOrDefault(t => t.Id == topic.Id);
        }


        #endregion

        #region Tools and Backup

        public bool BackupProject(string backupfolder = null, bool automaticBackup = false )
        {
            var folderName = FileUtils.SafeFilename(Title);
            var config = DocMonsterConfiguration.Current;
            var internalBackupFolder = Path.Combine(config.DocumentsFolder, "Backups",          
                folderName,
                automaticBackup ? "Automatic" : string.Empty,
                "Backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm"));

            if (string.IsNullOrEmpty(backupfolder))
            {
                backupfolder = internalBackupFolder;
            }
            

            try
            {
                if (!Directory.Exists(backupfolder))
                    Directory.CreateDirectory(backupfolder);

                FileUtils.CopyDirectory(ProjectDirectory, backupfolder, recursive: true);
            }
            catch {
                SetError("Couldn't create backup copy in " + backupfolder);
                return false;
            }

            try
            {
                Directory.Delete(Path.Combine(backupfolder, "wwwroot"), true);
                
                FileUtils.DeleteFiles(backupfolder, "~*.*", true);

                // delete old folders
                if (automaticBackup && backupfolder == internalBackupFolder)
                {
                    var path = Path.Combine(config.DocumentsFolder, "Backups", "Automatic");
                    var di = new DirectoryInfo(path);
                    var dirs = di.GetDirectories("Business_*.").OrderByDescending(d => d.LastWriteTimeUtc).Skip(config.AutomaticBackupCount);
                    foreach (var dir in dirs)
                    {
                        dir.Delete(true);
                    }
                }
            }
            catch { }

            


            return true;
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

    public class MenuLink
    {
        public string Title { get; set; }
        public string Link { get; set; }
    }

}
