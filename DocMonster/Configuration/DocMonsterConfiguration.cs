using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using MarkdownMonster;
using Westwind.Utilities.Configuration;

namespace DocMonster.Configuration
{
    public class DocMonsterConfiguration : BaseAddinConfiguration<DocMonsterConfiguration>, INotifyPropertyChanged
    {

        public static DocMonsterConfiguration Current { get; set; }


        static DocMonsterConfiguration()
        {
            Current = new DocMonsterConfiguration();
            Current.Initialize();
        }

        public DocMonsterConfiguration()
        {
            
            DocumentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Documentation Monster");         
            RecentProjects = new ObservableCollection<RecentProjectItem>();
            HomeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
        }

        protected override IConfigurationProvider OnCreateDefaultProvider(string sectionName, object configData)
        {
            var provider = new JsonFileConfigurationProvider<DocMonsterConfiguration>()
            {
                JsonConfigurationFile = Path.Combine(mmApp.Configuration.CommonFolder, "DocMonsterAddin.json")
            };

            return provider;
        }

        #region Static Configuration Options
        public static string AllowedTopicFileExtensions { get; } =
            ",.md,.markdown,.txt,.htm,.html";

        #endregion



        #region Folder Locations

        /// <summary>
        /// Folder from which the addin was started
        /// </summary>
        [JsonIgnore]
        public string HomeFolder { get;  }

        /// <summary>
        /// Generic Addins Folder location
        /// </summary>
        [JsonIgnore]
        public string AddinsFolder => Path.Combine(mmApp.Configuration.CommonFolder, "Addins");

        /// <summary>
        /// Last folder used when opening a document
        /// </summary>
        public string LastProjectFile { get; set; }

        /// <summary>
        /// Causes last project to be automatically opened when KavaDocs
        /// is started. If false empty project is shown.
        /// </summary>
        public bool OpenLastProject { get; set; }

        /// <summary>
        /// Determines whether the addin automatically loads itself
        /// when Mardown Monster starts. Default is false which means
        /// it loads only when you click on the addin icon.
        /// </summary>
        public bool AutoOpen { get; set; }

        /// <summary>
        /// If set to a non-zero value, automatically backs up the entire project
        /// to a backup folder, minus the wwwroot folder.
        ///
        /// The number specifies how many backups are created in a rolling backup.
        /// </summary>
        public int AutomaticBackupCount { get; set; } = 5;


        /// <summary>
        /// It true any topics that are edited are closed once you navigate
        /// off the topic in the topic browser. Creates more jumpy UI but
        /// keeps the document count simpler with less open files.
        /// </summary>
        public bool CloseTopicsOnDeselection { get; set; }

        /// <summary>
        /// Determines how topics are rendered either using the default Markdown Monster template
        /// or the Topic Template
        /// </summary>
        public TopicRenderingModes TopicRenderMode { get; set; } = TopicRenderingModes.MarkdownDefault;

        /// <summary>
        /// The preview WebServer port used for previews
        /// </summary>
        public int LocalWebServerPort { get; set; } = 5210;

        /// <summary>
        /// If true won't use the internal Web Server
        /// </summary>
        public bool DontStartInternalWebServer { get; set; }


        public string LastProjectCompany
        {
            get { return _LastProjectCompany; }
            set
            {
                if (value == _LastProjectCompany) return;
                _LastProjectCompany = value;
                OnPropertyChanged(nameof(LastProjectCompany));
            }
        }
        private string _LastProjectCompany;


        /// <summary>
        /// Last location where an image was opened.
        /// </summary>
        public string LastImageFolder { get; set; }

        public string DocumentsFolder { get; set; }

        [JsonIgnore]
        public string CommonFolder { get => mmApp.Configuration.CommonFolder; }
        
        
        public ObservableCollection<RecentProjectItem> RecentProjects
        {
            get { return _RecentProjects; }
            set
            {
                if (value == _RecentProjects) return;
                _RecentProjects = value;
                OnPropertyChanged(nameof(RecentProjects));
            }
        }
        private ObservableCollection<RecentProjectItem> _RecentProjects;

        #endregion





        //protected override IConfigurationProvider OnCreateDefaultProvider(string sectionName, object configData)
        //{
        //    var provider = new JsonFileConfigurationProvider<KavaDocsConfiguration>()
        //    {
        //        JsonConfigurationFile = Path.Combine(mmApp.Configuration.CommonFolder, "DocMonsterAddin.json")
        //    };

        //    if (!File.Exists(provider.JsonConfigurationFile))
        //    {
        //        if (!Directory.Exists(Path.GetDirectoryName(provider.JsonConfigurationFile)))
        //            Directory.CreateDirectory(Path.GetDirectoryName(provider.JsonConfigurationFile));
        //    }

        //    return provider;
        //}


        #region UI features
        /// <summary>
        /// Timeout for messages
        /// </summary>
        public int StatusMessageTimeout { get; set; } = 6000;

        #endregion

        #region Recent Projects List

        public void AddRecentProjectItem(string filename, string topicId = null, string projectTitle = null)
        {
            var recent = RecentProjects.FirstOrDefault(rec => rec.ProjectFile == filename);
            if (recent != null)
                RecentProjects.Remove(recent);
            else
                recent = new RecentProjectItem();

            recent.ProjectFile = filename;
            if (topicId != null)
                recent.LastTopicId = topicId;

            if (projectTitle != null)
                recent.ProjectTitle = projectTitle;

            RecentProjects.Insert(0, recent);
            Write();
        }

        public void CleanupRecentProjects()
        {
            // Remove missing projects
            var missing = new List<RecentProjectItem>();
            foreach (var recentProject in RecentProjects)
            {
                if (!File.Exists(recentProject.ProjectFile))
                    missing.Add(recentProject);
            }
            foreach (var recent in missing)
                RecentProjects.Remove(recent);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }        
    }



    public class BaseAddinConfiguration<T> : AppConfiguration
    {


    }

    public class RecentProjectItem
    {
        public string ProjectFile { get; set; }

        public string ProjectTitle { get; set; }
        public string LastTopicId { get; set; }
    }

    public enum TopicRenderingModes
    {
        TopicTemplate,
        MarkdownDefault
    }
}
