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
using DocMonster.Model;
using DocMonster.Utilities;
using DocMonster.Windows.Dialogs;
using DocMonsterAddin.Core.Configuration;
using MarkdownMonster;
using MarkdownMonster.Windows;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Westwind.Utilities;

namespace DocMonsterAddin.Controls
{
    /// <summary>
    /// Interaction logic for TopicsTree.xaml
    /// </summary>
    public partial class TopicsTree : UserControl
    {
        public TopicsTreeModel Model { get; set; }

        public TopicsTree(TopicsTreeModel model = null, DocProject project = null)
        {
            InitializeComponent();
            Loaded += TopicsTree_Loaded;

            if (model == null)
                model = new TopicsTreeModel(project);

            Model = model;
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
                Model.DocMonsterModel.Configuration.AddRecentProjectItem(Model.Project.Filename,
                    Model.DocMonsterModel.ActiveTopic?.Id,Model.Project.Title);
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

            Model.DocMonsterModel.Configuration.AddRecentProjectItem(project.Filename,projectTitle: project.Title);
        }

  

        public void RefreshTree()
        {
            Model.RefreshTree();
        }

        public void SetSearchText(string searchText, bool selectAll = false)
        {
            Dispatcher.Invoke(() => {
                TextSearchText.Text = searchText;
                TextSearchText.Focus();

                if (selectAll)
                    TextSearchText.SelectAll();

            },DispatcherPriority.Background);
        }

        #endregion

        #region Selection Handling

        private long _HandleSelectionDoubleExecTicks;

        /// <summary>
        /// Selects a topic in the treeview by making the topic
        /// selected and then ensuring it's visible.
        ///
        /// This method does not update the topic in the editor or
        /// update the preview, or save the previous topic - if you
        /// need this behavior use HandleSelection() instead.
        /// </summary>
        /// <param name="topic"></param>
        public void SelectTopic(DocTopic topic)
        {
            var lastTopic = kavaUi.Model.ActiveTopic;

            var foundTopic = Model.FindTopicInTree(null, topic);
            if (foundTopic != null)
            {
                if (lastTopic != null)
                    lastTopic.TopicState.IsSelected = false;

                foundTopic.TopicState.IsSelected = true;
                var parent = foundTopic.Parent;
                while (parent != null)
                {
                    parent.IsExpanded = true;
                    parent = parent.Parent;
                }
            }
        }
        /// <summary>
        /// Selects a new topic updates the editor and preview.
        /// This method is more complete than SetSelection()
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="forceFocus"></param>
        /// <returns></returns>
        public async Task<bool> HandleSelection(DocTopic topic = null, bool forceFocus = false, bool dontSaveProject = false)
        {

            // avoid double selection from here
            var ticks = DateTime.Now.Ticks;
            if (ticks - _HandleSelectionDoubleExecTicks < 10_000 * 100)
                return true;

            _HandleSelectionDoubleExecTicks = ticks;

            var editor = Model.DocMonsterModel?.ActiveMarkdownEditor;

            bool selectTopic = false;
            if (topic == null)
                topic = TreeTopicBrowser.SelectedItem as DocTopic;
            else
                selectTopic = true;


            if (topic == null)
                return false;

            if (Model.SelectionHandler != null)
            {
                if (Model.SelectionHandler.Invoke(topic,false))
                    return true;                
            }

            //if (topic.Topics.Count > 0)
            //    topic.IsExpanded = !topic.IsExpanded;

            var lastTopic = kavaUi.Model.ActiveTopic;
            if (lastTopic != null)
            {
                lastTopic.TopicState.IsSelected = false;               
                if (editor != null)
                {
                    var md = await editor.GetMarkdown();
                    if (md != null)
                    {
                        if(lastTopic.SaveTopicFile(md))
                           await editor.SetDirty(false);
                    }
                    if (Model.DocMonsterModel.Configuration.CloseTopicsOnDeselection)
                        await mmApp.Model.Window.CloseTab(editor.MarkdownDocument.Filename);
                }
            }
            kavaUi.Model.LastTopic = lastTopic;



            // TODO: This doesn't seem to work correctly - saves, but also affects
            //       the new topic.
            // Save the previously active topic 
            if (!dontSaveProject && kavaUi.Model.LastTopic.TopicState.IsDirty)
            {                
                bool result = await SaveProjectFileForTopic(kavaUi.Model.LastTopic);
                if (result)
                    kavaUi.Model.Window.ShowStatus("Topic saved.", 3000);
            }


            kavaUi.Model.ActiveTopic = topic;

            // TODO: Move to function
            if (kavaUi.Model.RecentTopics.Contains(topic))
                kavaUi.Model.RecentTopics.Remove(topic);
            kavaUi.Model.RecentTopics.Insert(0, topic);

            if (kavaUi.Model.RecentTopics.Count > 15)
                kavaUi.Model.RecentTopics =
                    new ObservableCollection<DocTopic>(kavaUi.Model.RecentTopics.Take(14));


            // set topic state to selected and unchanged
            if (selectTopic)
                topic.TopicState.IsSelected = true;
            topic.TopicState.IsDirty = false;

            Dispatcher.InvokeAsync(() => OpenTopicInEditor().FireAndForget(), DispatcherPriority.Normal);

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

        private DateTime _lastDoubleClick;
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (Model.SelectionHandler != null)
            {
                var ms =  (DateTime.UtcNow - _lastDoubleClick).TotalMilliseconds;
                if (ms < 1500)
                    return;

                _lastDoubleClick = DateTime.UtcNow;

                var topic = TreeTopicBrowser.SelectedItem as DocTopic;
                if (topic != null && Model.SelectionHandler.Invoke(topic,true))
                    return;
            }

            OpenTopicInMMEditor().FireAndForget();                  
        }


