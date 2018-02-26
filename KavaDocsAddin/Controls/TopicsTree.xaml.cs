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
using MarkdownMonster;
using MarkdownMonster.Windows;

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
                Model.AppModel.ActiveTopic = project.Topics[0];
                ;
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
                kavaUi.AddinModel.RecentTopics =
                    new ObservableCollection<DocTopic>(kavaUi.AddinModel.RecentTopics.Take(14));

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


            var tab = Model.AppModel.Window.GetTabFromFilename(editorFile);
            if (tab != null)
            {
                var doc = tab.Tag as MarkdownDocumentEditor;
                doc.MarkdownDocument.Load(editorFile);
                //doc.LoadDocument(doc.MarkdownDocument);
                doc.SetScrollPosition(0);
                doc.SetMarkdown(doc.MarkdownDocument.CurrentText);

            }
            else
                Model.AppModel.Window.OpenTab(editorFile);
        }




        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // Tabbing out of treeview sets focus to editor
            if (e.Key == Key.Tab)
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

            // this works without a selection
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Model.AppModel.Commands.NewTopicCommand.Execute(null);
            }
            else if (e.Key == Key.Delete)
            {
                Model.AppModel.Commands.DeleteTopicCommand.Execute(null);
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



        #region Drag and Drop

        bool _isDragging = false;
        internal DragMoveResult _dragMoveResult;
        ContextMenu _dragContextMenu;
        private Point _lastMouseDown;

        public CommandBase MoveTopicCommand { get; set; }

        private void TreeViewItem_MouseDown
            (object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(TreeTopicBrowser);
            }
        }

        private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {

            if (!_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(TreeTopicBrowser);

                if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 30.0) ||
                    (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 20.0))
                {

                    _isDragging = true;
                    DragDrop.DoDragDrop(TreeTopicBrowser, TreeTopicBrowser.SelectedItem, DragDropEffects.Move);
                }
            }
        }



        private void TreeViewItem_Drop(object sender, DragEventArgs e)
        {
            _isDragging = false;

            if (!e.Data.GetDataPresent(typeof(DocTopic)))
                return;

            DocTopic sourceTopic = (DocTopic) e.Data.GetData(typeof(DocTopic));
            if (sourceTopic == null)
                return;

            var tvItem = WindowUtilities.FindAnchestor<TreeViewItem>((DependencyObject) e.OriginalSource);
            if (tvItem == null)
                return;
            var targetTopic = tvItem.DataContext as DocTopic;
            if (targetTopic == null)
                return;

            if (MoveTopicCommand == null)
                CreateMoveTopicCommand();

            _dragContextMenu = new ContextMenu()
            {
                DataContext = _dragMoveResult
            };

            var mi = new MenuItem
            {
                Header = "Move underneath this item",
                Tag = _dragContextMenu,
                Command = MoveTopicCommand,
                CommandParameter = new DragMoveResult
                {
                    SourceTopic = sourceTopic,
                    TargetTopic = targetTopic,
                    DropLocation = DropLocations.Below
                }
            };
            _dragContextMenu.Items.Add(mi);
            _dragContextMenu.Items.Add(new Separator());
            _dragContextMenu.Items.Add(new MenuItem
            {
                Header = "Move before this item",
                Tag = _dragContextMenu,
                Command = MoveTopicCommand,
                CommandParameter = new DragMoveResult
                {
                    SourceTopic = sourceTopic,
                    TargetTopic = targetTopic,
                    DropLocation = DropLocations.Before
                }
            });
            _dragContextMenu.Items.Add(new MenuItem
            {
                Header = "Move after this item",
                Tag = _dragContextMenu,
                Command = MoveTopicCommand,                
                CommandParameter = new DragMoveResult
                {
                    SourceTopic = sourceTopic,
                    TargetTopic = targetTopic,
                    DropLocation = DropLocations.After
                }
            });

            _dragContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            _dragContextMenu.PlacementTarget = tvItem;
            _dragContextMenu.Visibility = Visibility.Visible;
            _dragContextMenu.IsOpen = true;
            
        }

        void CreateMoveTopicCommand()
        {
            MoveTopicCommand = new CommandBase((parameter, command) =>
            {

                _dragContextMenu.Visibility = Visibility.Collapsed;
                WindowUtilities.DoEvents();

                var dragResult = parameter as DragMoveResult;

                var targetTopics = dragResult.TargetTopic?.Topics;
                if (targetTopics == null)
                    targetTopics = Model.AppModel.ActiveProject.Topics;

                var targetParentTopics = dragResult.TargetTopic.Parent?.Topics;
                if (targetParentTopics == null)
                    targetParentTopics = Model.AppModel.ActiveProject.Topics;

                var sourceTopics = dragResult.SourceTopic?.Parent?.Topics;
                if (sourceTopics == null)
                    sourceTopics = Model.AppModel.ActiveProject.Topics;   

                if (dragResult.DropLocation == DropLocations.Below)
                {
                    sourceTopics.Remove(dragResult.SourceTopic);

                    // run out of band
                    targetTopics.Add(dragResult.SourceTopic);

                    dragResult.TargetTopic.IsExpanded = true;
                }
                else if (dragResult.DropLocation == DropLocations.Before)
                {
                    sourceTopics.Remove(dragResult.SourceTopic);

                    var idx = targetParentTopics.IndexOf(dragResult.TargetTopic);
                    if (idx < 0)
                    {
                        sourceTopics.Add(dragResult.SourceTopic);
                        return;
                    }

                    // required to ensure items get removed before adding
                    targetParentTopics.Insert(idx, dragResult.SourceTopic);
                }
                else if (dragResult.DropLocation == DropLocations.After)
                {
                    sourceTopics.Remove(dragResult.SourceTopic);

                    var idx = targetParentTopics.IndexOf(dragResult.TargetTopic);
                    if (idx < 0)
                    {
                        sourceTopics.Add(dragResult.SourceTopic);
                        return;
                    }
                    
                    idx++;
                    targetParentTopics.Insert(idx, dragResult.SourceTopic);
                }

                dragResult.SourceTopic.Parent = dragResult.TargetTopic.Parent;
                dragResult.SourceTopic.ParentId = dragResult.TargetTopic.ParentId;     

                Model.AppModel.ActiveProject.SaveProject();
            }, (p, c) => true);
        }

        private void TreeViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DocTopic)))
                return;
            e.Effects = DragDropEffects.Move;
        }

        #endregion
    }


    public class DragMoveResult
    {
        public DocTopic SourceTopic;
        public DocTopic TargetTopic;
        public DropLocations DropLocation = DropLocations.After;
    }

    public enum DropLocations
    {
        Below,
        Before,
        After
    }



}