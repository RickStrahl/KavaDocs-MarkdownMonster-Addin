using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocMonster;
using DocMonster.Configuration;
using DocMonster.Model;
using DocMonsterAddin.Controls;
using DocMonsterAddin.WebServer;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.AddIns;
using MarkdownMonster.Controls;
using MarkdownMonster.Windows;
using MarkdownMonster.Windows.PreviewBrowser;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Westwind.Utilities;

namespace DocMonsterAddin
{
    public class DocMonsterAddin : MarkdownMonster.AddIns.MarkdownMonsterAddin
    {
        public static DocMonsterAddin Current;

        /// <summary>
        /// KavaDocs global configuration settings
        /// </summary>
        public DocMonsterConfiguration Configuration { get; set; }

        /// <summary>
        /// The KavaDocs Addin model
        /// </summary>
        public DocMonsterModel DocMonsterModel { get; set; }


        private bool IsAddinInitialized = false;


        public SimpleHttpServer WebServer {get; set; }

        #region Control References

        /// <summary>
        /// The KavaDocs main menu that is hooked to MM
        /// </summary>
        public DocMonsterMenuHandler DocMonsterMenu { get; set; }

        /// <summary>
        /// Reference to the KavaDocs Topic Editor Tab
        /// </summary>
        public TabItem DocMonsterTopicEditorTab { get; private set; }

        /// <summary>
        /// Reference to the Topic Editor Control
        /// </summary>
        public TopicEditor TopicEditor { get; private set; }


        /// <summary>
        /// Reference to the tab that contains the Topic Tree
        /// </summary>
        public TabItem DocMonsterTopicTreeTab { get; set; }

        /// <summary>
        /// Reference to the the Topic Tree Control
        /// </summary>
        public TopicsTree Tree { get; set; }

        public ImageSource DocMonsterIconImageSource => new BitmapImage(new Uri("pack://application:,,,/DocMonsterAddin;component/Assets/icon_128.png"));

        #endregion

        

        #region Initialization
        public override async Task OnApplicationStart()
        {
            Current = this;

            await base.OnApplicationStart();

            // Id - should match output folder name. REMOVE 'Addin' from the Id
            Id = "DocMonster";

            // a descriptive name - shows up on labels and tooltips for components
            // REMOVE 'Addin' from the Name
            Name = "Documentation Monster";


            // by passing in the add in you automatically
            // hook up OnExecute/OnExecuteConfiguration/OnCanExecute
            var menuItem = new AddInMenuItem(this)
            {
                Caption = Name,
              
            };

            try
            {
#pragma warning disable CA1416
                menuItem.IconImageSource = new ImageSourceConverter()
#pragma warning restore CA1416
                    .ConvertFromString("pack://application:,,,/DocMonsterAddin;component/Assets/icon_16.png") as ImageSource;
            }
            catch
            {

                // if an icon is specified it shows on the toolbar
                // if not the add-in only shows in the add-ins menu
                menuItem.FontawesomeIcon = FontAwesome6.EFontAwesomeIcon.Solid_Paw;
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
                
                DocMonsterModel = kavaUi.Model;
                DocMonsterModel.Addin = this;
                Configuration = kavaUi.Configuration;
                DocMonsterModel.Configuration = Configuration;
                Configuration.Initialize();

                // this doesn't work for in combination with the Topic browser so turn it off
                mmApp.Configuration.FolderBrowser.TrackDocumentInFolderBrowser = false;
                 
                if (Configuration.OpenLastProject)
                {
                    DocMonsterModel.ActiveProject = DocProjectManager.Current.LoadProject(Configuration.LastProjectFile);
                    if (DocMonsterModel.ActiveProject != null)
                        Model.Window.Dispatcher.Delay(10, p => Tree.LoadProject(DocMonsterModel.ActiveProject));
                }
                IsAddinInitialized = true;

                OpenDocMonsterTabs(false);

                // If no project is open try to open one
                if (DocMonsterModel.ActiveProject == null)
                    DocMonsterModel.Commands.OpenProjectCommand.Execute(null);
            }
            
        }

        /// <summary>
        /// Remove the Addin from Markdown Monster
        /// </summary>
        public void UninitializeKavaDocs()
        {
            if (IsAddinInitialized)
            {                
                mmApp.Model.Window.MainMenu.Items.Remove(DocMonsterMenu.KavaDocsMenuItem);
                mmApp.Model.Window.ShowRightSidebar(true);
                mmApp.Model.Window.ShowFolderBrowser();

                // Hide  Sidebar  Tab
                var lsb = mmApp.Model.Window.LeftSidebar;
                var config = lsb?.Configuration;
                if (config != null)
                {
                    var sbtDocMonster = config["Documentation Monster"];
                    sbtDocMonster.TabItem.Visibility = Visibility.Collapsed;
                }

                DocMonsterModel.Model.Window.RemoveRightSideBarPanelTabItem(DocMonsterTopicEditorTab);

                DocMonsterModel = null;                
                IsAddinInitialized = false;
            }
        }


