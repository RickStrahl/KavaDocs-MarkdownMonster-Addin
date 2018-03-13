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
using Westwind.Utilities;

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

            if (Model.Project != null)
            {                
                Model.KavaDocsModel.Configuration.AddRecentProjectItem(Model.Project.Filename,
                    Model.KavaDocsModel.ActiveTopic?.Id,Model.Project.Title);
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
                Model.KavaDocsModel.ActiveTopic = project.Topics[0];

            Model.TopicTree = project.Topics;

            Model.KavaDocsModel.Configuration.AddRecentProjectItem(project.Filename,projectTitle: project.Title);
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

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeTopicBrowser_Selected(sender, e);
            var topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic != null)
            {
                var tab = Model.KavaDocsModel.Window.RefreshTabFromFile(topic.GetTopicFileName());
                var editor = tab.Tag as MarkdownDocumentEditor;
                if (editor != null)
                    editor.Identifier = "KavaDocsDocument";
            }
            
        }


        public bool HandleSelection(DocTopic topic = null)
        {
            bool selectTopic = false;
            if (topic == null)
                topic = TreeTopicBrowser.SelectedItem as DocTopic;
            else
                selectTopic = true;

            if (topic == null)
                return false;

            kavaUi.AddinModel.LastTopic = kavaUi.AddinModel.ActiveTopic;

            bool result = SaveProjectFileForTopic(kavaUi.AddinModel.LastTopic);
            if (!result)
                return false;

            
            kavaUi.AddinModel.ActiveTopic = topic;

            
            // TODO: Move to function
            if (kavaUi.AddinModel.RecentTopics.Contains(topic))
                kavaUi.AddinModel.RecentTopics.Remove(topic);
            kavaUi.AddinModel.RecentTopics.Insert(0, topic);

            if (kavaUi.AddinModel.RecentTopics.Count > 15)
                kavaUi.AddinModel.RecentTopics =
                    new ObservableCollection<DocTopic>(kavaUi.AddinModel.RecentTopics.Take(14));

            OpenTopicInEditor();
            
            var file = topic.GetTopicFileName();
            var doc = new MarkdownDocument();
            doc.Load(file);

            // set topic state to selected and unchanged
            if (selectTopic)
                topic.TopicState.IsSelected = true;
            topic.TopicState.IsDirty = false;

            return true;
        }

        public bool SaveProjectFileForTopic(DocTopic topic, DocProject project = null)
        {
            if (topic == null)
                return false;

            if (!topic.TopicState.IsDirty)
                return true;

            if (project == null)
                project = kavaUi.AddinModel.ActiveProject;

            bool result =  project.SaveProject();
            if (result)
                topic.TopicState.IsDirty = false;

            return result;
        }

        public TabItem OpenTopicInEditor()
        {
            DocTopic topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic == null)
                return null;

            var window = Model.KavaDocsModel.Window;
            var file = topic.GetTopicFileName();
            var editorFile = topic.GetKavaDocsEditorFilePath();

            // is tab open already as a file? If so use that
            var tab = window.GetTabFromFilename(file);
            if (tab != null)
            {
                window.TabControl.SelectedItem = tab;
                window.CloseTab(editorFile);             
            }
            else
            {
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
                        return null;
                    }
                }

                // Will also open the tab if not open yet
                tab = Model.KavaDocsModel.Window.RefreshTabFromFile(editorFile, readOnly: true);
            }

            if (tab != null)
            {
                SetEditorWithTopic(tab.Tag as MarkdownDocumentEditor, kavaUi.AddinModel.ActiveTopic);

                // Explicitly read in the current text from an open tab and save to body
                var editor = tab.Tag as MarkdownDocumentEditor;
                var body = editor.GetMarkdown();
                if (!string.IsNullOrEmpty(body))                
                    topic.Body = topic.StripYaml(body);

                if (string.IsNullOrEmpty(topic.Link))
                {
                    var relative = FileUtils.GetRelativePath(topic.GetTopicFileName(), topic.Project.ProjectDirectory);
                    if (!string.IsNullOrEmpty(relative))
                    {
                        topic.Link = FileUtils.NormalizePath(relative);
                        if (String.IsNullOrEmpty(topic.Link))
                            topic.Link = topic.Link.Replace("\\", "/");
                    }
                }

                if (body != topic.Body)
                    editor.SetMarkdown(topic.Body);
            }

            return tab;
        }


        /// <summary>
        /// Attaches a KavaDocs Topic to the current document and sets the Identifier to
        /// `KavaDocsDocument' so that you can easily retrieve the topic associated with 
        /// an open document.
        /// 
        /// Used to allow for syncing back topics to the underlying topic.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="topic"></param>
        public static void SetEditorWithTopic(MarkdownDocumentEditor editor, DocTopic topic)
        {
            if (editor == null)
                return;
            if (topic == null)
            {
                editor.Identifier = null;
                editor.Properties.Remove("KavaDocsTopic");
            }
            else
            {
                editor.Identifier = "KavaDocsDocument";
                editor.Properties["KavaDocsTopic"] = topic;
            }
        }


        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // Tabbing out of treeview sets focus to editor
            if (e.Key == Key.Tab)
            {
                var tvi = e.OriginalSource as TreeViewItem;
                if (tvi == null)
                    return;                
            }

            // this works without a selection
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Model.KavaDocsModel.Commands.NewTopicCommand.Execute(null);
            }
            else if (e.Key == Key.Delete)
            {
                Model.KavaDocsModel.Commands.DeleteTopicCommand.Execute(null);
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

        
        internal DragMoveResult _dragMoveResult;
        ContextMenu _dragContextMenu;
        private Point _lastMouseDownPoint;
        //private DateTime _lastMouseDown;
        //bool _isDragging = false;

        public CommandBase MoveTopicCommand { get; set; }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDownPoint = e.GetPosition(TreeTopicBrowser);
                //_lastMouseDown = DateTime.UtcNow;
                //_isDragging = false;                
            }
        }

        private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            var time = DateTime.UtcNow;
            
            if (e.LeftButton == MouseButtonState.Pressed)
            {               
                var topic = TreeTopicBrowser.SelectedItem as DocTopic;
                if (topic == null)
                    return;
                
                
                //// At least 400 ms before dragging and less than 1.5secs to start dragging
                //if (time < _lastMouseDown.AddMilliseconds(200) || time > _lastMouseDown.AddMilliseconds(1500))
                //{
                //    Debug.WriteLine($"Not dragging: {time:HH:mm:ss:ms} {_lastMouseDown:HH:mm:ss:ms}");
                //    _isDragging = false;
                //    _lastMouseDown = DateTime.MinValue;                    
                //    return;
                    
                //}


                Point currentPosition = e.GetPosition(TreeTopicBrowser);

                if ((Math.Abs(currentPosition.X - _lastMouseDownPoint.X) > 25) ||
                    (Math.Abs(currentPosition.Y - _lastMouseDownPoint.Y) > 20))
                {
                    DragDrop.DoDragDrop(TreeTopicBrowser, TreeTopicBrowser.SelectedItem, DragDropEffects.Move | DragDropEffects.None);
                }
            }
        }



        private void TreeViewItem_Drop(object sender, DragEventArgs e)
        {            
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

            if (sourceTopic == targetTopic)
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
            _dragContextMenu.Items.Add(new Separator());
            _dragContextMenu.Items.Add(new MenuItem
            {
                Header = "Cancel topic move",
                Tag = _dragContextMenu,
                Command = MoveTopicCommand,
                CommandParameter = null
            });

            WindowUtilities.DoEvents();
            _dragContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            _dragContextMenu.PlacementTarget = tvItem;            
            _dragContextMenu.Visibility = Visibility.Visible;
            _dragContextMenu.IsOpen = true;
            WindowUtilities.DoEvents();
        }

        void CreateMoveTopicCommand()
        {
            MoveTopicCommand = new CommandBase((parameter, command) =>
            {

                _dragContextMenu.Visibility = Visibility.Collapsed;
                WindowUtilities.DoEvents();
                
                var dragResult = parameter as DragMoveResult;
                if (dragResult == null)
                    return;
               
                if (dragResult.TargetTopic == dragResult.SourceTopic)
                    return;

                var targetTopics = dragResult.TargetTopic?.Topics;
                if (targetTopics == null)
                    targetTopics = Model.KavaDocsModel.ActiveProject.Topics;

                var targetParentTopics = dragResult.TargetTopic.Parent?.Topics;
                if (targetParentTopics == null)
                    targetParentTopics = Model.KavaDocsModel.ActiveProject.Topics;

                var sourceTopics = dragResult.SourceTopic?.Parent?.Topics;
                if (sourceTopics == null)
                    sourceTopics = Model.KavaDocsModel.ActiveProject.Topics;   

                if (dragResult.DropLocation == DropLocations.Below)
                {
                    sourceTopics.Remove(dragResult.SourceTopic);

                    // run out of band
                    targetTopics.Add(dragResult.SourceTopic);

                    dragResult.TargetTopic.IsExpanded = true;

                    dragResult.SourceTopic.Parent = dragResult.TargetTopic;
                    dragResult.SourceTopic.ParentId = dragResult.TargetTopic.Id;
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
                    
                    dragResult.SourceTopic.Parent = dragResult.TargetTopic.Parent;
                    dragResult.SourceTopic.ParentId = dragResult.TargetTopic.ParentId;
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

                    dragResult.SourceTopic.Parent = dragResult.TargetTopic.Parent;
                    dragResult.SourceTopic.ParentId = dragResult.TargetTopic.ParentId;
                }
    

                Model.KavaDocsModel.ActiveProject.SaveProject();
            }, (p, c) => true);
        }

        private void TreeViewItem_DragOver(object sender, DragEventArgs e)
        {

            var targetTv = sender as TreeViewItem;
            if (sender == null)
                e.Effects = DragDropEffects.None;
            if (!e.Data.GetDataPresent(typeof(DocTopic)))
                e.Effects = DragDropEffects.None;
            else
            {
                var targetTopic = targetTv.DataContext as DocTopic;
                var sourceTopic = (DocTopic)e.Data.GetData(typeof(DocTopic));
                    
                if (targetTopic == sourceTopic)                    
                    e.Effects = DragDropEffects.None;
                else
                    e.Effects = DragDropEffects.Link;
            }
        }


        #endregion

        private void MenuRecentItems_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            if (menu == null)
                return;

            menu.Items.Clear();
            foreach (var recent in Model.KavaDocsModel.Configuration.RecentProjects)
            {
                var header = recent.ProjectTitle;
                if (!String.IsNullOrEmpty(header))
                    header += $" ({FileUtils.GetCompactPath(recent.ProjectFile)})";
                else
                {
                    header = $"{System.IO.Path.GetFileNameWithoutExtension(recent.ProjectFile)} ({FileUtils.GetCompactPath(recent.ProjectFile)})";
                }

                var mi = new MenuItem()
                {
                    Header =  header,
                    Command = Model.KavaDocsModel.Commands.OpenRecentProjectCommand,
                    CommandParameter = recent
                };
                menu.Items.Add(mi);
            }
        }
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
        After,
        None
    }



}