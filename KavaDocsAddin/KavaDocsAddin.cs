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

        public KavaDocsConfiguration Configuration { get; set; }

        private KavaDocsModel _addinModel;

        public KavaDocsModel AddinModel
        {
            get
            {
                return _addinModel;
            }
            set
            {
                _addinModel = value;
            }
        }

        public TabItem KavaDocsTab { get; set; }

        public KavaDocsMenuHandler KavaDocsMenu { get; set; }

        public bool HasUiLoaded = false;

        public TabItem KavaDocsTopicTab { get; private set; }
        public TopicEditor TopicEditor { get; private set; }
        public TopicsTree Tree { get; set; }

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


        public void InitializeKavaDocs()
        {
            if (!HasUiLoaded)
            {
                kavaUi.Addin = this;
                AddinModel = kavaUi.AddinModel;
                AddinModel.Addin = this;
                Configuration = kavaUi.Configuration;


                var tabItem = new TabItem() {Name = "KavaDocsTree", Header = " Kava Docs "};
                KavaDocsTab = tabItem;

                Tree = new TopicsTree();
                tabItem.Content = Tree;
                Model.Window.AddLeftSidebarPanelTabItem(tabItem);


                tabItem = new TabItem() { Name = "KavaDocsTopic", Header = " Topic " };
                KavaDocsTopicTab = tabItem;

                TopicEditor = new TopicEditor();
                tabItem.Content = TopicEditor;
                Model.Window.AddRightSidebarPanelTabItem(tabItem);


                KavaDocsMenu = new KavaDocsMenuHandler();
                KavaDocsMenu.CreateKavaDocsMainMenu();

                if (Configuration.OpenLastProject)
                {
                    AddinModel.ActiveProject = DocProjectManager.Current.LoadProject(Configuration.LastProjectFile);
                    if (AddinModel.ActiveProject != null)
                        Model.Window.Dispatcher.Delay(10, p => Tree.LoadProject(AddinModel.ActiveProject));
                }
                HasUiLoaded = true;
            }
        }


        public override void OnApplicationShutdown()
        {
            if (AddinModel != null)
            {
                AddinModel.ActiveProject?.CloseProject();
                AddinModel.Configuration.Write();
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
            InitializeKavaDocs();  // will check if already loaded

            // Activate the Tab
            Model.Window.ShowFolderBrowser();
            Model.Window.SidebarContainer.SelectedItem = KavaDocsTab;

            // If no project is open try to open one
            if (AddinModel.ActiveProject == null)
                AddinModel.Commands.OpenProjectCommand.Execute(null);
        }


        public override void OnAfterSaveDocument(MarkdownDocument doc)
        {
            base.OnAfterSaveDocument(doc);

            if (doc == null || AddinModel?.ActiveTopic == null)
                return;

            var topic = AddinModel.ActiveTopic;           
            var editor = AddinModel.ActiveMarkdownEditor;
            var projectFilename = topic?.Project?.Filename;
            var lowerFilename = doc.Filename.ToLower();
            
            // Save Project File
            if (projectFilename != null && lowerFilename == projectFilename.ToLower())
            {
                // reload the project
                AddinModel.LoadProject(AddinModel.ActiveProject.Filename);
                return;
            }

            // Check for explicit KavaDocs Documents
            if (topic == null || string.IsNullOrEmpty(doc.CurrentText))
                return;

            // Save the underlying topic file
            if (lowerFilename == topic.GetTopicFileName().ToLower())
            {
                AddinModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                AddinModel.ActiveProject.SaveProject();
            }
            // READ-ONLY this shouldn't really happen any more
            // Saving the KavaDocs.md file - assume we're on the active topic 
            else if (lowerFilename == topic.GetKavaDocsEditorFilePath().ToLower())
            {
                topic.Body = doc.CurrentText;
                AddinModel.ActiveProject.UpdateTopicFromMarkdown(doc, topic);
                AddinModel.ActiveProject.SaveProject();
            }
            // Any previously activated document file
            else if (editor.Properties.TryGetValue("KavaDocsTopic", out object objTopic))
            {
                AddinModel.ActiveProject.UpdateTopicFromMarkdown(doc, objTopic as DocTopic);
                AddinModel.ActiveProject.SaveProject();
            }
        }


        public override void OnDocumentActivated(MarkdownDocument doc)
        {
            base.OnDocumentActivated(doc);
            if (AddinModel == null || Model?.ActiveEditor == null)
                return;
            
            if (!Model.ActiveEditor.Properties.TryGetValue("KavaDocsTopic", out object objTopic))                                                    
                 return;            

            var topic = objTopic as DocTopic;
            AddinModel.ActiveTopic = topic;
            topic.TopicState.IsSelected = true;
        }

        public override void OnDocumentUpdated()
        {
            if (AddinModel == null || Model?.ActiveEditor == null || Model.ActiveEditor.Identifier != "KavaDocsDocument")
                return;

            Model.ActiveEditor.Properties["KavaDocsUnEdited"] = false;
        }


        //public override void OnNotifyAddin(string command, object parameter)
        //{
        //    if (command == "ReadOnlyEditorDoubleClick")
        //    {
        //        Tree.OpenTopicInMMEditor(AddinModel.ActiveTopic);
        //    }
        //}


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

        public override string OnModifyPreviewHtml(string renderedHtml, string markdownHtml)
        {

            if (!markdownHtml.Contains("kavaDocs: true"))
                return renderedHtml;
            
            var topic = AddinModel.ActiveTopic;
            if (topic == null)
                return renderedHtml;

            topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Preview;
            topic.TopicState.IsPreview = true;

            if (topic.IsLink)
            {
                renderedHtml = topic.Body;
                if (string.IsNullOrEmpty(topic.Body) && topic.Link.StartsWith("http"))
                    renderedHtml = topic.Link;            
            }
            else
                renderedHtml = topic.RenderTopicToFile(addPragmaLines: true);

            topic.TopicState.IsPreview = false;
            topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Html;

            return base.OnModifyPreviewHtml(renderedHtml, markdownHtml);
        }

        

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