        public void OpenDocMonsterTabs(bool noSelection = false)
        {

            
            var config = mmApp.Configuration.LeftSidebar;
            var leftSidebar = mmApp.Model.Window.LeftSidebar;

            // Activate the Sidebar
            Model?.Window?.ShowFolderBrowser();


            var sbtDocMonster = config["Documentation Monster"];
            if (sbtDocMonster == null)
            {
                sbtDocMonster = new SidebarTab()
                {
                    Tabname = "Documentation Monster",
                    TabLocation = SidebarTabLocation.Top,
                };
                config.Tabs.Add(sbtDocMonster);
            }

            Tree = new TopicsTree();    // set in mainline
            sbtDocMonster.TabContent = Tree;
            sbtDocMonster.HeaderImage = DocMonsterIconImageSource;

            //new ImageAwesome { Icon = EFontAwesomeIcon.Duotone_CircleQuestion, PrimaryColor = Brushes.White, SecondaryColor = System.Windows.Media.Brushes.SteelBlue, SecondaryOpacity = 1, Height = 23 }.Source;

            var tabItem = leftSidebar.CreateTabItemFromSidebarTab(sbtDocMonster);
            DocMonsterTopicTreeTab = tabItem;

            mmApp.Model.Window.LeftSidebar.RefreshTabBindings();
            if (!noSelection)
                leftSidebar.SelectTab(sbtDocMonster.TabItem);

            // Set up the KavaDocs Topic Tree in the Left Sidebar
            tabItem = new MetroTabItem() { Name = "KavaDocsTopic" };
            DocMonsterTopicEditorTab = tabItem;
            TopicEditor = new TopicEditor();
            tabItem.Content = TopicEditor;

            Model.Window.AddRightSidebarPanelTabItem(tabItem, "Topic", DocMonsterIconImageSource);


            DocMonsterMenu = new DocMonsterMenuHandler();
            DocMonsterMenu.CreateKavaDocsMainMenu();

        }


        public override Task OnWindowLoaded()
        {     
            if (kavaUi.Configuration.AutoOpen)
                OnExecute(null);
            return Task.CompletedTask;
        }


        public override Task OnApplicationShutdown()
        {
            if (DocMonsterModel != null)
            {
                DocMonsterModel.ActiveProject?.CloseProject();
                DocMonsterModel.Configuration.Write();
            }

            // stop web server if its running
            SimpleHttpServer.StopHttpServerOnThread();

            base.OnApplicationShutdown();
            return Task.CompletedTask;
        }


        public void StartWebServer()
        {
            if (DocMonsterModel?.ActiveProject == null) return;
   
            var result = SimpleHttpServer.StartHttpServerOnThread(DocMonsterModel.ActiveProject.OutputDirectory, Configuration.WebServerPort);
            
            if (!result)
            {
                Model.Window.ShowStatusError("Preview Web Server was not started.");                
                return;
            }

            ShellUtils.GoUrl("http://localhost:" + Configuration.WebServerPort);
        }

        public void StopWebServer()
        {
            SimpleHttpServer.StopHttpServerOnThread();
        }


        #endregion

        #region Interception Hooks

        public override Task OnExecute(object sender)
        {
            if (IsAddinInitialized)
            {
                UninitializeKavaDocs();
                return Task.CompletedTask;
            }

            InitializeKavaDocs();  // will check if already loaded
            return Task.CompletedTask;
        }


        public override async Task OnExecuteConfiguration(object sender)
        {            
            var configFile = Path.Combine(mmApp.Configuration.CommonFolder, "docmonsteraddin.json");
            if (!File.Exists(configFile))
                DocMonsterConfiguration.Current.Write();
          
            await Model.Window.OpenTab(configFile);
        }


        public override async Task OnDocumentUpdated()
        {
            if (DocMonsterModel == null || Model?.ActiveEditor == null || Model.ActiveEditor.Identifier != "KavaDocsDocument")
                return;

            var topic = GetTopicFromEditor();
            if (topic != null && Model.ActiveEditor != null)
            {               
                var md = await Model.ActiveEditor.GetMarkdown();
                if(md != null)
                    topic.Body = md;

                Model.ActiveEditor.Properties[Constants.EditorPropertyNames.KavaDocsUnedited] = false;
            }            
        }

