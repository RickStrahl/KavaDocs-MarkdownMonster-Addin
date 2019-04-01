using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DocHound.Windows.Dialogs;
using Westwind.Utilities;

namespace KavaDocsAddin
{
    public class KavaDocsMenuHandler
    {
        public KavaDocsModel Model { get; set; }

        public MenuItem KavaDocsMenuItem { get; set; }

        public MenuItem ViewMenuItem { get; set; }

        public KavaDocsMenuHandler()
        {
            Model = kavaUi.AddinModel;
        }
        public MenuItem CreateKavaDocsMainMenu()
        {
            var topMi = new MenuItem() {  Name = "MainMenuKavaDocsMenu", Header = "_Kava Docs" };
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

            // Topic Submenu
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
            Model.Addin.AddMenuItem(topMi, "MainMenuTools",mode: 0);
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
                Model.Window.ShowLeftSidebar();
                Model.Window.SidebarContainer.SelectedItem = Model.Addin.KavaDocsTopicTreeTab;
            };

            viewMenu.Items.Insert(viewMenu.Items.IndexOf(viewMenuItem)+1, mi);


            return topMi;
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
