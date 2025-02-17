using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DocMonster;
using DocMonster.Configuration;
using DocMonster.Model;
using DocMonster.Utilities;
using DocMonster.Windows.Dialogs;
using DocMonsterAddin.Core.Configuration;
using DocMonsterAddin.Windows.Dialogs;
using DocMonsterAddin.Controls;
using MarkdownMonster;
using MarkdownMonster.Windows;
using Microsoft.Win32;
using Westwind.Utilities;
using DocMonsterAddin.WebServer;
using KavaDocsAddinControls;

namespace DocMonsterAddin
{
    public class AppCommands
    {
        private DocMonsterModel Model;
        

        public AppCommands(DocMonsterModel model)
        {
            Model = model;
            CreateCommands();
        }

        

        private void CreateCommands()
        {
            Command_OpenProject();
            Command_OpenFolder();
            Command_SaveProject();
            Command_NewProject();
            Command_CloseProject();

            // Topic
            Command_NewTopic();
            Command_DeleteTopic();
            Command_RefreshTree();
            Command_ImportDotnetLibrary();

            Command_OpenTopicFileExplicitly();
            Command_OpenRecentProject();



            // Generic Edit Toolbar Insertion features
            Command_ToolbarInsertMarkdown();

            // Tools
            Command_Settings();
            Command_ProjectSettings();
            Command_StartPreviewWebServer();
            Command_BackupProject();


            // Shell
            Command_UpdateScriptsAndTemplates();

            // Views
            Command_PreviewBrowser();
            Command_CloseRightSidebarCommand();
            Command_OpenInWebBrowser();

            // Editing
            Command_LinkTopicDialog();

            // Build
            Command_BuildHtml();
            Command_PublishProject();
        }

        #region File Commands

        public CommandBase OpenProjectCommand { get; set; }
        
