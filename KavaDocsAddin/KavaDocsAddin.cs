using System;
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

        public AddinModel AddinModel { get; set; }

       public TabItem KavaDocsTab { get; set; }

        public KavaDocsMenuHandler KavaDocsMenu { get; set; }

        public TopicsTree Tree { get; set; }

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
                    .ConvertFromString("pack://application:,,,/KavaDocsAddin;component/icon.png") as ImageSource;
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


        
        public override void OnWindowLoaded()
        {
            base.OnWindowLoaded();

            kavaUi.Addin = this;
            AddinModel = kavaUi.AddinModel;
            AddinModel.Addin = this;

            Configuration = kavaUi.Configuration;


            var tabItem = new TabItem() { Name="KavaDocsTree", Header = " Kava Docs " };
            KavaDocsTab = tabItem;

            Tree = new TopicsTree();            
            tabItem.Content = Tree;   
            
            Model.Window.AddSidebarPanelTabItem(tabItem);
      
            KavaDocsMenu = new KavaDocsMenuHandler();
            KavaDocsMenu.CreateKavaDocsMenu();

            //AddinModel.ActiveProject = DocProjectManager.Current.LoadProject(@"C:\Temp\markdownmonster_help\_toc.json");
            //Model.Window.Dispatcher.Delay(10, p => Tree.LoadProject(AddinModel.ActiveProject));
        }
    

        public override void OnAfterSaveDocument(MarkdownDocument doc)
        {
            base.OnAfterSaveDocument(doc);

            if (string.IsNullOrEmpty(doc.CurrentText) || !doc.Filename.EndsWith(DocTopic.KavaDocsEditorFilename))
                return;
            
            if (!doc.CurrentText.StartsWith("---") || !doc.CurrentText.Contains("kavaDocs: true"))
                return;


            AddinModel.ActiveProject.UpdateTopicFromMarkdown(doc,AddinModel.ActiveTopic);
        }

        public override void OnExecute(object sender)
        {
            //MessageBox.Show("Hello from your Kava Docs Addin", "Kava Docs",
            //    MessageBoxButton.OK, MessageBoxImage.Information);


            Model.Window.SidebarContainer.SelectedItem = KavaDocsTab;

            var project = DocProjectManager.Current.LoadProject(@"C:\Temp\markdownmonster_help\_toc.json");
            
            //var topic = project.LoadBySlug("Using-the-Editor");

            var model = kavaUi.AddinModel;
            model.ActiveProject = project;
            //model.ActiveTopic = topic;

            //this.PreviewTopic();


            // *** Some things you can do:

            // // modify selected editor text
            //var text = GetSelection();
            //text = "<small>" + text + "</small>";
            //SetSelection(text);
            //RefreshPreview();

            // // open a new tab with a file
            //OpenTab(Path.Combine(mmApp.Configuration.CommonFolder, "KavaDocsAddin.json"));

            // // run a process
            //var imageFile = GetSelection();  // assume image file is selected
            //if (!imageFile.Contains(":\\"))
            //    imageFile = Path.Combine(Path.GetDirectoryName(ActiveDocument.Filename), imageFile);
            //Process.Start("paint.exe", imageFile);
        }

        public override void OnExecuteConfiguration(object sender)
        {
            MessageBox.Show("Configuration for our sample Addin", "Markdown Addin Sample",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public override bool OnCanExecute(object sender)
        {
            return true;
        }


        #region Previewing

        public override string OnModifyPreviewHtml(string renderedHtml, string markdownHtml)
        {

            if (!markdownHtml.Contains("kavaDocs: true"))
                return renderedHtml;
            
            var topic = AddinModel.ActiveTopic;
            if (topic == null)
                return renderedHtml;

            topic.Project.ActiveRenderMode = HtmlRenderModes.Preview;
            topic.TopicState.IsPreview = true;

            renderedHtml = topic.RenderTopicToFile(addPragmaLines: true);

            topic.TopicState.IsPreview = false;
            topic.Project.ActiveRenderMode = HtmlRenderModes.Html;

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


            string html = null;
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
