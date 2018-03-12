using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocHound.Configuration;
using DocHound.Model;
using DocHound.Utilities;
using KavaDocsAddin;
using KavaDocsAddin.Core.Configuration;
using MahApps.Metro.Controls;
using MarkdownMonster;
using Microsoft.WindowsAPICodePack.Dialogs;
using Westwind.Utilities;
using KavaDocsModel = KavaDocsAddin.KavaDocsModel;

namespace DocHound.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class NewProjectDialog 
    {
        public DocProjectCreator ProjectCreator { get; set; }

        public KavaDocsModel AppModel { get; set; } 

        public MainWindow Window { get; set; } 
        

        public NewProjectDialog(MainWindow window)
        {
            InitializeComponent();
            
            Owner = window;            
            AppModel = kavaUi.AddinModel;
            Window = kavaUi.AddinModel.Window;            
            
            ProjectCreator = new DocProjectCreator();
            DataContext = this;

            Loaded += NewProjectDialog_Loaded;
        }

        private void NewProjectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            TextProjectTitle.Focus();
        }

        public bool CreateProject(DocProjectCreator creator = null)
        {
            if (creator == null)
                creator = ProjectCreator;
            
            if (!creator.IsTargetFolderMissingOrEmpty(creator.ProjectFolder))
            {
                string msg = $@"Your new Project Folder: 
                
{creator.ProjectFolder}

exists already. 

Documentation Monster requires a new project folder. Please choose another folder for your new project or delete this folder and try again.
";
                MessageBox.Show(msg, "New Project Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var project = creator.CreateProject();
            if (project != null)
            {
                // TODO: Need to figure out how to open
                kavaUi.AddinModel.OpenProject(project.Filename);

                Window.ShowStatus($"New Project '{project.Title}' has been created.",
                    KavaApp.Configuration.StatusMessageTimeout);
                return true;
            }

            MessageBox.Show($"New Project wasn't created:\r\n\r\n{creator.ErrorMessage}",
                "New Project Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);


            return false;
        }


        private void Button_CreateProjectClick(object sender, RoutedEventArgs e)
        {
            if (CreateProject())            
                Close();            
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextProjectTitle_OnKeyUp(object sender, KeyEventArgs e)
        {

            if (string.IsNullOrEmpty(ProjectCreator.Title))
            {
                ProjectCreator.Filename = null;
                ProjectCreator.ProjectFolder = null;
            }
            else
            {

                ProjectCreator.Filename = FileUtils.CamelCaseSafeFilename(ProjectCreator.Title) + ".dmjson";
                ProjectCreator.ProjectFolder = System.IO.Path.Combine(KavaApp.Configuration.DocumentsFolder,                    
                    FileUtils.SafeFilename(ProjectCreator.Title));
            }
        }

        private void ButtonGetDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();

            dlg.Title = "Select folder to open in the Folder Browser";
            dlg.IsFolderPicker = true;
            if (!string.IsNullOrEmpty(kavaUi.Configuration.LastProjectFile))
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(kavaUi.Configuration.LastProjectFile);
            else
                dlg.InitialDirectory = kavaUi.Configuration.DocumentsFolder;
            dlg.RestoreDirectory = true;
            dlg.ShowHiddenItems = true;
            dlg.ShowPlacesList = true;            
            dlg.EnsurePathExists = false;

            var result = dlg.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
                return;

            ProjectCreator.ProjectFolder = dlg.FileName;

        }
    }
}
