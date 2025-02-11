using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using MarkdownMonster;
using Westwind.Utilities;

namespace DocMonsterAddin
{
    public class TreeviewContextMenuHandler
    {
        public DocMonsterModel Model { get; set; }

        public MenuItem KavaDocsMenuItem { get; set; }

        public MenuItem ViewMenuItem { get; set; }

        public TreeviewContextMenuHandler()
        {
            Model = kavaUi.Model;
        }

        public ContextMenu CreateTreeviewContextMenu()
        {
            var ctxMenu = new ContextMenu() { Name = "TopicContextMenu" };
            ctxMenu.DataContext = Model.TopicsTree.Model;

            var mi = new MenuItem
            {
                Name = "MenuNewTopic",
                Header = "_New Topic",
                InputGestureText = "ctrl-n",
                Command = Model.Commands.NewTopicCommand
            };
            ctxMenu.Items.Add(mi);

            mi = new MenuItem
            {
                Name = "MenuDeleteTopic",
                Header = "Delete Topic",
                InputGestureText = "del",
                Command = Model.Commands.DeleteTopicCommand
            };
            ctxMenu.Items.Add(mi);

            ctxMenu.Items.Add(new Separator());

            mi = new MenuItem
            {
                Name = "MenuOpenExternalTopicFile",
                Header = "Open Topic File Explicitly",
                Command = Model.Commands.OpenTopicFileExplicitlyCommand
            };
            ctxMenu.Items.Add(mi);

            mi = new MenuItem
            {
                Name = "MenuOpenExternalTopicFile",
                Header = "Open in Explorer",
            };
            mi.Click += (s, e) =>
            {
                var file = Model.ActiveTopic.GetTopicFileName();
                if (File.Exists(file))
                    ShellUtils.OpenFileInExplorer(file);
            };
            ctxMenu.Items.Add(mi);



            ctxMenu.Items.Add(new Separator());

            // *** Template Submenu

            mi = new MenuItem
            {
                Name = "MenuTemplates",
                Header = "Templates",                
            };
            ctxMenu.Items.Add(mi);

            var sub = new MenuItem
            {
                Header = "Edit Topic Template",                
            };
            sub.Click += (s, e) =>
            {
                var path = Path.Combine(Model.ActiveProject.ProjectDirectory, $"_kavadocs\\Themes\\{Model.ActiveTopic?.DisplayType}.html");
                if (!File.Exists(path))
                    ShellUtils.OpenFileInExplorer(path);
                else
                    ShellUtils.ShellExecute(path, "EDIT");
            };
            mi.Items.Add(sub);

            sub = new MenuItem
            {
                Header = "Edit Layout Template (__layout.cshtml)",
                CommandParameter = "_layout.cshtml"
            };
            sub.Click += On_OpenStaticScriptFile;
            mi.Items.Add(sub);

            mi.Items.Add(new Separator());

            sub = new MenuItem
            {
                Header = "Edit Project CSS (kavadocs.css)",
                CommandParameter = "kavadocs.css"
            };
            sub.Click += On_OpenStaticScriptFile;            
            mi.Items.Add(sub);

            
            sub = new MenuItem
            {
                Header = "Edit Project Script (kavadocs.js)",
                CommandParameter = "scripts\\kavadocs.js"
            };
            sub.Click += On_OpenStaticScriptFile;

            mi.Items.Add(new Separator());

            sub = new MenuItem
            {
                Header = "Open Project Template Folder",
                CommandParameter = ""
            };
            sub.Click += On_OpenStaticScriptFile;
            mi.Items.Add(sub);

            mi.Items.Add(new Separator());

            sub = new MenuItem
            {
                Header = "Re-generate Project with Updated Templates",
                Command = Model.Commands.BuildHtmlCommand
            };            
            mi.Items.Add(sub);

            sub = new MenuItem
            {
                Header = "Update Scripts and Templates",
                Command = Model.Commands.UpdateScriptsAndTemplatesCommand,
                ToolTip = "Updates scripts, css, templates, template images, icons from the template into the output folder."
            };
            mi.Items.Add(sub);

            ctxMenu.Items.Add(new Separator());

            // *** Import
            mi = new MenuItem
            {               
                Header = "Import",
            };
            ctxMenu.Items.Add(mi);

            sub = new MenuItem()
            {
                Header = "Import .NET Library",
                Command = Model.Commands.ImportDotnetLibraryCommand
            };
            mi.Items.Add(sub);

            // *** Misc

            if (Model.ActiveTopic != null)
            {
                mi = new MenuItem
                {
                    Header = "Show File in Explorer",
                    Command = mmApp.Model.Commands.OpenInExplorerCommand,
                    CommandParameter = Path.Combine(Model.ActiveTopic.Project.ProjectDirectory, Model.ActiveTopic.Link)
                };
                ctxMenu.Items.Add(mi);
            }

            mi = new MenuItem
            {
                Name = "MenuRefresh",
                Header = "Reload Tree",
                //Command = Model.Commands.ReloadTreeCommand
            };
            ctxMenu.Items.Add(mi);

            return ctxMenu;
        }

        private void On_OpenStaticScriptFile(object sender, System.Windows.RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var relativeFile = mi.CommandParameter as string;
            var file = Path.Combine(
                Model.ActiveProject.ProjectDirectory,
                "_kavadocs\\Themes\\",
                relativeFile);

            if (File.Exists(file))
                ShellUtils.GoUrl(file);

            if (Directory.Exists(file))
                ShellUtils.OpenFileInExplorer(file);
            
                      
        }

    }

}
