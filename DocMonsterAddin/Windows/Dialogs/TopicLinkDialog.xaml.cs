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
using DocMonster.Model;
using DocMonsterAddin.Controls;
using LibGit2Sharp;
using MahApps.Metro.Controls;
using MarkdownMonster;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for TopicLinkDialog.xaml
    /// </summary>
    public partial class TopicLinkDialog : MetroWindow
    {
        public TopicsTree TreeTopics { get; set; }

        public TopicLinkModel Model { get; set; }
        public TopicLinkDialog()
        {
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Model = new TopicLinkModel()
            {
                Project = kavaUi.Model.ActiveProject,
                EmbeddingType = "topic",
                SelectedTopic = kavaUi.Model.ActiveTopic
            };
            var model = new TopicsTreeModel(kavaUi.Model.ActiveProject);            
            model.SelectionHandler = (topic) =>
            {
                Model.SelectedTopic = topic;
                var file = topic.RenderTopicFilename.Replace(".html", "_topic-link.html");
                topic.RenderTopicToFile(file, TopicRenderModes.Preview);
                WebView.Source = new Uri(file);
                return true; // don't navigate further
            };


            TreeTopics = new TopicsTree(model: model);    // set in mainline
            TreeTopics.Visibility = Visibility.Visible;
            TreeContainer.Children.Add(TreeTopics);

            DataContext = Model;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class TopicLinkModel : INotifyPropertyChanged
    {        

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


        private string _embeddingType;

        public string EmbeddingType
        {
            get => _embeddingType;
            set => SetField(ref _embeddingType, value);
        }

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