        /// <summary>
        /// Saves a topic in the tree and then saves the project to
        /// disk.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="project"></param>
        /// <param name="async"></param>
        /// <returns>false if data wasn't written (could be because there's nothing that's changed)</returns>
        public async Task<bool> SaveProjectFileForTopic(DocTopic topic, DocProject project = null, bool async = false)
        {
            if (topic == null)
                return false;

            if (!topic.TopicState.IsDirty)
                return false;

            if (project == null)
                project = kavaUi.Model.ActiveProject;

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
        public async Task<TabItem> OpenTopicInMMEditor(DocTopic topic = null)
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
                    await File.WriteAllTextAsync(file, "");

                tab = await Model.DocMonsterModel.Window.RefreshTabFromFile(file, isPreview: false, noFocus: false);
                Model?.DocMonsterModel?.Window?.BindTabHeaders();

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
        public async Task<TabItem> OpenTopicInEditor(bool setFocus = false)
        {
            Debug.WriteLine("OpenTopicInEditor");

            var topic = TreeTopicBrowser.SelectedItem as DocTopic;
            if (topic == null)
                return null;

            var window = Model.DocMonsterModel.Window;

            TabItem tab;           

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

                
                tab = await window.RefreshTabFromFile(file, isPreview: true, noFocus: true,
                    noPreview: true,
                    existingTab: window.PreviewTab);

                
                editor = tab?.Tag as MarkdownDocumentEditor;
                if (editor == null)
                    return null;
            }

            if (tab == null)
                return null;

            // make sure topic is associated with editor
            SetEditorWithTopic(editor, topic, isUnEdited: true);

            
            // Explicitly read in the current text from an open tab and save to body                
            var body = await editor.GetMarkdown();
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
                await editor.SetMarkdown(topic.Body);
            
            await window.PreviewMarkdownAsync();
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

                    HandleSelection().FireAndForget();
                }

            }, e, DispatcherPriority.Background, Dispatcher);            
        }

        private void TextSearch_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Down)
            {                
                if (TreeTopicBrowser.Items.Count > 0)
                {
                    DocTopic topic = null;
                    var oldTopic = TreeTopicBrowser.SelectedItem as DocTopic;
                    if (oldTopic != null && !oldTopic.TopicState.IsHidden)
                        topic = oldTopic;
                    else
                    {
                        // search for first non-hidden item in the root tree list!
                        topic = Model.FilteredTopicTree.FirstOrDefault(t => !t.TopicState.IsHidden);
                        if (topic == null)
                            return;
                    }

                    SelectTopic(topic);

                    var tvi = WindowUtilities.GetNestedTreeviewItem(topic, TreeTopicBrowser);
                    if (tvi == null) return;
                    TreeTopicBrowser.Focus();
                    tvi.Focus();                    

                    e.Handled = true;
                }
            }
        }

        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // Tabbing out of treeview sets focus to editor
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                if (Model.NonDefaultHandTreeHandling) return; 


                var tvi = e.OriginalSource as TreeViewItem;
                if (tvi == null)
                    return;

                var topic = tvi.Tag as DocTopic;
                OpenTopicInMMEditor().FireAndForget();
            }

            // this works without a selection
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Model.DocMonsterModel.Commands.NewTopicCommand.Execute(null);
            }
            else if (e.Key == Key.Delete)
            {
                Model.DocMonsterModel.Commands.DeleteTopicCommand.Execute(null);
            }
            // Find
            else if (e.Key == Key.F && Keyboard.IsKeyDown(Key.LeftCtrl) )
            {
                TextSearchText.Focus();
                e.Handled = true;
            }

        }

        public TreeViewItem GetTreeviewItem(DocTopic item)
        {
            return (TreeViewItem) TreeTopicBrowser
                .ItemContainerGenerator
                .ContainerFromItem(item);
        }

        #endregion

        #region SearchKey

        #endregion



        #region Drag and Drop


        internal DragMoveResult DragMoveResult = null;
        ContextMenu _dragContextMenu;
        private Point _lastMouseDownPoint;

        public CommandBase MoveTopicCommand { get; set; }

        private async void TreeViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var topic = (e.OriginalSource as FrameworkElement)?.DataContext as DocTopic;
            if (topic == null )
                return;

            if (e.ChangedButton == MouseButton.Left)
                _lastMouseDownPoint = e.GetPosition(TreeTopicBrowser);
            
            if (!await HandleSelection(topic))
                return;

            if (TreeTopicBrowser.SelectedItem is DocTopic)
            {
                var tvi = e.OriginalSource as TreeViewItem;
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

            var tvItem = WindowUtilities.FindAncestor<TreeViewItem>( (DependencyObject) e.OriginalSource);
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
                    targetTopics = Model.DocMonsterModel.ActiveProject.Topics;

                var targetParentTopics = dragResult.TargetTopic.Parent?.Topics;
                if (targetParentTopics == null)
                    targetParentTopics = Model.DocMonsterModel.ActiveProject.Topics;

                var sourceTopics = dragResult.SourceTopic?.Parent?.Topics;
                if (sourceTopics == null)
                    sourceTopics = Model.DocMonsterModel.ActiveProject.Topics;   

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

                    UpdateMovedTopic(dragResult.SourceTopic).FireAndForget();
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
                    UpdateMovedTopic(dragResult.SourceTopic).FireAndForget();
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
                    UpdateMovedTopic(dragResult.SourceTopic).FireAndForget();
                }

                Model.DocMonsterModel.ActiveProject.SaveProject();
            }, (p, c) => true);
        }

        async Task UpdateMovedTopic(DocTopic topic)
        {
            // TODO: Get latest changes from Editor
            var editorTopic = Model.MarkdownMonsterModel.ActiveEditor?.Properties[Constants.EditorPropertyNames.KavaDocsTopic] as DocTopic;
            if (editorTopic == topic)
                topic.Body = await Model.MarkdownMonsterModel.ActiveEditor.GetMarkdown();            
            else
            {
                // TODO: Check if the topic is open in another tab
                var tab = Model.MarkdownMonsterModel.Window.GetTabFromFilename(topic.GetTopicFileName());
                if (tab != null)
                {
                    topic = (tab.Tag as MarkdownDocumentEditor)?.Properties[Constants.EditorPropertyNames.KavaDocsTopic] as DocTopic;
                    if(topic != null)
                        topic.Body = await Model.MarkdownMonsterModel.ActiveEditor.GetMarkdown();                    
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

        private void TreeTopicBrowser_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var ctxHandler = new TreeviewContextMenuHandler();
            TreeTopicBrowser.ContextMenu = ctxHandler.CreateTreeviewContextMenu();
            TreeTopicBrowser.ContextMenu.IsOpen = true;
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
