

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using MarkdownMonster;
using MarkdownMonster.AddIns;
using Westwind.Utilities.Configuration;

namespace DocHound.Configuration
{
    public class KavaDocsConfiguration : BaseAddinConfiguration<KavaDocsConfiguration>
    {

        public KavaDocsConfiguration()
        {
            ConfigurationFilename = "KavaDocsAddin.json";
            DocumentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kava Docs");         
            RecentProjects = new ObservableCollection<RecentProjectItem>();
            HomeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        #region Static Configuration Options
        public static string AllowedTopicFileExtensions { get; } =
            ",.md,.markdown,.txt,.htm,.html";

        #endregion

       

        #region Folder Locations

        /// <summary>
        /// Folder from which the addin was started
        /// </summary>
        public string HomeFolder { get;  }

        /// <summary>
        /// Generic Addins Folder location
        /// </summary>
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
        /// Last location where an image was opened.
        /// </summary>
        public string LastImageFolder { get; set; }

        public string DocumentsFolder { get; set; }

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
        //        JsonConfigurationFile = Path.Combine(mmApp.Configuration.CommonFolder, "KavaDocsAddin.json")
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

    }

    public class RecentProjectItem
    {
        public string ProjectFile { get; set; }

        public string ProjectTitle { get; set; }
        public string LastTopicId { get; set; }
    }
}