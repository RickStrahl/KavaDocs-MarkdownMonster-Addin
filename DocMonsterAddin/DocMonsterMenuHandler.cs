using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocMonster.Configuration;
using DocMonster.Model;
using DocMonster.Windows.Dialogs;
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
            Model = kavaUi.AddinModel;
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
                Command = Model.Commands.LinkTopicDialogCommand
            };
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "Import .NET Library",
                Command = Model.Commands.ImportDotnetLibraryCommand
            };
            mi.Items.Add(mic);

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
                Header = "Update Output Scripts and Templates",
                Command = Model.Commands.UpdateScriptsAndTemplatesCommand,
                ToolTip = "Copies the scripts and styles from the templates folder into the 'wwwroot' output folder of the project."
            };
            mi.Items.Add(mic);

            mic = new MenuItem()
            {
                Header = "_Publish Project",
                IsEnabled = false
                //Command = Model.Commands.BuildHtmlCommand
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
                Header = "Kava Docs Settings",
            };
            mic.Click += MenuKavaDocsSettings_Click;
            mi.Items.Add(mic);


            // insert Item after MainMenuEdit item on Main menu
            Model.Addin.AddMenuItem(topMi, "MainMenuTools", addMode: AddMenuItemModes.AddAfter );
            KavaDocsMenuItem = topMi;

            // Add Shortcut to main mane
            // ButtonShowFavorites MainMenuView

            var viewMenuItem = kavaUi.AddinModel.Model.Window.ButtonShowFavorites;
            var viewMenu = kavaUi.AddinModel.Model.Window.MainMenuView;

            mi = new MenuItem
            {
                Header = "Activate _KavaDocs",
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

        private void MenuKavaDocsSettings_Click(object sender, RoutedEventArgs e)
        {
            kavaUi.MarkdownMonsterModel.Window.OpenTab(System.IO.Path.Combine(kavaUi.MarkdownMonsterModel.Configuration.CommonFolder, "KavaDocsAddin.json"));
        }
    }
}