        public override Task OnAfterSaveDocument(MarkdownDocument doc)
        {
            
            base.OnAfterSaveDocument(doc);
           
            if (doc == null || DocMonsterModel == null)
                return Task.CompletedTask;


            // Reload settings after saving them in the editor
            if (doc.Filename.IndexOf("docmonsteraddin.json", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                DocMonsterModel.Configuration.Read();
                return Task.CompletedTask;
            }


            // Reload Project file if editing project file
            if (DocMonsterModel.ActiveProject != null &&
                DocMonsterModel.ActiveProject.Filename.Equals(doc.Filename,StringComparison.InvariantCultureIgnoreCase))
            {
                DocMonsterModel.LoadProject(DocMonsterModel.ActiveProject.Filename);
                return Task.CompletedTask;
            }
            
            if (DocMonsterModel?.ActiveTopic != null )
            {
                var topic = Model.ActiveEditor.GetProperty<DocTopic>(Constants.EditorPropertyNames.KavaDocsTopic);
                if (topic != null)
                {
                    InitializeKavaDocs(); // will check if already loaded
                    OnTopicFilesSaved(topic, doc);
                    return Task.CompletedTask;
                }                
            }

            return Task.CompletedTask;
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
                DocMonsterModel.LoadProject(DocMonsterModel.ActiveProject.Filename);
                return;
            }

            // Check for explicit KavaDocs Documents
            if (string.IsNullOrEmpty(doc.CurrentText))
                return;

            // Save the underlying topic file
            if (filename.Equals(topic.GetTopicFileName(), StringComparison.InvariantCultureIgnoreCase))
            {
                DocMonsterModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                DocMonsterModel.ActiveProject.SaveProject();
            }
            // READ-ONLY this shouldn't really happen any more
            // Saving the KavaDocs.md file - assume we're on the active topic 
            else if (filename.Equals(topic.GetKavaDocsEditorFilePath(),StringComparison.InvariantCultureIgnoreCase))
            {
                topic.Body = doc.CurrentText;
                DocMonsterModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                DocMonsterModel.ActiveProject.SaveProject();
            }
            // Any previously activated document file
            else if (DocMonsterModel.ActiveMarkdownEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))
            {
                DocMonsterModel.ActiveProject.UpdateTopicFromMarkdown(doc, objTopic as DocTopic);
                DocMonsterModel.ActiveProject.SaveProject();
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

        public override Task OnDocumentActivated(MarkdownDocument doc)
        {
            base.OnDocumentActivated(doc);

            if (DocMonsterModel == null || Model?.ActiveEditor == null)
                return Task.CompletedTask;
            
            if (!Model.ActiveEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic, out object objTopic))                                                    
                 return Task.CompletedTask;            

            var topic = objTopic as DocTopic;
            if (topic == null) return Task.CompletedTask;

            DocMonsterModel.ActiveTopic = topic;
            topic.TopicState.IsSelected = true;

            return Task.CompletedTask;
        }



        public override bool OnCanExecute(object sender)
        {
            return true;
        }
        #endregion


        #region Previewing

   

        /// <summary>
        /// Override the Preview rendering for Links and using Topic Rendering logic
        /// </summary>
        /// <param name="renderedHtml"></param>
        /// <param name="markdownHtml"></param>
        /// <returns></returns>
        public override Task<string> OnModifyPreviewHtml(string renderedHtml, string markdownHtml)
        {

            // default rendering if specified
            if (DocMonsterModel == null ||
                kavaUi.Configuration.TopicRenderMode == TopicRenderingModes.MarkdownDefault)
                return Task.FromResult(renderedHtml);

            if (mmApp.Model.ActiveEditor == null ||
                !mmApp.Model.ActiveEditor.Properties.TryGetValue(Constants.EditorPropertyNames.KavaDocsTopic,
                    out object objTopic))
                return Task.FromResult(renderedHtml);

            var topic = objTopic as DocTopic;
            if (topic == null || topic != DocMonsterModel.ActiveTopic)
                return Task.FromResult(renderedHtml);

            var editor = mmApp.Model.ActiveEditor;
            var doc = mmApp.Model.ActiveEditor?.MarkdownDocument;
            if (doc == null)
                return Task.FromResult(renderedHtml);


            //topic.Body = await mmApp.Model.ActiveEditor.GetMarkdown();
            topic.Project.Settings.ActiveRenderMode = HtmlRenderModes.Preview;
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
                renderedHtml = topic.RenderTopic( TopicRenderModes.Preview);

                //topic.TopicState.IsPreview = false;
                //topic.RenderTopicToFile(renderMode: TopicRenderModes.Html);
                
                //var url = "http://localhost:5200/docs/" + topic.Slug + ".html";
                //ShellUtils.GoUrl(url);
                //var handler = Model.Window.PreviewBrowser as WebViewPreviewControl;
                //handler.Background = Brushes.White;
                //Model.Window.Dispatcher.Delay(100,() =>                 
                //handler.Navigate(url), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }


            return Task.FromResult(renderedHtml); //return base.OnModifyPreviewHtml(renderedHtml, markdownHtml);
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


        public void PreviewTopicExternal()
        {

            var model = kavaUi.Model;            
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
