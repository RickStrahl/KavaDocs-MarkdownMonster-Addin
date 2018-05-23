using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DocHound;
using DocHound.Configuration;
using DocHound.Model;
using FontAwesome.WPF;
using KavaDocsAddin.Controls;
using MarkdownMonster;
using MarkdownMonster.AddIns;
using MarkdownMonster.Windows;
using Westwind.Utilities;

namespace KavaDocsAddin
{
    public class KavaDocsAddin : MarkdownMonster.AddIns.MarkdownMonsterAddin
    {
        /// <summary>
        /// KavaDocs global configuration settings
        /// </summary>
        public KavaDocsConfiguration Configuration { get; set; }

        /// <summary>
        /// The KavaDocs Addin model
        /// </summary>
        public KavaDocsModel KavaDocsModel { get; set; }


        private bool IsAddinInitialized = false;


        #region Control References

        /// <summary>
        /// The KavaDocs main menu that is hooked to MM
        /// </summary>
        public KavaDocsMenuHandler KavaDocsMenu { get; set; }

        /// <summary>
        /// Reference to the KavaDocs Topic Editor Tab
        /// </summary>
        public TabItem KavaDocsTopicEditorTab { get; private set; }

        /// <summary>
        /// Reference to the Topic Editor Control
        /// </summary>
        public TopicEditor TopicEditor { get; private set; }


        /// <summary>
        /// Reference to the tab that contains the Topic Tree
        /// </summary>
        public TabItem KavaDocsTopicTreeTab { get; set; }

        /// <summary>
        /// Reference to the the Topic Tree Control
        /// </summary>
        public TopicsTree Tree { get; set; }
        #endregion


        #region Initialization
        public override void OnApplicationStart()
        {
            base.OnApplicationStart();


            // Id - should match output folder name. REMOVE 'Addin' from the Id
            Id = "KavaDocs";

            // a descriptive name - shows up on labels and tooltips for components
            // REMOVE 'Addin' from the Name
            Name = "Kava Docs";


            // by passing in the add in you automatically
            // hook up OnExecute/OnExecuteConfiguration/OnCanExecute
            var menuItem = new AddInMenuItem(this)
            {
                Caption = Name,
              
            };

            try
            {
                menuItem.IconImageSource = new ImageSourceConverter()
                    .ConvertFromString("pack://application:,,,/KavaDocsAddin;component/Assets/icon_16.png") as ImageSource;
            }
            catch
            {

                // if an icon is specified it shows on the toolbar
                // if not the add-in only shows in the add-ins menu
                menuItem.FontawesomeIcon = FontAwesomeIcon.Paw;
            }

            // if you don't want to display config or main menu item clear handler
            //menuItem.ExecuteConfiguration = null;

            // Must add the menu to the collection to display menu and toolbar items            
            MenuItems.Add(menuItem);

        }      


        /// <summary>
        /// Attach the addin to Markdown Monster
        /// </summary>
        public void InitializeKavaDocs()
        {
            if (!IsAddinInitialized)
            {
                kavaUi.Addin = this;

                KavaDocsModel = kavaUi.AddinModel;
                KavaDocsModel.Addin = this;
                Configuration = kavaUi.Configuration;
                KavaDocsModel.Configuration = Configuration;


                // Set up the KavaDocs Topic Tree in the Left Sidebar
                var tabItem = new TabItem() {Name = "KavaDocsTree", Header = " Kava Docs "};
                KavaDocsTopicTreeTab = tabItem;
                Tree = new TopicsTree();
                tabItem.Content = Tree;
                Model.Window.AddLeftSidebarPanelTabItem(tabItem);

                // Kava Docs Topic Editor Tab
                tabItem = new TabItem() { Name = "KavaDocsTopic", Header = " Topic " };
                KavaDocsTopicEditorTab = tabItem;

                TopicEditor = new TopicEditor();
                tabItem.Content = TopicEditor;
                Model.Window.AddRightSidebarPanelTabItem(tabItem);


                KavaDocsMenu = new KavaDocsMenuHandler();
                KavaDocsMenu.CreateKavaDocsMainMenu();

                if (Configuration.OpenLastProject)
                {
                    KavaDocsModel.ActiveProject = DocProjectManager.Current.LoadProject(Configuration.LastProjectFile);
                    if (KavaDocsModel.ActiveProject != null)
                        Model.Window.Dispatcher.Delay(10, p => Tree.LoadProject(KavaDocsModel.ActiveProject));
                }
                IsAddinInitialized = true;

                // Activate the Tab
                Model.Window.ShowFolderBrowser();
                Model.Window.SidebarContainer.SelectedItem = KavaDocsTopicTreeTab;

                // If no project is open try to open one
                if (KavaDocsModel.ActiveProject == null)
                    KavaDocsModel.Commands.OpenProjectCommand.Execute(null);
            }
        }


