using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using DocMonster.Model;
using DocMonster.Annotations;
using DocMonster.Utilities;
using MarkdownMonster.Windows;
using MarkdownMonster;

namespace DocMonsterAddin.Controls
{
    public class TopicsTreeModel : INotifyPropertyChanged
    {
        
        public string TopicsFilter
        {
            get { return _topicsFilter; }
            set
            {
                if (value == _topicsFilter) return;

                if (value == "Search..." && _topicsFilter == "" ||
                    string.IsNullOrEmpty(value) && _topicsFilter == "Search...")
                {
                    _topicsFilter = value;
                    return;
                }
                _topicsFilter = value;
                
                OnPropertyChanged();

                // debounce the filter
                OnPropertyChanged(nameof(FilteredTopicTree));
                //debounceTopicsFilter.Debounce(500, e => OnPropertyChanged(nameof(FilteredTopicTree)));
            }
        }
        private string _topicsFilter;
        private readonly DebounceDispatcher debounceTopicsFilter = new DebounceDispatcher();

        public DocProject Project { get; set; }

        public ObservableCollection<DocTopic> TopicTree
        {
            get { return _topicTree; }
            set
            {
                if (Equals(value, _topicTree)) return;
                _topicTree = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredTopicTree));
            }
        }
        private ObservableCollection<DocTopic> _topicTree;        
        

        public ObservableCollection<DocTopic> FilteredTopicTree
        {            
            get
            {
                if (Project == null)
                    return null;

                Project.FilterTopicsInTree(Project.Topics, _topicsFilter, false);
                return Project.Topics;

                //ObservableCollection<DocTopic> topicTree;
;

                //AppModel.Window.ShowStatus("Filtering topics...");

                //if (!string.IsNullOrEmpty(_topicsFilter) && _topicsFilter.Length > 2 )
                //{
                //    var topicList = new List<DocTopic>();

                //    // First clear out all child topics and collect all matches
                //    foreach (var topic in Project.Topics)
                //    {
                //        topic.Topics = null;
                //        if (string.IsNullOrEmpty(topic.Title))
                //            continue;

                //        if (topic.Title.ToLower().Contains(_topicsFilter.ToLower()))
                //            topicList.Add(topic);                    
                //    }
                //    var parents = new List<DocTopic>();
                //    foreach (var topic in topicList)
                //    {
                //        var topicParents = GetParentTopics(topic);
                //        foreach (var p in topicParents)
                //            p.IsExpanded = true;

                //        foreach(var tp in topicParents)
                //            parents.Add(tp);                        
                //    }
                //    foreach (var prt in parents)
                //        topicList.Add(prt);

                //    // distinct
                //    var distinct = new ObservableCollection<DocTopic>(topicList.GroupBy(tp => tp.Id).Select(g => g.First()));
                    
                //    Project.GetTopicTree(distinct);
                //    AppModel.Window.ShowStatus();
                //    return Project.Topics;
                //}

                //Project.GetTopicTree();
                //AppModel.Window.ShowStatus();
                //return Project.Topics;
            }
        }

        IEnumerable<DocTopic> GetParentTopics(DocTopic topic)
        {
            List<DocTopic> topicList = new List<DocTopic>();
            
            while(!string.IsNullOrEmpty(topic.ParentId))
            {
                var parentTopic = DocMonsterModel.ActiveProject.Topics.First(tp => tp.Id == topic.ParentId);
                topicList.Add(parentTopic);
                topic = parentTopic;
            }

            return topicList;
        }

        public void SelectTopic(DocTopic topic)
        {
            var found = FindTopic(null, topic);
            if (found != null)
                found.TopicState.IsSelected = true;    
        }

        public void RefreshTree()
        {
            OnPropertyChanged(nameof(TopicTree));
            OnPropertyChanged(nameof(FilteredTopicTree));
        }


        /// <summary>
        /// Searches the tree for a specific item
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public DocTopic FindTopic(DocTopic parent, DocTopic topic)
        {
            var topics = parent?.Topics;
            if (topics == null)
                topics = Project.Topics;

            if (parent != null)
            {
                // check for root folder match
                if (parent.Link == topic.Link)
                    return parent;
            }

            foreach (var ttopic in topics)
            {
                if (ttopic.Link == topic.Link)
                    return ttopic;

                if (ttopic.Topics != null && ttopic.Topics.Count > 0)
                {
                    var ftopic = FindTopic(ttopic, topic);
                    if (ftopic != null)
                        return ftopic;
                }
            }

            return null;            
        }

        public DocMonsterModel DocMonsterModel { get; }

        public AppModel MarkdownMonsterModel { get;  }

        public TopicsTreeModel(DocProject project)
        {
            DocMonsterModel = kavaUi.AddinModel;
            MarkdownMonsterModel = kavaUi.MarkdownMonsterModel;

            Project = project;
            if (project != null)
                project.GetTopicTree();
            else
                TopicTree = new ObservableCollection<DocTopic>();
        }

      

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
