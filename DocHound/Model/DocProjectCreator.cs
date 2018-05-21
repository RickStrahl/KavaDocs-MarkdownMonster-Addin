using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using DocHound.Annotations;
using DocHound.Utilities;
using Westwind.Utilities;

namespace DocHound.Model
{
    /// <summary>
    /// Use this class to create new DocProject.
    /// Set the properties then call CreateProject.
    /// </summary>
    public class DocProjectCreator : INotifyPropertyChanged
    {
        
        /// <summary>
        /// The descriptive name of this project
        /// By default this will also be the filename of the project (name.json)
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }
        private string _title;

        
        /// <summary>
        /// Folder where the new project will be created
        /// </summary>
        public string ProjectFolder
        {
            get { return _projectFolder; }
            set
            {
                if (value == _projectFolder) return;
                _projectFolder = value;
                OnPropertyChanged();
            }
        }
        private string _projectFolder;



        public string Filename
        {
            get { return _filename; }
            set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged();
            }
        }

        private string _filename;


        public string Company
        {
            get { return _company; }
            set
            {
                if (value == _company) return;
                _company = value;
                OnPropertyChanged();
            }
        }

        private string _company;

        
        /// <summary>
        /// Installation folder of the Application/Addin
        /// </summary>
        public string InstallFolder
        {
            get { return _installFolder; }
            set
            {
                if (value == _installFolder) return;
                _installFolder = value;
                OnPropertyChanged();
            }
        }
        private string _installFolder;


        public DocProjectCreator()
        {
            if (string.IsNullOrEmpty(InstallFolder))
                InstallFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }


        public bool IsTargetFolderMissingOrEmpty(string folder)
        {
            if (!Directory.Exists(folder))
                 return true;

            var files = Directory.GetFiles(folder);
            var dirs = Directory.GetDirectories(folder);
            if (files.Length > 0 || dirs.Length > 0)
                return false;

            return true;
        }

        /// <summary>
        /// Creates a new project structure in the ProjectFolder
        /// specified.
        /// </summary>        
        /// <returns></returns>
        public DocProject CreateProject()
        {
            if (string.IsNullOrEmpty(ProjectFolder))
            {
                SetError("Please provide a Project Folder.");
                return null;
            }

            if (string.IsNullOrEmpty(Title))
            {
                SetError("Please provide a Title for the new project.");
                return null;
            }

            if (!IsTargetFolderMissingOrEmpty(ProjectFolder))            
            {
                SetError("Couldn't create new project: Project exists already - please use another folder.");
                return null;                
            }

            string filename = "_toc.json";
            if (string.IsNullOrEmpty(filename))
                filename = Path.Combine(ProjectFolder, "_toc.json");
            else
                filename = Path.Combine(ProjectFolder, filename);

            var project = new DocProject()
            {
                Title = Title,
                Filename = filename,
                Owner = Company
            };

            if (!CopyProjectAssets(project))
                return null;

           
            string body = @"## Welcome to your new Documentation Project

Here are a few tips to get started:

* Press **ctrl-n** to create a new Topic
* Enter text into the main text area using [Markdown formatting](https://documentationmonster.west-wind.com)
* Use the Topic Editor on the right to enter topic data
* Use drag and drop in the Topic tree to move topics around
* Use the Image toolbar icon to select images from disk
* Paste images from from the clipboard into your text

Time to get going!
";
            var topic = new DocTopic(project)
            {
                Title = Title,
                Body = body,
                DisplayType = "index"
            };
            project.Topics.Add(topic);            
            project.SaveProject();

            return project;
        }

        public bool CopyProjectAssets(DocProject project)
        {
            try
            {
                string folder = project.ProjectDirectory;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string wwwFolder = Path.Combine(folder, "kavadocs");
                if (!Directory.Exists(wwwFolder))
                    Directory.CreateDirectory(wwwFolder);

                KavaUtils.CopyDirectory(Path.Combine(InstallFolder, "ProjectTemplates"), wwwFolder);
            }
            catch (Exception ex)
            {
                SetError("Failed to copy project assets: " + ex.GetBaseException().Message);
                return false;
            }

            return true;
        }

        #region Error Handling

        public string ErrorMessage { get; set; }
        
        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }
        #endregion

        #region InotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
