

using System;
using System.ComponentModel;
using System.IO;
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

            this.
            // uses this file for storing settings in `%appdata%\Markdown Monster`
            // to persist settings call `KavaDocsAddinConfiguration.Current.Write()`
            // at any time or when the addin is shut down
            ConfigurationFilename = "KavaDocsAddin.json";
            
            DocumentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Documentation Monster");
        }

        #region Static Configuration Options
        public static string AllowedTopicFileExtensions { get; } =
            ",.md,.markdown,.txt,.htm,.html";

        #endregion

       

        #region Folder Locations
        /// <summary>
        /// Last folder used when opening a document
        /// </summary>
        public string LastProjectFile { get; set; }

        public bool OpenLastProject { get; set; }

        /// <summary>
        /// Last location where an image was opened.
        /// </summary>
        public string LastImageFolder { get; set; }

        public string DocumentsFolder { get; set; }

        public string CommonFolder { get => mmApp.Configuration.CommonFolder; }
        
        internal string AddinsFolder => Path.Combine(mmApp.Configuration.CommonFolder, "Addins");

        

        #endregion
    

        #region UI features
        /// <summary>
        /// Timeout for messages
        /// </summary>
        public int StatusMessageTimeout { get; set; } = 6000;

        #endregion

    }
}