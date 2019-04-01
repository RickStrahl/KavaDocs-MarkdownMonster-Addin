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
using DocHound.Windows.Dialogs;
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

        public bool HandleSelection(DocTopic topic = null, bool forceFocus = false)
        {
            bool selectTopic = false;
            if (topic == null)
                topic = TreeTopicBrowser.SelectedItem as DocTopic;
            else
                selectTopic = true;

            if (topic == null)
                return false;

            var lastTopic = kavaUi.AddinModel.ActiveTopic;
            if (lastTopic != null)
                lastTopic.TopicState.IsSelected = false;

            kavaUi.AddinModel.LastTopic = lastTopic;
            
            bool result = SaveProjectFileForTopic(kavaUi.AddinModel.LastTopic);
            
            if (result)
                kavaUi.AddinModel.Window.ShowStatus("Topic saved.",3000);
                
            kavaUi.AddinModel.ActiveTopic = topic;
            
            // TODO: Move to function
            if (kavaUi.AddinModel.RecentTopics.Contains(topic))
                kavaUi.AddinModel.RecentTopics.Remove(topic);
            kavaUi.AddinModel.RecentTopics.Insert(0, topic);

            if (kavaUi.AddinModel.RecentTopics.Count > 15)
                kavaUi.AddinModel.RecentTopics =
                    new ObservableCollection<DocTopic>(kavaUi.AddinModel.RecentTopics.Take(14));

            //OpenTopicInEditor();
            Dispatcher.InvokeAsync(() => OpenTopicInEditor(),DispatcherPriority.ApplicationIdle);
            
            var file = topic.GetTopicFileName();
            var doc = new MarkdownDocument();
            doc.Load(file);

            // set topic state to selected and unchanged
            if (selectTopic)
                topic.TopicState.IsSelected = true;
            topic.TopicState.IsDirty = false;

            return true;
        }


        //private void TreeTopicBrowser_Selected(object sender, RoutedEventArgs e)
        //{
        //    e.Handled = true; // don't bubble up through parents

        //    if (!HandleSelection())
        //        return;

        //    if (TreeTopicBrowser.SelectedItem is DocTopic)
        //    {
        //        TreeViewItem tvi = e.OriginalSource as TreeViewItem;
        //        tvi?.BringIntoView();
        //    }
        //}

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenTopicInMMEditor();                    
        }


        /// <summary>
        /// Saves a topic in the tree and then saves the project to
        /// disk.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="project"></param>
        /// <param name="async"></param>
        /// <returns>false if data wasn't written (could be because there's nothing that's changed)</returns>
        public bool SaveProjectFileForTopic(DocTopic topic, DocProject project = null, bool async = false)
        {
            if (topic == null)
                return false;

            if (!topic.TopicState.IsDirty)
                return false;

            if (project == null)
                project = kavaUi.AddinModel.ActiveProject;


            if (async)
            {
                project.SaveProjectAsync();
                topic.TopicState.IsDirty = false;
                return true;
            }

            bool result =  project.SaveProject();
            if (result)
                topic.TopicState.IsDirty = false;

            return result;        
        }


        /// <summary>
        /// Opens a topic for editing in a standard Markdown Monster
        /// tab (not the preview Tab)
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public TabItem OpenTopicInMMEditor(DocTopic topic = null)
        {
            if (topic == null)
                topic = TreeTopicBrowser.SelectedItem as DocTopic;

            TabItem tab = null;

            if (topic != null)
            {
                var file = topic.GetTopicFileName();
                if (file == null)
                    return null;
                
                if (!File.Exists(file))
                    File.WriteAllText(file, "");

                tab = Model.KavaDocsModel.Window.RefreshTabFromFile(file, isPreview: false, noFocus: false);
                Model.KavaDocsModel.Window.BindTabHeaders();

                if (tab.Tag is MarkdownDocumentEditor editor)
                {
                    editor.Properties[Constants.EditorPropertyNames.KavaDocsTopic] = topic;
                    editor.Properties[Constants.EditorPropertyNames.KavaDocsUnedited] = false;
                }
            }

            return tab;
        }


        /// <summary>
        /// Opens read
        /// </summary>
        /// <returns></returns>
        public TabItem OpenTopicInEditor(bool setFocus = false)
        {
            Debug.WriteLine("OpenTopicInEditor");

            DocTopic topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic == null)
                return null;

            var window = Model.KavaDocsModel.Window;

            TabItem tab;
            if (topic != null && topic.Body != null && (topic.IsLink || topic.Body.StartsWith("http")))
            {
                tab = Model.MarkdownMonsterModel.Window.OpenBrowserTab(topic.Link ?? topic.Body);
                return tab;
            }

            var file = topic.GetTopicFileName(force: true);

            MarkdownDocumentEditor editor = null;

            // is tab open already as a file? If so use that
            tab = window.GetTabFromFilename(file);
            if (tab != null)
            {
                editor = tab?.Tag as MarkdownDocumentEditor;
                if (editor == null)
                    return null;
            }
            else
            {
                // Will also open the tab if not open yet
                // EXPLICITLY NOT SELECTING THE TAB SO THAT IT'S NOT RENDERED YET
                // Assign topic first then explicitly select
                //tab = Model.KavaDocsModel.Window.RefreshTabFromFile(file, noFocus: !setFocus, isPreview: true, noSelectTab:true);

                tab = Model.KavaDocsModel.Window.ActivateTab(file,
                    noFocus: !setFocus,
                    isPreview: true,
                    noSelectTab: true);

                //RefreshTabFromFile(file, noFocus: !setFocus, isPreview: true, noSelectTab: true);

                editor = tab?.Tag as MarkdownDocumentEditor;
                if (editor == null)
                    return null;
            }

            if (tab == null)
                return null;

            // make sure topic is associated with editor
            SetEditorWithTopic(editor, topic, isUnEdited: true); // kavaUi.AddinModel.ActiveTopic);
           
            // Explicitly read in the current text from an open tab and save to body                
            var body = editor.GetMarkdown();
            if (!string.IsNullOrEmpty(body))
                topic.Body = topic.StripYaml(body);

            if (string.IsNullOrEmpty(topic.Link))
            {
                var relative = FileUtils.GetRelativePath(topic.GetTopicFileName(), topic.Project.ProjectDirectory);
                if (!string.IsNullOrEmpty(relative))
                {
                    topic.Link = FileUtils.NormalizePath(relative);
                    if (string.IsNullOrEmpty(topic.Link))
                        topic.Link = topic.Link.Replace("\\", "/");
                }
            }

            if (body != topic.Body)
                editor.SetMarkdown(topic.Body);

            window.TabControl.SelectedItem = tab;


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
        public static void SetEditorWithTopic(MarkdownDocumentEditor editor, DocTopic topic, bool isUnEdited = true)
        {
            if (editor == null)
                return;

            if (topic == null)
            {
                editor.Identifier = null;                
                editor.Properties.Remove(Constants.EditorPropertyNames.KavaDocsTopic);
                editor.Properties.Remove(Constants.EditorPropertyNames.KavaDocsUnedited);
            }
            else
            {
                editor.Identifier = "KavaDocsDocument";
                editor.Properties[Constants.EditorPropertyNames.KavaDocsTopic] = topic;
                editor.Properties[Constants.EditorPropertyNames.KavaDocsUnedited] = isUnEdited;
            }
        }

        public static DocTopic GetEditorTopic(MarkdownDocumentEditor editor)
        {
            if (!editor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object topic))
                return null;

            return topic as DocTopic;
        }

        public static DocTopic GetEditorTopic(TabItem tab)
        {
            var editor = tab.Tag as MarkdownDocumentEditor;
            if (editor == null)
                return null;

            return GetEditorTopic(editor);
        }

        private DebounceDispatcher keyUpDispatcher = new DebounceDispatcher();

        // This is too slow so require SPACE to refresh on key navigation
        private void TreeViewItem_KeyUp(object sender, KeyEventArgs e)
        {
            keyUpDispatcher.Debounce(500, (p) =>
            {
                var ev = p as KeyEventArgs;

                if (ev.Key == Key.Up || ev.Key == Key.Down)
                {
                    var tvi = e.OriginalSource as TreeViewItem;
                    if (tvi == null)
                        return;

                    HandleSelection();
                }
            }, e, DispatcherPriority.ApplicationIdle, Dispatcher);
        }

        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // Tabbing out of treeview sets focus to editor
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                var tvi = e.OriginalSource as TreeViewItem;
                if (tvi == null)
                    return;

                var topic = tvi.Tag as DocTopic;
                OpenTopicInMMEditor();
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


        internal DragMoveResult DragMoveResult = null;
        ContextMenu _dragContextMenu;
        private Point _lastMouseDownPoint;

        public CommandBase MoveTopicCommand { get; set; }

        private void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var topic = (e.OriginalSource as FrameworkElement)?.DataContext as DocTopic;
            if (topic == null || (sender as TreeViewItem)?.DataContext != topic)
                return;

            if (e.ChangedButton == MouseButton.Left)
                _lastMouseDownPoint = e.GetPosition(TreeTopicBrowser);

            if (!HandleSelection())
                return;

            if (TreeTopicBrowser.SelectedItem is DocTopic)
            {
                TreeViewItem tvi = e.OriginalSource as TreeViewItem;
                tvi?.BringIntoView();
            }
        }

        private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {
      if (e.LeftButton == MouseButtonState.Pressed)
            {               
                var topic = TreeTopicBrowser.SelectedItem as DocTopic;
                if (topic == null)
                    return;
      
                Point currentPosition = e.GetPosition(TreeTopicBrowser);

                if ((Math.Abs(currentPosition.X - _lastMouseDownPoint.X) > 50) ||
                    (Math.Abs(currentPosition.Y - _lastMouseDownPoint.Y) > 25))
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
                DataContext = DragMoveResult
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

                    var so = targetTopics.Count * 10;
                    foreach (var topic in targetTopics)
                    {
                        so -= 10;
                        topic.SortOrder = so;
                    }

                    UpdateMovedTopic(dragResult.SourceTopic);
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

                    var so = targetParentTopics.Count * 10;
                    foreach (var topic in targetParentTopics)
                    {
                        so -= 10;
                        topic.SortOrder = so;
                    }
                    UpdateMovedTopic(dragResult.SourceTopic);
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

                    var so = targetParentTopics.Count * 10;
                    foreach (var docTopic in targetParentTopics)
                    {
                        so -= 10;
                        docTopic.SortOrder = so;
                    }                    
                    UpdateMovedTopic(dragResult.SourceTopic);
                }

                Model.KavaDocsModel.ActiveProject.SaveProject();
            }, (p, c) => true);
        }

        void UpdateMovedTopic(DocTopic topic)
        {
            // TODO: Get latest changes from Editor
            var editorTopic = Model.MarkdownMonsterModel.ActiveEditor?.Properties[Constants.EditorPropertyNames.KavaDocsTopic] as DocTopic;
            if (editorTopic == topic)
                topic.Body = Model.MarkdownMonsterModel.ActiveEditor.GetMarkdown();            
            else
            {
                // TODO: Check if the topic is open in another tab
                var tab = Model.MarkdownMonsterModel.Window.GetTabFromFilename(topic.GetTopicFileName());
                if (tab != null)
                {
                    topic = (tab.Tag as MarkdownDocumentEditor)?.Properties[Constants.EditorPropertyNames.KavaDocsTopic] as DocTopic;
                    if(topic != null)
                        topic.Body = Model.MarkdownMonsterModel.ActiveEditor.GetMarkdown();                    
                }
                else
                    topic.LoadTopicFile(); // get latest from disk
            }

            // delete the old file
            topic.DeleteTopicFile();  // delete in old location

            // create new link and slug
            topic.CreateRelativeSlugAndLink();
            topic.SaveTopicFile();  // write in new location
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

        private void TreeTopicBrowser_GotFocus(object sender, RoutedEventArgs e)
        {

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