        /// <summary>
        /// Remove the Addin from Markdown Monster
        /// </summary>
        public void UninitializeKavaDocs()
        {
            if (IsAddinInitialized)
            {
                // Set up the KavaDocs Topic Tree in the Left Sidebar
                mmApp.Model.Window.SidebarContainer.Items.Remove(KavaDocsTopicTreeTab);
                mmApp.Model.Window.RightSidebarContainer.Items.Remove(KavaDocsTopicEditorTab);
                mmApp.Model.Window.MainMenu.Items.Remove(KavaDocsMenu.KavaDocsMenuItem);
                mmApp.Model.Window.ShowRightSidebar(true);
                mmApp.Model.Window.ShowFolderBrowser();

                KavaDocsModel = null;                

                IsAddinInitialized = false;
            }
        }


        public override void OnApplicationShutdown()
        {
            if (KavaDocsModel != null)
            {
                KavaDocsModel.ActiveProject?.CloseProject();
                KavaDocsModel.Configuration.Write();
            }

            base.OnApplicationShutdown();                   
        }

        #endregion

        #region Interception Hooks

        public override void OnWindowLoaded()
        {
            if (kavaUi.Configuration.AutoOpen)
                OnExecute(null);
        }


        public override void OnExecute(object sender)
        {
            if (IsAddinInitialized)
            {
                UninitializeKavaDocs();
                return;
            }

            InitializeKavaDocs();  // will check if already loaded
        }


        public override void OnAfterSaveDocument(MarkdownDocument doc)
        {
            base.OnAfterSaveDocument(doc);
           
            if (doc == null)
                return;


            if (doc.Filename.IndexOf("kavadocsaddin.json", StringComparison.InvariantCultureIgnoreCase) > -1)
            {                
                KavaDocsModel.Configuration.Read(); // reload settings from saved
                return;
            }



            if (KavaDocsModel?.ActiveTopic != null )
            {
                var topic = Model.ActiveEditor.GetProperty<DocTopic>(Constants.EditorPropertyNames.KavaDocsTopic);
                if (topic != null)
                {
                    InitializeKavaDocs(); // will check if already loaded
                    OnTopicFilesSaved(topic, doc);
                    return;
                }                
            }
        }

