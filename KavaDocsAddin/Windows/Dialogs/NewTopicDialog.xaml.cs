using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocHound.Annotations;

using DocHound.Annotations;
using DocHound.Model;
using KavaDocsAddin;
using KavaDocsAddin.Controls;
using MahApps.Metro.Controls;
using MarkdownMonster;

namespace DocHound.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for NewTopicDialog.xaml
    /// </summary>
    public partial class NewTopicDialog : INotifyPropertyChanged        
    {
        public MarkdownMonster.MainWindow Window
        {
            get { return _window; }
            set
            {
                if (Equals(value, _window)) return;
                _window = value;
                OnPropertyChanged();
            }
        }
        private MarkdownMonster.MainWindow _window;


        public AddinModel AppModel
        {
            get { return _appModel; }
            set
            {                
                if (_appModel == value) return;
                _appModel = value;
                OnPropertyChanged(nameof(AppModel));
            }
        }
        private AddinModel _appModel = kavaUi.AddinModel;

        
        public DocTopic Topic
        {
            get { return _topic; }
            set
            {
                if (Equals(value, _topic)) return;
                _topic = value;
                OnPropertyChanged();
            }
        }
        private DocTopic _topic;

        
        public string ParentTopicTitle
        {
            get { return _parentTopicTitle; }
            set
            {
                if (value == _parentTopicTitle) return;
                _parentTopicTitle = value;
                OnPropertyChanged();
            }
        }
        private string _parentTopicTitle;

        public string NewTopicLevel { get; set; }


        public List<TopicTypeListItem> TopicTypesList
        {
            get
            {
                if (AppModel.ActiveProject == null)
                    return null;

                var list = new List<TopicTypeListItem>();

                foreach (var type in AppModel.ActiveProject.TopicTypes)
                {
                    var item = new TopicTypeListItem()
                    {
                        Type = type.Key
                    };
                    list.Add(item);
                }
                return list;
            }
        }

        
        #region Initialization

        public NewTopicDialog(MainWindow window)
        {
            InitializeComponent();
            

            Owner = window;
            Window = window;
            AppModel = kavaUi.AddinModel;

            Topic = new DocTopic(AppModel.ActiveProject);

            Loaded += NewTopicDialog_Loaded;
        }
        

        private void NewTopicDialog_Loaded(object sender, RoutedEventArgs e)
        {
            SetTopicLevel(AppModel.ActiveTopic);
            DataContext = this;

            TextTopicTitle.Focus();
        }

        #endregion

        #region Topic Manipulation

        public DocTopic CreateTopic()
        {
            ButtonCreateTopic.Focus();
            
            if (RadioButtonBelow.IsChecked.Value)
            {
                Topic.ParentId = AppModel.ActiveTopic.Id;
                Topic.Parent = AppModel.ActiveTopic;

                AppModel.ActiveTopic.Topics.Insert(0, Topic);
                AppModel.ActiveTopic.IsExpanded = true;
            }
            else if (RadioButtonCurrent.IsChecked.Value)
            {

                var parentTopic = AppModel.ActiveTopic.Parent;
                string parentId = null;
                if (parentTopic != null)
                    parentId = parentTopic.Id;


                Topic.ParentId = parentId;
                Topic.Parent = parentTopic;
                if(AppModel.ActiveTopic.SortOrder > 0)
                    Topic.SortOrder = AppModel.ActiveTopic.SortOrder + 1;
                Topic.Type = AppModel.ActiveTopic.Type;

                if (parentTopic != null)                
                    parentTopic.Topics.Insert(0, Topic);
                else
                    AppModel.ActiveProject.Topics.Add(Topic);                        
                
            }
            else if (RadioButtonTop.IsChecked.Value)
            {
                Topic.ParentId = null;
                Topic.Parent = null;
                AppModel.ActiveProject.Topics.Add(Topic);
                  
            }

            // HACK: Filtered Collection won't update on its
            AppModel.TopicsTree.Model.OnPropertyChanged(nameof(TopicsTreeModel.FilteredTopicTree));
            //CollectionViewSource.GetDefaultView(Window.TopicsTree.Model.FilteredTopicTree).Refresh();

            Topic.TopicState.IsSelected = true;

            // make sure it gets written to disk
            AppModel.ActiveProject.SaveTopic(Topic);
            AppModel.ActiveProject.SaveProject();

            AppModel.ActiveTopic = Topic;
            AppModel.Window.PreviewMarkdownAsync();

            return Topic;
        }

        public void SetTopicLevel(DocTopic topic)
        {
            if (topic != null)
            {
                if (topic.Type == "header" ||
                    topic.Type == "classheader" || topic.Type== "index")
                {
                    Topic.ParentId = topic.Id;
                    ParentTopicTitle = topic.Title;
                    Topic.Type = "topic";
                    NewTopicLevel = "Below";
                    if (topic.Type == "classheader")
                        Topic.Type = "classmethod";

                    RadioButtonBelow.IsChecked = true;
                }
                else
                {
                    NewTopicLevel = "Current";
                    Topic.ParentId = topic.ParentId;
                    ParentTopicTitle = AppModel.ActiveProject.Topics
                        .FirstOrDefault(t => t.Id == Topic.ParentId)?.Title;
                    Topic.Type = topic.Type;
                    RadioButtonCurrent.IsChecked = true;
                }
            }
        }
        

        #endregion
        

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void Button_CreateTopicClick(object sender, RoutedEventArgs e)
        {
            if (CreateTopic() != null)
                Close();
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
