using System;
using System.IO;
using System.Windows;
using DocHound.Configuration;
using DocHound.Model;
using DocHound.Utilities;
using KavaDocsAddin;
using KavaDocsAddin.Core.Configuration;
using MarkdownMonster;
using MarkdownMonster.Utilities;
using MarkdownMonster.Windows;
using Westwind.Utilities;
using KavaDocsModel = KavaDocsAddin.KavaDocsModel;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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


        #region Loading/Unloading

        public NewProjectDialog(MainWindow window)
        {
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Owner = window;
            AppModel = kavaUi.AddinModel;
            Window = kavaUi.AddinModel.Window;
            

            ProjectCreator = new DocProjectCreator()
            {
                Owner = kavaUi.Configuration.LastProjectCompany
            };

            DataContext = this;

            Loaded += NewProjectDialog_Loaded;
            Closing += NewProjectDialog_Closing;
         
        }

        private void NewProjectDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ProjectCreator != null && !string.IsNullOrEmpty(ProjectCreator.Owner))
                AppModel.Configuration.LastProjectCompany = ProjectCreator.Owner;
        }

        private void NewProjectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            TextProjectTitle.Focus();
        }

        #endregion

        #region Main Operation Button Handlers

        public bool CreateProject(DocProjectCreator creator = null)
        {

            WindowUtilities.FixFocus(this, ButtonGetDirectory);

            if (creator == null)
                creator = ProjectCreator;

            if (!creator.IsTargetFolderMissingOrEmpty(creator.ProjectFolder))
            {
                string msg = $@"Your new Project Folder: 
                
{creator.ProjectFolder}

exists already. 

Kava Docs requires a new project folder. Please choose another folder for your new project or delete this folder and try again.
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
            {
                Close();
                if (!string.IsNullOrEmpty(ProjectCreator.Owner))
                    kavaUi.Configuration.LastProjectCompany = ProjectCreator.Owner;
            }
        }

        private void Button_ImportFromHelpBuilder(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json|" +
                         "All files (*.*)|*.*",
                CheckFileExists = true,
                RestoreDirectory = true,
                Multiselect = false,
                Title = "Open Help Builder JSON Export File"
            };

            var result = openFileDialog.ShowDialog(this);
            if (!result.Value)
                return;

            var inputJsonFile = openFileDialog.FileName;

            if (string.IsNullOrEmpty(ProjectCreator.ProjectFolder))
            {
                string initialPath = kavaUi.Configuration.DocumentsFolder;
                if (!string.IsNullOrEmpty(kavaUi.Configuration.LastProjectFile))
                    initialPath  = System.IO.Path.GetDirectoryName(kavaUi.Configuration.LastProjectFile);

                string selectedFolder = mmWindowsUtils.ShowFolderDialog(initialPath, "Select folder for new KavaDocs Project");

                if (string.IsNullOrEmpty(selectedFolder))
                    return;

                //var dlg = new CommonOpenFileDialog()
                //{
                //    Title = "Select folder for new KavaDocs Project",
                //    IsFolderPicker = true,
                //    RestoreDirectory = true,
                //    ShowPlacesList = true,
                //    Multiselect = false,
                //    EnsureValidNames = false,
                //    EnsureFileExists = false,
                //    EnsurePathExists = false
                //};

                //if (!string.IsNullOrEmpty(kavaUi.Configuration.LastProjectFile))
                //    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(kavaUi.Configuration.LastProjectFile);
                //else
                //    dlg.InitialDirectory = kavaUi.Configuration.DocumentsFolder;

                //var res = dlg.ShowDialog();

                //if (res != CommonFileDialogResult.Ok)
                //    return;

                ProjectCreator.ProjectFolder = selectedFolder;
            }

            

            if (Directory.Exists(ProjectCreator.ProjectFolder) && Directory.GetFiles(ProjectCreator.ProjectFolder).Length > 0)
            {
                if (MessageBox.Show(
                        "The output folder exists already. The folder to create a new project has to be empty, so either delete the folder or pick a different one.\r\n\r\n" +
                        "Do you want to continue and delete the existing folder?", "New Project Folder exists already",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;

                try
                {
                    Directory.Delete(ProjectCreator.ProjectFolder, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete output folder:\r\n{ex.Message}");
                    return;
                }
            }

            var importer = new HelpBuilder5JsonImporter
            {
                Title = ProjectCreator.Title,
                Owner = ProjectCreator.Owner
            };
            if (!importer.ImportHbp(inputJsonFile, ProjectCreator.ProjectFolder, KavaDocsConfiguration.Current.HomeFolder))
            {
                MessageBox.Show($"Couldn't create new project from import file.");
                return;
            }

            Close();

            if(!string.IsNullOrEmpty(ProjectCreator.Owner))
                kavaUi.Configuration.LastProjectCompany = ProjectCreator.Owner;

            kavaUi.AddinModel.OpenProject(System.IO.Path.Combine(ProjectCreator.ProjectFolder, "_toc.json"));
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Other Events

        private void TextProjectTitle_OnKeyUp(object sender, KeyEventArgs e)
        {

            if (string.IsNullOrEmpty(ProjectCreator.Title))
            {
                ProjectCreator.Filename = null;
                ProjectCreator.ProjectFolder = null;
            }
            else
            {

                ProjectCreator.Filename = FileUtils.CamelCaseSafeFilename(ProjectCreator.Title) + ".kavadocs";
                ProjectCreator.ProjectFolder = System.IO.Path.Combine(KavaApp.Configuration.DocumentsFolder,
                    FileUtils.SafeFilename(ProjectCreator.Title));
            }
        }

        private void ButtonGetDirectory_Click(object sender, RoutedEventArgs e)
        {
            string initialPath = kavaUi.Configuration.DocumentsFolder;
            if (!string.IsNullOrEmpty(kavaUi.Configuration.LastProjectFile))
                initialPath = System.IO.Path.GetDirectoryName(kavaUi.Configuration.LastProjectFile);

            string selectedFolder = mmWindowsUtils.ShowFolderDialog(initialPath, "Select folder to open in the Folder Browser");
            if (string.IsNullOrEmpty(selectedFolder))
                return;

            //var dlg = new CommonOpenFileDialog();

            //dlg.Title = "Select folder to open in the Folder Browser";
            //dlg.IsFolderPicker = true;
            //if (!string.IsNullOrEmpty(kavaUi.Configuration.LastProjectFile))
            //    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(kavaUi.Configuration.LastProjectFile);
            //else
            //    dlg.InitialDirectory = kavaUi.Configuration.DocumentsFolder;
            //dlg.RestoreDirectory = true;
            //dlg.ShowHiddenItems = true;
            //dlg.ShowPlacesList = true;
            //dlg.EnsurePathExists = false;

            //var result = dlg.ShowDialog();

            //if (result != CommonFileDialogResult.Ok)
            //    return;

            ProjectCreator.ProjectFolder = selectedFolder; // dlg.FileName;

        }
        #endregion
    }
}