        /// <summary>
        /// Called after a topic file (the MD file linked to a topic)
        /// has been saved on disk. This allows updating of topic
        /// info from the saved content.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="doc"></param>
        public void OnTopicFilesSaved(DocTopic topic, MarkdownDocument doc)
        {
            if (topic == null)
                return;

            var projectFilename = topic?.Project?.Filename;
            var filename = doc.Filename.ToLower();


            if (topic.Body == "NoContent")
                topic.Body = null;

            // Save Project File
            if (projectFilename != null && filename.Equals(projectFilename,StringComparison.InvariantCultureIgnoreCase))
            {
                // reload the project
                KavaDocsModel.LoadProject(KavaDocsModel.ActiveProject.Filename);
                return;
            }

            // Check for explicit KavaDocs Documents
            if (string.IsNullOrEmpty(doc.CurrentText))
                return;

            // Save the underlying topic file
            if (filename.Equals(topic.GetTopicFileName(), StringComparison.InvariantCultureIgnoreCase))
            {
                KavaDocsModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                KavaDocsModel.ActiveProject.SaveProject();
            }
            // READ-ONLY this shouldn't really happen any more
            // Saving the KavaDocs.md file - assume we're on the active topic 
            else if (filename.Equals(topic.GetKavaDocsEditorFilePath(),StringComparison.InvariantCultureIgnoreCase))
            {
                topic.Body = doc.CurrentText;
                KavaDocsModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                KavaDocsModel.ActiveProject.SaveProject();
            }
            // Any previously activated document file
            else if (KavaDocsModel.ActiveMarkdownEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))
            {
                KavaDocsModel.ActiveProject.UpdateTopicFromMarkdown(doc, objTopic as DocTopic);
                KavaDocsModel.ActiveProject.SaveProject();
            }
        }

        DocTopic GetTopicFromEditor()
        {
            if (Model.ActiveEditor == null)
                return null;

            if (!Model.ActiveEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))
                return null;

