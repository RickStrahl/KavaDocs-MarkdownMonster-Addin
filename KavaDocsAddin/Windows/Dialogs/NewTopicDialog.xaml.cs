using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DocHound.Annotations;
using DocHound.Model;
using KavaDocsAddin;
using KavaDocsAddin.Controls;
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


        public KavaDocsModel AppModel
        {
            get { return _appModel; }
            set
            {                
                if (_appModel == value) return;
                _appModel = value;
                OnPropertyChanged(nameof(AppModel));
            }
        }
        private KavaDocsModel _appModel = kavaUi.AddinModel;

        
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


        // TODO: FIX UP NEW TOPIC PATH TO MATCH OLD TOPIC PATH
        //public string OldTopicPath
        //{
        //    get
        //    {
        //        var path = AppModel.ActiveTopic?.Link;
        //        if (path == null)
        //            return null;

        //        var ext = 

        //        return;
        //    }
        //}



        public bool IsHeaderTopic
        {
            get
            {
                if (Topic == null ||
                    Topic.DisplayType != "header" &&
                    Topic.DisplayType != "classheader")
                    return false;                

                return true;
            }
        }

        public List<DisplayTypeItem> TopicTypesList
        {
            get
            {
                if (AppModel.ActiveProject == null)
                    return null;

                var list = new List<DisplayTypeItem>();

                foreach (var type in AppModel.ActiveProject.ProjectSettings.TopicTypes)
                {
                    var item = new DisplayTypeItem()
                    {
                        DisplayType = type.Key
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
            mmApp.SetThemeWindowOverride(this);

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

                AppModel.ActiveTopic.Topics.Add(Topic);
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
                Topic.DisplayType = AppModel.ActiveTopic.DisplayType;

                
                if (parentTopic != null)
                {
                    var topicIndex = 0;
                    if (AppModel.ActiveTopic != null)
                    {
                        topicIndex = parentTopic.Topics.IndexOf(AppModel.ActiveTopic);
                        if (topicIndex < 0)
                            topicIndex = 0;
                    }

                    if (topicIndex < parentTopic.Topics.Count)
                    {
                        topicIndex++;
                        parentTopic.Topics.Insert(topicIndex, Topic);
                    }
                    else
                        parentTopic.Topics.Add(Topic);
                }
                else
                    AppModel.ActiveProject.Topics.Add(Topic);                        
                
            }
            else if (RadioButtonTop.IsChecked.Value)
            {
                Topic.ParentId = null;
                Topic.Parent = null;
                AppModel.ActiveProject.Topics.Add(Topic);
                  
            }

            Topic.Body = "# " + Topic.Title;
            Topic.SaveTopicFile();

            AppModel.ActiveTopic = Topic;


            // make sure it gets written to disk
            //AppModel.ActiveProject.SaveTopic(Topic);
            AppModel.ActiveProject.SaveProject();


            Dispatcher.Invoke(() =>
            {
                // HACK: Filtered Collection won't update on its own
                AppModel.TopicsTree.Model.OnPropertyChanged(nameof(TopicsTreeModel.FilteredTopicTree));
                //CollectionViewSource.GetDefaultView(Window.TopicsTree.Model.FilteredTopicTree).Refresh();

                Topic.TopicState.IsSelected = true;
            
                //AppModel.Window.PreviewMarkdownAsync();
                //Dispatcher.InvokeAsync(() =>
                //        AppModel.TopicsTree.OpenTopicInMMEditor(Topic),
                //        System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                AppModel.TopicsTree.OpenTopicInMMEditor(Topic);
            },System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            return Topic;
        }

        public void SetTopicLevel(DocTopic topic)
        {
            if (topic != null)
            {
                if (topic.DisplayType == "header" ||
                    topic.DisplayType == "classheader" ||
                    topic.DisplayType== "index")
                {
                    Topic.ParentId = topic.Id;
                    ParentTopicTitle = topic.Title;
                    Topic.DisplayType = "topic";
                    NewTopicLevel = "Below";
                    if (topic.DisplayType == "classheader")
                        Topic.DisplayType = "classmethod";

                    RadioButtonBelow.IsChecked = true;
                }
                else
                {
                    NewTopicLevel = "Current";
                    Topic.ParentId = topic.ParentId;
                    ParentTopicTitle = AppModel.ActiveProject.Topics
                        .FirstOrDefault(t => t.Id == Topic.ParentId)?.Title;
                    Topic.DisplayType = topic.DisplayType;
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

        private void ComboTopicType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // force refresh
            OnPropertyChanged(nameof(IsHeaderTopic));
        }

        private void TextTopicTitle_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateSlugAndLink();
        }

        private void UpdateSlugAndLink()
        {
            if (string.IsNullOrEmpty(Topic.Title))
            {
                Topic.Slug = null;
                Topic.Link = null;
            }
            else
            {
                string baseRoute = "";
                var baseTopic = AppModel.ActiveTopic;

                if (RadioButtonCurrent.IsChecked.Value)
                    baseRoute = baseTopic.Parent?.Slug;
                if (RadioButtonBelow.IsChecked.Value)
                    baseRoute = baseTopic.Slug;

                if (!string.IsNullOrEmpty(baseRoute))
                    baseRoute += "/";

                Topic.Slug = Topic.CreateSlug();
                //if (IsHeaderTopic)
                //    Topic.Link = Topic.Slug + "/" + Topic.Slug + ".md";
                //else
                    Topic.Link = Topic.Slug + ".md";

                Topic.Link = baseRoute + Topic.Link;
                Topic.Slug = baseRoute + Topic.Slug;
            }
        }
    }
}
