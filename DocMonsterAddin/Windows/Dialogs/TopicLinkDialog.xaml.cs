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
using DocMonster;
using DocMonster.Model;
using DocMonsterAddin.Controls;
using LibGit2Sharp;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Utilities;
using MarkdownMonster.Windows;
using Westwind.Utilities;
using Westwind.WebView.Wpf;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for TopicLinkDialog.xaml
    /// </summary>
    public partial class TopicLinkDialog : MetroWindow
    {
        public TopicsTree TreeTopics { get; set; }

        public TopicLinkModel Model { get; set; }

        public WebViewHandler WebViewHandler { get; set; }


        public StatusBarHelper Status { get;  }

        public TopicLinkDialog(string linkText = null, bool isTopicPickerMode = false)
        {

            _ = ParseSearchText(linkText);

            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);
            Model = new TopicLinkModel()
            {
                Project = kavaUi.Model.ActiveProject,
                EmbeddingType = "Topic Id",
                SelectedTopic = kavaUi.Model.ActiveTopic,
                LinkText = linkText,
                IsTopicPickerMode = isTopicPickerMode
            };

            if (isTopicPickerMode)
                Title = "Topic Picker";

            Status = new StatusBarHelper(StatusText, StatusIcon);

            // Force use of the shared WebView Environment so we're using
            // the shared runtime folder etc.
            WebViewHandler = new WebViewHandler(WebView);

            // load asynchronously
            Task.Run(() =>
            {
                // Load a separate copy so we don't navigate the original instance
                var project = DocProjectManager.Current.LoadProject(kavaUi.Model.ActiveProject.Filename);
                Dispatcher.Invoke(() =>
                {
                    Model.Project = project;

                    var model = new TopicsTreeModel(project);
                    model.SelectionHandler = (topic, isComplete) =>
                    {
                        SelectTopicAndDisplay(topic);
                        if (isComplete)
                        {
                            if (isTopicPickerMode)
                                Button_Click(ButtonCopyTopicToClipboard, null);
                            else
                                Button_Click(ButtonOk, null);
                        }

                        return true; // don't navigate further
                    };

                    // render with empty project Then fill with project after async load ^^^
                    TreeTopics = new TopicsTree(model: model);    // set in mainline            
                    TreeTopics.HeaderBar.Visibility = Visibility.Collapsed;
                    TreeContainer.Children.Add(TreeTopics);

                    TreeTopics.SetSearchText(Model.SearchText, true);

                    var topic = TreeTopics.Model.FindTopicInTreeByText(null, Model.SearchText);
                    topic = topic ?? TreeTopics.Model.FilteredTopicTree.FirstOrDefault();
                    if (topic != null)
                    {
                        SelectTopicAndDisplay(topic);
                        TreeTopics.SelectTopic(topic);
                    }

                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            });

            WindowUtilities.CenterWindow(this, mmApp.Model.Window);

            DataContext = Model;
        }




        /// <summary>
        /// Parses search Text if not provided from Clipboard
        /// </summary>
        /// <param name="searchText"></param>
        private async Task ParseSearchText(string searchText)
        {
            if (mmApp.Model.ActiveEditor == null) return;

            if (!string.IsNullOrEmpty(searchText)) {
                Model.SearchText = searchText;
                return;
            }

            var selection = await mmApp.Model.ActiveEditor.GetSelection();
            if (string.IsNullOrEmpty(selection)) return;

            Model.SearchText = selection;
            Model.LinkText = selection;
        }

        public void SelectTopicAndDisplay(DocTopic topic = null)
        {
            if (topic == null)
                topic = kavaUi.Model.ActiveTopic;

            if (string.IsNullOrEmpty(topic?.RenderTopicFilename))
                return;

            var oldTitle = Model.SelectedTopic.Title;

            // if empty or 'default' replace with new text - if custom title - leave it alone
            if (string.IsNullOrEmpty(Model.LinkText) || Model.LinkText == oldTitle)
            {
                Model.LinkText = topic.Title;
            }

            Model.SelectedTopic = topic;


            Model.TopicLink = GetTopicLink();

            var file = topic.RenderTopicFilename.Replace(".html", "_topic-link.html");
            topic.RenderTopicToFile(file, TopicRenderModes.Preview);
            WebViewHandler.Navigate(new Uri(file), true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;

            // Just to make sure!
            Model.SearchText = TreeTopics.TextSearchText.Text;

            if (sender == ButtonOk)
            {

                var topic = Model.SelectedTopic;
                string html = null;
                if (Model.EmbeddingType.StartsWith("Topic", StringComparison.OrdinalIgnoreCase))
                    html = $"[{Model.LinkText}](dm-topic://{topic.Id})";
                else if (Model.EmbeddingType.Equals("Link", StringComparison.OrdinalIgnoreCase))
                    html = $"[{Model.LinkText}](dm-slug://{topic.Slug})";
                else if (Model.EmbeddingType.Equals("Relative Link"))
                {
                    var currentTopic = kavaUi.Model.ActiveTopic;
                    var relPath = GetRelativePath("/" + currentTopic.Slug.TrimStart('/'), "/" + topic.Slug.TrimStart('/')) + ".html";
                    html = $"[{Model.LinkText}]({relPath})";
                }

                else if (Model.EmbeddingType.Equals("Title", StringComparison.OrdinalIgnoreCase))
                    html = $"[{Model.LinkText}](dm-title://{topic.Title.Replace(" ", "%20")})";


                Close();

                mmApp.Model.ActiveEditor.SetSelectionAndFocus(html).FireAndForget();
            }
            else if (sender == ButtonCancel)
            {
                Close();

                mmApp.Model.Window.Activate();
            }
            else if (sender == ButtonCopyTopicToClipboard)
            {
                if (Model.SelectedTopic == null) return;

                var text = GetTopicLink();
                if (!string.IsNullOrEmpty(text))
                {
                    ClipboardHelper.SetText(text);
                    Status.ShowStatusSuccess("Link copied to clipboard",4000);
                }
            }
        }

        public string GetTopicLink()
        {
            string text = null;
            var topic = Model.SelectedTopic;

            if (Model.EmbeddingType.StartsWith("Topic", StringComparison.OrdinalIgnoreCase))
                text = topic.Id;
            else if (Model.EmbeddingType.Equals("Slug", StringComparison.OrdinalIgnoreCase) ||
                     Model.EmbeddingType.Contains("Link", StringComparison.OrdinalIgnoreCase))                     
                text = topic.Slug;
            else if (Model.EmbeddingType.Equals("Title", StringComparison.OrdinalIgnoreCase))
                text = topic.Title;
            else
                text = topic.Id;

            return text;
        }

        string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri("file://" + fromPath);
            Uri toUri = new Uri("file://" + toPath);
            return fromUri.MakeRelativeUri(toUri).ToString();
        }

        private void StatusTextTopicLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(Model.TopicLink))
            {
                Clipboard.SetText(Model.TopicLink);
                mmApp.Window.ShowStatusSuccess("Link copied to clipboard");
            }                
        }
    }

    public class TopicLinkModel : INotifyPropertyChanged
    {
        /// <summary>
        /// If true the dialog is in Topic Picker Mode which
        /// simple browses topics and allows for copying of the
        /// Id to the clipboard.
        ///
        /// You can also read the Model.TopicLink property to get
        /// the currently selected id.
        /// </summary>
        public bool IsTopicPickerMode
        {
            get => _isTopicPickerMode;
            set
            {
                if (value == _isTopicPickerMode) return;
                _isTopicPickerMode = value;
                OnPropertyChanged();
            }
        }
        private bool _isTopicPickerMode;

        public DocTopic SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                if (Equals(value, _selectedTopic)) return;
                _selectedTopic = value;
                OnPropertyChanged();
            }
        }
        private DocTopic _selectedTopic;

        
        public DocProject Project
        {
            get => _project;
            set
            {
                if (Equals(value, _project)) return;
                _project = value;
                OnPropertyChanged();
            }
        }
        private DocProject _project;


        public string TopicLink
        {
            get => _topicLink;
            set
            {
                if (value == _topicLink) return;
                _topicLink = value;
                OnPropertyChanged();
            }
        }
        private string _topicLink;


        /// <summary>
        /// Topic Id, Link, Title
        /// </summary>
        public string EmbeddingType
        {
            get => _embeddingType;
            set
            {
                if (value == _embeddingType) return;
                _embeddingType = value;
                OnPropertyChanged();
            }
        }
        private string _embeddingType;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value == _searchText) return;
                _searchText = value;
                OnPropertyChanged();
            }
        }
        private string _searchText;

        public string LinkText
        {
            get => _linkText;
            set => SetField(ref _linkText, value);
        }
        private string _linkText;

        public string[] EmbeddingTypes { get; set; } = ["Topic Id", "Slug", "Relative Link", "Title"];

        #region INotifiyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