            return objTopic as DocTopic;
        }

        public override void OnDocumentActivated(MarkdownDocument doc)
        {
            base.OnDocumentActivated(doc);
            if (KavaDocsModel == null || Model?.ActiveEditor == null)
                return;
            
            if (!Model.ActiveEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))                                                    
                 return;            

            var topic = objTopic as DocTopic;
            KavaDocsModel.ActiveTopic = topic;
            topic.TopicState.IsSelected = true;
        }

        public override void OnDocumentUpdated()
        {
            if (KavaDocsModel == null || Model?.ActiveEditor == null || Model.ActiveEditor.Identifier != "KavaDocsDocument")
                return;

            Model.ActiveEditor.Properties[Constants.EditorPropertyNames.KavaDocsUnedited] = false;
        }



        public override void OnExecuteConfiguration(object sender)
        {
            Model.Window.OpenTab(Path.Combine(Model.Configuration.CommonFolder, "KavaDocsAddin.json"));
        }

        public override bool OnCanExecute(object sender)
        {
            return true;
        }
        #endregion


        #region Previewing

        /// <summary>
        /// OVerride the Preview rendering for Links and using Topic Rendering logic
        /// </summary>
        /// <param name="renderedHtml"></param>
        /// <param name="markdownHtml"></param>
        /// <returns></returns>
        public override string OnModifyPreviewHtml(string renderedHtml, string markdownHtml)
        {
            
            if (mmApp.Model.ActiveEditor == null ||
                !mmApp.Model.ActiveEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))
                return renderedHtml;

            var topic = objTopic as DocTopic;

            if (topic == null || topic != KavaDocsModel.ActiveTopic) 
                return renderedHtml;
            
  
            topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Preview;
            topic.TopicState.IsPreview = true;

            if (topic.IsLink)
            {
                renderedHtml = topic.Body;
                if (renderedHtml == "NoContent")
                    renderedHtml = null;

                if (string.IsNullOrEmpty(topic.Body) && topic.Link.StartsWith("http"))
                    renderedHtml = topic.Link;            
            }
            else
            {
                // Currently we're not rendering topics and just using MM to render topic content
                //renderedHtml = topic.RenderTopicToFile(addPragmaLines: true);
            }

            topic.TopicState.IsPreview = false;
            topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Html;

            return renderedHtml; //return base.OnModifyPreviewHtml(renderedHtml, markdownHtml);
        }

        // Completely take over preview rendering

        //// IMPORTANT: for browser COM CSE errors which can happen with script errors
        //[HandleProcessCorruptedStateExceptions]
        //[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        //public void PreviewTopic(bool keepScrollPosition = false, bool usePragmaLines = true)
        //{
        //    var model = kavaUi.AddinModel;
        //    var mmModel = mmApp.Model;
        //    var topic = model.ActiveTopic;
        //    var project = model.ActiveProject;

        //    string html = null;
        //    try
        //    {
        //        // only render if the preview is actually visible and rendering in Preview Browser
        //        if (!mmModel.IsPreviewBrowserVisible)
        //            return;

        //        if (model.ActiveTopic == null)
        //            return;

        //        topic.Project.ActiveRenderMode = HtmlRenderModes.Preview;
        //        topic.TopicState.IsPreview = usePragmaLines;

        //        string renderedHtml = topic.RenderTopicToFile(addPragmaLines: usePragmaLines);

        //        topic.TopicState.IsPreview = false;
        //        topic.Project.ActiveRenderMode = HtmlRenderModes.Html;

        //        if (renderedHtml == null)
        //        {
        //            mmModel.Window.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Red, false);
        //            mmModel.Window.ShowStatus($"Access denied: {System.IO.Path.GetFileName(topic.RenderTopicFilename)}", 5000);
        //            return;
        //        }

        //        renderedHtml = StringUtils.ExtractString(renderedHtml,
        //            "<!-- Documentation Monster Content -->",
        //            "<!-- End Documentation Monster Content -->");


        //        // if content contains <script> tags we must do a full page refresh
        //        bool forceRefresh = renderedHtml != null && renderedHtml.Contains("<script ");

        //        var webBrowser = mmModel.Window.PreviewBrowser.WebBrowser;
        //        if (keepScrollPosition && !forceRefresh)
        //        {
        //            string browserUrl = webBrowser.Source.ToString().ToLower();
        //            string documentFile = new Uri(topic.RenderTopicFilename).ToString().ToLower();

        //            if (browserUrl == documentFile)
        //            {
        //                dynamic dom = webBrowser.Document;
        //                //var content = dom.getElementById("MainContent");
                        
        //                if (string.IsNullOrEmpty(renderedHtml))
        //                    PreviewTopic(false, false); // fully reload document
        //                else
        //                {
        //                    try
        //                    {
        //                        // explicitly update the document with JavaScript code
        //                        // much more efficient and non-jumpy and no wait cursor
        //                        var window = dom.parentWindow;
        //                        window.updateDocumentContent(renderedHtml);

        //                        try
        //                        {
        //                            int lineno = model.ActiveMarkdownEditor.GetLineNumber();
        //                            if (lineno > -1)
        //                                window.scrollToPragmaLine(lineno);
        //                        }
        //                        catch
        //                        {
        //                            /* ignore scroll error */
        //                        }
        //                    }
        //                    catch
        //                    {
        //                        // Refresh doesn't fire Navigate event again so 
        //                        // the page is not getting initiallized properly
        //                        //PreviewBrowser.Refresh(true);
        //                        webBrowser.Tag = "EDITORSCROLL";
        //                        webBrowser.Navigate(new Uri(topic.RenderTopicFilename));
        //                    }
        //                }

        //                return;
        //            }
        //        }

        //        webBrowser.Tag = "EDITORSCROLL";
        //        webBrowser.Navigate(new Uri(topic.RenderTopicFilename));
        //        return;
        //    }
        //    catch
        //    {
        //    }
        //}

        // IMPORTANT: for browser COM CSE errors which can happen with script errors
        [HandleProcessCorruptedStateExceptions]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void PreviewTopicExternal()
        {

            var model = kavaUi.AddinModel;            
            var topic = model.ActiveTopic;

            try
            {
                if (model.ActiveTopic == null)
                    return;

                topic.RenderTopicToFile(topic.RenderTopicFilename);
                ShellUtils.GoUrl(topic.RenderTopicFilename);
            }
            catch
            {
            }
        }
        #endregion

    }

}
