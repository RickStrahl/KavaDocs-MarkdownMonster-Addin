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
using DocHound.Model;
using DocHound.Annotations;
using DocHound.Utilities;
using MarkdownMonster.Windows;

namespace KavaDocsAddin.Controls
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
                var parentTopic = AppModel.ActiveProject.Topics.First(tp => tp.Id == topic.ParentId);
                topicList.Add(parentTopic);
                topic = parentTopic;
            }

            return topicList;
        }

        public KavaDocsModel AppModel { get; }

        public TopicsTreeModel(DocProject project)
        {
            AppModel = kavaUi.AddinModel;

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
