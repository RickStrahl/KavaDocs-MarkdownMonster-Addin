using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocMonster.Configuration;
using DocMonster.Model;
using DocMonster.Windows.Dialogs;
using DocMonsterAddin.WebServer;
using DocMonsterAddin.Windows.Dialogs;
using FluentFTP.Helpers;
using MarkdownMonster;
using MarkdownMonster.AddIns;
using Westwind.Utilities;

namespace DocMonsterAddin
{

    /// <summary>
    /// This class creates the KavaDocs main menu dynamically at runtime
    /// </summary>
    public class DocMonsterMenuHandler
    {
        public DocMonsterModel Model { get; set; }

        public MenuItem KavaDocsMenuItem { get; set; }

        public MenuItem ViewMenuItem { get; set; }

        public DocMonsterMenuHandler()
        {
            Model = kavaUi.Model;
        }
        public MenuItem CreateKavaDocsMainMenu()
        {
            var topMi = new MenuItem() {  Name = "MainMenuKavaDocsMenu", Header = "_Doc Monster" };
            topMi.DataContext = Model.TopicsTree.Model;

            var mi = new MenuItem()
            {
                Header = "_New Project",
                Command = Model.Commands.NewProjectCommand,
                InputGestureText = "Alt-K-N"
            };
            topMi.Items.Add(mi);

            mi = new MenuItem()
            {
                Header = "_Open Project",
                Command = Model.Commands.OpenProjectCommand,
                InputGestureText = "Alt-K-O"
            };
            topMi.Items.Add(mi);

            mi = new MenuItem()
            {
                Header = "Recent Projects",
                Name="MenuRecentItems",
                Command = Model.Commands.NewProjectCommand,
            };
            mi.SubmenuOpened += MenuRecentItems_SubmenuOpened;
            mi.Items.Add(new MenuItem());  // empty item so it pops open otherwise it won't
            topMi.Items.Add(mi);


            mi = new MenuItem()
            {
                Header = "_Close Project",
                Command = Model.Commands.CloseProjectCommand
            };
            topMi.Items.Add(mi);

            mi = new MenuItem()
            {
                Header = "_Save Project",
                Command = Model.Commands.SaveProjectCommand,
                InputGestureText = "Alt-K-S"
            };
            topMi.Items.Add(mi);

            topMi.Items.Add(new Separator());

            // *** Topic Submenu
            mi = new MenuItem()
            {
                Header = "Topic Operations",
            };
            topMi.Items.Add(mi);
         
            var mic = new MenuItem()
            {
                Header = "_Link to another Topic",
                Command = Model.Commands.LinkTopicDialogCommand,
                InputGestureText = "Alt-K"
            };
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "Import .NET Library",
                Command = Model.Commands.ImportDotnetLibraryCommand
            };
            mi.Items.Add(mic);

            mi = new MenuItem()
            {
                Header = "Topic Browser",
                InputGestureText = "Alt-T",
                Command = Model.Commands.TopicBrowserCommand
            };            
            topMi.Items.Add(mi);


            topMi.Items.Add(new Separator());
            
            mi = new MenuItem()
            {
                Header = "Preview using Topic Template",
                IsCheckable = true,
                IsChecked = Model.Configuration.TopicRenderMode == DocMonster.Configuration.TopicRenderingModes.TopicTemplate
            };
            mi.Unchecked += OnRenderModeToggled;
            mi.Checked += OnRenderModeToggled;
            topMi.Items.Add(mi);

            if (Model.ActiveProject != null)
            {
                if (SimpleHttpServer.Current == null)
                {
                    mi = new MenuItem()
                    {
                        Header = "Start Preview Web Server (stopped)",
                        Command = Model.Commands.StartPreviewWebServerCommand,
                        CommandParameter = "Start"
                    };
                    topMi.Items.Add(mi);
                }
                else
                {
                    mi = new MenuItem()
                    {
                        Header = "Stop Preview Web Server (running)",
                        Command = Model.Commands.StartPreviewWebServerCommand,
                        CommandParameter = "Stop"
                    };
                    topMi.Items.Add(mi);
                }            
            }

            topMi.Items.Add(new Separator());

            // *** Build SubMenu

            mi = new MenuItem()
            {
                Header = "Build and Publish",
            };
            topMi.Items.Add(mi);

            mic = new MenuItem()
            {
                Header = "_Build Project Html Output",
                Command = Model.Commands.BuildHtmlCommand
            };
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "_Publish Project",
                IsEnabled = true,
                Command = Model.Commands.PublishProjectCommand
            };
            mi.Items.Add(mic);


            mic = new MenuItem()
            {
                Header = "Update Output Scripts and Templates",
                Command = Model.Commands.UpdateScriptsAndTemplatesCommand,
                ToolTip = "Copies the scripts and styles from the templates folder into the 'wwwroot' output folder of the project."
            };
            mi.Items.Add(mic);

  