        private void Command_OpenProject()
        {
            OpenProjectCommand = new CommandBase((parameter, command) =>
            {
                var fd = new OpenFileDialog
                {
                    DefaultExt = ".md",
                    Filter = "Markdown files (*.kava,*.json)|*.kava;*.json|" +
                             "All files (*.*)|*.*",
                    CheckFileExists = true,
                    RestoreDirectory = true,
                    Multiselect = false,
                    Title = "Open Kava Docs Project"
                };

                if (!string.IsNullOrEmpty(dmApp.Configuration.LastProjectFile))
                    fd.InitialDirectory = dmApp.Configuration.LastProjectFile;

                bool? res = null;
                try
                {
                    res = fd.ShowDialog();
                }
                catch (Exception ex)
                {
                    mmApp.Log("Unable to open file.", ex);
                    MessageBox.Show(
                        $@"Unable to open file:\r\n\r\n" + ex.Message,
                        "An error occurred trying to open a file",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (res == null || !res.Value)
                    return;

                Model.OpenProject(fd.FileName);

            }, (parameter, command) =>
            {
                return true;
            });
        }


        public CommandBase OpenFolderCommand { get; set; }

        void Command_OpenFolder()
        {
            OpenFolderCommand = new CommandBase((parameter, command) =>
            {
                if (Model.ActiveProject?.Filename == null)
                    return;

                Model.Window.ShowFolderBrowser(folder: Model.ActiveProject.ProjectDirectory );
            }, (p, c) => Model.ActiveProject != null);
        }




        public CommandBase OpenProjectSettingsCommand { get; set; }

        void Command_OpenProjectSettings()
        {
            OpenProjectSettingsCommand = new CommandBase((parameter, command) =>
                {
                    var form = new ProjectSettingsDialog(Model.Model.Window);
                    form.Show();
                },
                (p, c) => Model.ActiveProject != null);
        }

        public CommandBase SaveProjectCommand { get; set; }

        public void Command_SaveProject()
        {
            SaveProjectCommand = new CommandBase(async (parameter, command) =>
            {
                if (Model.ActiveMarkdownEditor != null && Model.ActiveProject != null)
                {

                    await Model.ActiveMarkdownEditor.GetMarkdown();    
                    Model.ActiveProject.SaveProject();
                    Model.Addin.RefreshPreview();
                }

                Model.Window.ShowStatus("Project saved.", dmApp.Configuration.StatusMessageTimeout);
            });
        }

        public CommandBase NewProjectCommand { get; set; }

        public void Command_NewProject()
        {
            NewProjectCommand = new CommandBase((parameter, command) =>
            {                                
                var dialog = new NewProjectDialog(Model.Window);
                dialog.ShowDialog();                
            });
        }


        public CommandBase CloseProjectCommand { get; set; }

        public void Command_CloseProject()
        {
            CloseProjectCommand = new CommandBase((parameter, command) =>
            {
                string projectName = Model.ActiveProject.Title;
                Model.ActiveProject.SaveProject();
                Model.ActiveProject = null;

                Model.TopicsTree.LoadProject(null);

                Model.Window.ShowStatus("Project " + projectName + " has been closed.",
                    dmApp.Configuration.StatusMessageTimeout);
            });
        }


        public CommandBase BackupProjectCommand { get; set; }

        void Command_BackupProject()
        {
            BackupProjectCommand = new CommandBase((parameter, command) =>
            {
                if (Model.ActiveProject == null)
                {
                    mmApp.Window.ShowStatusError("No project found to backup.");
                    return;
                }

                var folderName = FileUtils.SafeFilename(Model.ActiveProject.Title);
                 var backupfolder = Path.Combine(kavaUi.Configuration.DocumentsFolder, "Backups", folderName, "Backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm")) ;
                if (!Directory.Exists(backupfolder))
                    Directory.CreateDirectory(backupfolder);

                FileUtils.CopyDirectory(Model.ActiveProject.ProjectDirectory, backupfolder, recursive: true);

                mmApp.Window.ShowStatusSuccess("Project backed up to: " + backupfolder,15_000);

            }, (p, c) => true);
        }


        #endregion

        #region Markdown Editor Commands

        public CommandBase ToolbarInsertMarkdownCommand { get; set; }

        public void Command_ToolbarInsertMarkdown()
        {
            ToolbarInsertMarkdownCommand = new CommandBase((parameter, command) =>
            {
                string action = parameter as string;
                Model.ActiveMarkdownEditor?.ProcessEditorUpdateCommand(action);
            });
        }
        #endregion

        #region Tool Commands
        public CommandBase SettingsCommand { get; set; }

        public void Command_Settings()
        {
            SettingsCommand = new CommandBase((parameter, command) =>
            {
                ShellUtils.GoUrl(Path.Combine(mmApp.Configuration.CommonFolder, "docmonsteraddin.json"));
            });
        }


        public CommandBase ProjectSettingsCommand { get; set; }

        void Command_ProjectSettings()
        {
            ProjectSettingsCommand = new CommandBase((parameter, command) =>
                {
                    Model.Window.OpenTab(Model.ActiveProject.Filename,rebindTabHeaders:true);
                });
        }


        public CommandBase StartPreviewWebServerCommand { get; set; }

        void Command_StartPreviewWebServer()
        {
            StartPreviewWebServerCommand = new CommandBase((parameter, command) =>
            {
                string mode = parameter as string;
                if ( mode.Equals("Start", StringComparison.OrdinalIgnoreCase))
                {
                    Model.Addin.StartWebServer();                    
                }
                else
                {
                    Model.Addin.StopWebServer();                    
                }

            }, (p, c) => true);
        }


        #endregion



        #region Topic Commands

        public CommandBase NewTopicCommand { get; set; }

        public void Command_NewTopic()
        {
            NewTopicCommand = new CommandBase((parameter, command) =>
            {
                var newTopic = new NewTopicDialog(Model.Window);
                newTopic.ShowDialog();
            });
        }


        



        public CommandBase DeleteTopicCommand { get; set; }

        public void Command_DeleteTopic()
        {
            DeleteTopicCommand = new CommandBase( async (parameter, command) =>
            {
                if (!mmApp.Model.IsEditorActive)
                    return;

                var topic = Model.ActiveTopic;
                if (topic == null)
                    return;
                if (MessageBox.Show($"You are about to delete topic\r\n\r\n{topic.Title}\r\n\r\nAre you sure?",
                        "Delete Topic",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.No)
                    return;


                var parentTopics = topic.Parent?.Topics;
                if (parentTopics == null)
                    parentTopics = Model.ActiveProject.Topics;
                int topicIndex = -1;
                if (parentTopics != null)
                    topicIndex = parentTopics.IndexOf(topic);

                topic.IsExpanded = false;

                mmApp.Window.ShowStatusProgress("Deleting topic...");
                await Task.Delay(40);

              
                await mmApp.Window.Dispatcher.InvokeAsync(() =>
                {
                    Model.ActiveProject.DeleteTopic(topic);
                });

                mmApp.Window.ShowStatus();

                Model.ActiveProject.SaveProjectAsync();

                var parent = topic.Parent;

                DocTopic newTopic = null;
                if (parent != null)
                {
                    if (topicIndex < 1)
                        newTopic = parent; //Model.TopicsTree.SelectTopic(parent);
                    else
                        newTopic = parent.Topics[topicIndex - 1]; // Model.TopicsTree.SelectTopic(parent.Topics[topicIndex - 1]);
                }
                // root topic / project
                else
                {
                    if (topicIndex > -0)
                        newTopic = parentTopics[topicIndex - 1];  // Model.TopicsTree.SelectTopic(parentTopics[topicIndex - 1]);
                }

                if (newTopic != null)
                {
                    Model.TopicsTree.HandleSelection(newTopic, dontSaveProject: true);
         
                }
            });
        }


        public CommandBase OpenTopicFileExplicitlyCommand { get; set; }

        void Command_OpenTopicFileExplicitly()
        {
            OpenTopicFileExplicitlyCommand = new CommandBase((parameter, command) =>
            {
                var topic = Model.ActiveTopic;
                if (topic == null)
                    return;

                Model.Window.OpenTab(topic.GetTopicFileName(),readOnly: false);
            });
        }


        


        public CommandBase RefreshTreeCommand { get; set; }

        void Command_RefreshTree()
        {
            RefreshTreeCommand = new CommandBase((parameter, command) =>
                {
                    Model.TopicsTree.LoadProject(Model.ActiveProject);
                });
        }




        public CommandBase OpenRecentProjectCommand { get; set; }

        void Command_OpenRecentProject()
        {
            OpenRecentProjectCommand = new CommandBase((parameter, command) =>
            {
                var recent = parameter as RecentProjectItem;
                if (recent == null)
                    return;

                Model.OpenProject(recent.ProjectFile);
            });
        }


        public CommandBase ImportDotnetLibraryCommand { get; set; }

        void Command_ImportDotnetLibrary()
        {
            ImportDotnetLibraryCommand = new CommandBase((parameter, command) =>
            {
                var dlg = new ImportDotnetLibraryDialog();
                dlg.Show();

            }, (p, c) => true);
        }


        #endregion

        #region View Commands


        public CommandBase CloseRightSidebarCommand { get; set; }

        void Command_CloseRightSidebarCommand()
        {
            CloseRightSidebarCommand= new CommandBase((parameter, command) =>
            {
                Model.Window.ShowRightSidebar(true);
            });
        }



        public CommandBase PreviewBrowserCommand { get; set; }

        public void Command_PreviewBrowser()
        {
            PreviewBrowserCommand = new CommandBase((parameter, command) =>
            {
                Model.Window.ShowPreviewBrowser(!mmApp.Configuration.IsPreviewVisible);
                Model.PreviewTopic(false);
                
                
            });
        }


        public CommandBase OpenTopicInWebBrowserCommand { get; set; }

        void Command_OpenInWebBrowser()
        {
            OpenTopicInWebBrowserCommand = new CommandBase((parameter, command) =>
            {
                var settings = Model.ActiveProject.Settings;

                if (Model.ActiveTopic == null) return;

                if (string.IsNullOrEmpty(settings.WebSiteBaseUrl))
                {
                    mmApp.Window.ShowStatusError("Please configure your Web Site Base Url in project settings...");
                    return;
                }

                var url = Model.ActiveProject.Settings.WebSiteBaseUrl +
                          Model.ActiveProject.Settings.RelativeBaseUrl.TrimStart('/') +
                          Model.ActiveTopic.Slug + ".html";

                ShellUtils.GoUrl(url);
            }, (p, c) => true);
        }

        #endregion

        #region Editing
        public CommandBase LinkTopicDialogCommand { get; set; }

        void Command_LinkTopicDialog()
        {
            LinkTopicDialogCommand = new CommandBase((parameter, command) =>
            {
                var window = new TopicLinkDialog();
                window.Show();


                //var form = new PasteTopicBookmark();
                //form.Owner = mmApp.Model.Window;
                //form.ShowDialog();

                //if (!form.Cancelled && form.SelectedTopic != null && mmApp.Model.ActiveEditor != null)
                //{
                //    var selText = await mmApp.Model.ActiveEditor.GetSelection();
                //    var link = form.SelectedTopic.Link;
                //    if (string.IsNullOrEmpty(link))
                //        link = form.SelectedTopic.Slug;

                //    string origLink = link;

                //    if (!File.Exists(link))
                //    {
                //        if (!link.EndsWith(".md"))
                //            link += ".md";

                //        if (!File.Exists(link))
                //        {
                //            if (!link.EndsWith(".html"))
                //                link += ".html";
                //            if (!File.Exists(link))
                //                link = origLink;
                //        }
                //    }

                //    if (string.IsNullOrEmpty(selText))
                //        selText = form.SelectedTopic.Title;

                //    string md = $"[{selText}]({link})";

                //    await mmApp.Model.ActiveEditor.SetSelectionAndFocus(md);
                //}
            });
        }

        #endregion

        #region Build Operations

        public CommandBase BuildHtmlCommand { get; set; }

        void Command_BuildHtml()
        {
            BuildHtmlCommand = new CommandBase((parameter, command) =>
            {
                var window = new GenerateHtmlOutputDialog(Model.ActiveProject);
                window.Show();

                //Task.Run(() =>
                //{
                    //var project = kavaUi.Model.ActiveProject;
                    //var output = new HtmlOutputGenerator(project);
                    //output.Generate();

                    //mmApp.Model.Window.Dispatcher.Invoke(
                    //    () =>
                    //    {
                    //        //ShellUtils.OpenFileInExplorer(project.OutputDirectory);                           
                    //        var url = $"http://localhost:{kavaUi.Configuration.WebServerPort}{kavaUi.Model.ActiveProject.Settings.RelativeBaseUrl}";                            
                    //        ShellUtils.GoUrl(url);
                    //        mmApp.Model.Window.ShowStatusSuccess("Project output has been generated.");
                    //    });
                //});                            
            }, (p, c) => true);
        }


        public CommandBase PublishProjectCommand { get; set; }

        void Command_PublishProject()
        {
            PublishProjectCommand = new CommandBase((parameter, command) =>
            {
                var window = new PublishDialog(Model.ActiveProject);
                window.Show();

            }, (p, c) => true);
        }

        public CommandBase UpdateScriptsAndTemplatesCommand { get; set; }

        void Command_UpdateScriptsAndTemplates()
        {
            UpdateScriptsAndTemplatesCommand = new CommandBase((parameter, command) =>
            {
                mmApp.Model.Window.ShowStatusProgress("Copying scripts and templates...");

                var generator = new HtmlOutputGenerator(kavaUi.Model.ActiveProject);
                generator.CopyScriptsAndTemplates();


                mmApp.Model.Window.ShowStatusSuccess("Scripts and Templates copied.");
            }, (p, c) => true);
        }


        #endregion

    }
}
