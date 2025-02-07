using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocMonster;
using DocMonster.Model;
using DocMonsterAddin;
using DocMonsterAddin.Controls;
using MarkdownMonster;
using MarkdownMonster.Utilities;
using MarkdownMonster.Windows;

namespace KavaDocsAddinControls
{
    /// <summary>
    /// Interaction logic for TopicPicker.xaml
    /// </summary>
    public partial class TopicPicker : UserControl
    {
        public TopicsTreeModel Model { get; set; }

        public Action<DocTopic> TopicSelected { get; set; }

        public DocTopic SelectedTopic { get; set; }

        public TopicPicker()
        {
            InitializeComponent();

            // Create a new instance of the project so we don't navigate
            // the main tree
            var project = DocProjectManager.Current.LoadProject(kavaUi.Model.ActiveProject?.Filename);            
            project.GetTopicTree();

            Model = new TopicsTreeModel(project);

            
            
            DataContext = Model;

        }


        #region Main Operations

        public void LoadProject(DocProject project)
        {
            if (Model.Project != null)
            {
                Model.DocMonsterModel.Configuration.AddRecentProjectItem(Model.Project.Filename,
                    Model.DocMonsterModel.ActiveTopic?.Id, Model.Project.Title);
            }

            Model.Project = project;

            if (Model.Project == null)
            {
                Model.TopicTree = new ObservableCollection<DocTopic>();
                return;
            }

            project.GetTopicTree();

            //StringBuilder sb = new StringBuilder();
            //project.WriteTopicTree(project.Topics, 0, sb);

            if (project.Topics != null && project.Topics.Count > 0)
                Model.DocMonsterModel.ActiveTopic = project.Topics[0];

            Model.TopicTree = project.Topics;

            Model.DocMonsterModel.Configuration.AddRecentProjectItem(project.Filename, projectTitle: project.Title);
        }

        public void SelectTopic(DocTopic topic)
        {
            Model.SelectTopic(topic);
        }


        #endregion

        #region Selection Handling

        private void TreeTopicBrowser_Selected(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // don't bubble up through parents

            var topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic != null)
            {
                SelectedTopic = topic;                

                TreeViewItem tvi = e.OriginalSource as TreeViewItem;
                tvi?.BringIntoView();                
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = TreeTopicBrowser.SelectedItem as DocTopic;

            if (selected != null)
            {
                SelectedTopic = selected;
                TopicSelected?.Invoke(selected);
            }
        }

        #endregion
    }
}