            // Settings Submenu
            mi = new MenuItem()
            {
                Header = "Settings",
            };
            topMi.Items.Add(mi);

            // Settings children
            mic = new MenuItem()
            {
                Header = "Project Settings",
            };
            mic.Click += MenuProjectSettings_Click;
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "Application Settings",
            };
            mic.Click += MenuDocumentationMonsterApplicationSettings;
            mi.Items.Add(mic);


            mi.Items.Add(new Separator());

            mic = new MenuItem()
            {
                Header = "Cleanup Project Folder"
            };
            mic.Click += CleanupProjectFolder_Click;
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "Open Project Folder in Explorer",
                Command = mmApp.Model.Commands.OpenInExplorerCommand,
                CommandParameter = Model.ActiveProject?.ProjectDirectory
            };
            
            mi.Items.Add(mic);

            mi.Items.Add(new Separator());

            mic = new MenuItem()
            {
                Header = "Backup Project",
                Command = kavaUi.Model.Commands.BackupProjectCommand
            };
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "Open Backup Folder",                
            };
            mic.Click += (s, e) =>
            {                
                var backupfolder = System.IO.Path.Combine(kavaUi.Configuration.DocumentsFolder, "Backups");                                
                if (Model.ActiveProject != null )
                {
                    var folderName = FileUtils.SafeFilename(Model.ActiveProject.Title);
                    backupfolder = System.IO.Path.Combine(backupfolder, folderName);
                }
                if(Directory.Exists(backupfolder))
                    ShellUtils.OpenFileInExplorer(backupfolder);
            };
            mi.Items.Add(mic);


            // insert Item after MainMenuEdit item on Main menu
            Model.Addin.AddMenuItem(topMi, "MainMenuTools", addMode: AddMenuItemModes.AddAfter );
            KavaDocsMenuItem = topMi;

            // Add Shortcut to main mane
            // ButtonShowFavorites MainMenuView

            var viewMenuItem = kavaUi.Model.Model.Window.ButtonShowFavorites;
            var viewMenu = kavaUi.Model.Model.Window.MainMenuView;

            mi = new MenuItem
            {
                Header = "Activate _Documentation Monster",
                InputGestureText = "Alt-V-K"
            };
            mi.Click += (s, e) =>
            {
                if (Model?.Window == null)
                    return;
                Model.Window.ShowLeftSidebar();                
                Model.Window.LeftSidebar.SelectTab("Documentation Monster");
            };

            viewMenu.Items.Insert(viewMenu.Items.IndexOf(viewMenuItem)+1, mi);


            return topMi;
        }

        private void OnRenderModeToggled(object s, RoutedEventArgs e)
        {
            Model.Configuration.TopicRenderMode = ((MenuItem) s).IsChecked
                ? TopicRenderingModes.TopicTemplate
                : TopicRenderingModes.MarkdownDefault;
        }

        public void RemoveMenu()
        {

        }



        private void MenuRecentItems_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            if (menu == null)
                return;

            menu.Items.Clear();

            Model.Configuration.CleanupRecentProjects();

            foreach (var recent in Model.Configuration.RecentProjects)
            {
                var header = recent.ProjectTitle;
                if (!string.IsNullOrEmpty(header))
                    header += $" ({FileUtils.GetCompactPath(recent.ProjectFile)})";
                else
                {
                    header = $"{System.IO.Path.GetFileNameWithoutExtension(recent.ProjectFile)} ({FileUtils.GetCompactPath(recent.ProjectFile)})";
                }

                var mi = new MenuItem()
                {
                    Header = header,
                    Command = Model.Commands.OpenRecentProjectCommand,
                    CommandParameter = recent
                };
                menu.Items.Add(mi);
            }
        }


        private void MenuProjectSettings_Click(object sender, RoutedEventArgs e)
        {
            var form = new ProjectSettingsDialog(kavaUi.MarkdownMonsterModel.Window);
            form.Show();
        }

        private void MenuDocumentationMonsterApplicationSettings(object sender, RoutedEventArgs e)
        {
            kavaUi.MarkdownMonsterModel.Window.OpenTab(System.IO.Path.Combine(kavaUi.MarkdownMonsterModel.Configuration.CommonFolder, "docmonsteraddin.json"));
        }


        private void CleanupProjectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Model.ActiveProject == null)
            {
                Model.Addin.ShowStatusError("There is no project to clean up.");
                return;
            }

            FileUtils.DeleteFiles(Model.ActiveProject.ProjectDirectory, "~*.*", true);
            Model.Addin.ShowStatus($"Temporary files have been deleted from the project folder.");


        }
    }
}
