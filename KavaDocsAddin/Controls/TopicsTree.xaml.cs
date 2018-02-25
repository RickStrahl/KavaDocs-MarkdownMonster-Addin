using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DocHound.Model;
using DocHound.Utilities;
using KavaDocsAddin.Core.Configuration;

namespace KavaDocsAddin.Controls
{
    /// <summary>
    /// Interaction logic for TopicsTree.xaml
    /// </summary>
    public partial class TopicsTree : UserControl
    {
        public TopicsTreeModel Model { get; set; }

        public TopicsTree()
        {
            InitializeComponent();
            Loaded += TopicsTree_Loaded;

            Model = new TopicsTreeModel(null);
            DataContext = Model;
        }

        private void TopicsTree_Loaded(object sender, RoutedEventArgs e)
        {



        }

        #region Main Operations

        public void LoadProject(DocProject project)
        {            
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
            {
                Model.AppModel.ActiveTopic = project.Topics[0];                ;
            }

            Model.TopicTree = project.Topics;
        }

        #endregion

        #region Selection Handling


        private void TreeTopicBrowser_Selected(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // don't bubble up through parents

            if (!HandleSelection())
                return;        
            
            var topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic != null)
            {
                TreeViewItem tvi = e.OriginalSource as TreeViewItem;
                 tvi?.BringIntoView();
            }            
        }


        public bool HandleSelection(DocTopic topic = null)
        {
            if (topic == null)
                topic = TreeTopicBrowser.SelectedItem as DocTopic;

            if (topic == null)
                return false;
            

            kavaUi.AddinModel.LastTopic = kavaUi.AddinModel.ActiveTopic;
            kavaUi.AddinModel.ActiveTopic = topic;
            if (kavaUi.AddinModel.RecentTopics.Contains(topic))
                kavaUi.AddinModel.RecentTopics.Remove(topic);
            kavaUi.AddinModel.RecentTopics.Insert(0, topic);

            if (kavaUi.AddinModel.RecentTopics.Count > 15)
                kavaUi.AddinModel.RecentTopics = new ObservableCollection<DocTopic>(kavaUi.AddinModel.RecentTopics.Take(14));

            OpenTopicInEditor();
            
            return true;
        }

        private void OpenTopicInEditor()
        {
            DocTopic topic = TreeTopicBrowser.SelectedItem as DocTopic;
             if (topic == null)
                return;

            var file = topic.GetTopicFileName();
            var editorFile = topic.GetKavaDocsEditorFilePath();
            try
            {
                File.Copy(file, editorFile, true);
            }
            catch 
            {
                try
                {
                    File.Copy(file, editorFile, true);
                }
                catch
                {
                    return;
                }
            }

            Model.AppModel.Window.OpenTab(editorFile);
        }

      
        

        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // Tabbing out of treeview sets focus to editor
            if (e.Key == Key.Tab )
            {
                var tvi = e.OriginalSource as TreeViewItem;
                if (tvi == null)
                    return;

                if (kavaUi.AddinModel.TopicEditor.TabTopic.IsSelected)
                {
                    Model.AppModel.ActiveMarkdownEditor.GotoLine(0);
                    Model.AppModel.ActiveMarkdownEditor.SetEditorFocus();
                }
            }
        }

        #endregion

        #region SearchKey



        public void SelectTopic(DocTopic topic)
        {
            topic.TopicState.IsSelected = true;
        }

        public TreeViewItem GetTreeviewItem(DocTopic item)
        {  
            return (TreeViewItem) TreeTopicBrowser
                .ItemContainerGenerator
                .ContainerFromItem(item);            
        }
        #endregion


        #region SearchText Handling
        //private void TextSearchText_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    if (Model.TopicsFilter == "Search...")
        //    {
        //        Model.TopicsFilter = "";
        //        TextSearchText.Text = "";
        //    }
        //}

        //private void TextSearchText_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(Model.TopicsFilter))
        //    {
        //        Model.TopicsFilter = "Search...";
        //        TextSearchText.Text = "Search...";
        //    }
        //}


    }
    #endregion

}
