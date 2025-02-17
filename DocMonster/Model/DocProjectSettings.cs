using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocMonster.Configuration;
using DocMonster.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Westwind.Utilities;

namespace DocMonster.Model
{
    /// <summary>
    /// Client project settings for a KavaDocs project
    /// </summary>
    public class DocProjectSettings : INotifyPropertyChanged
    {
        private readonly DocProject _project;

        public DocProjectSettings(DocProject project)
        {
            _project = project;
            if (TopicTypes == null)
            {
                TopicTypes = new Dictionary<string, string>
                {
                    {"index", "Top level topic for the documentation."},
                    {"header", "Header topic for sub-topics"},
                    {"topic", "Generic topic"},
                    {"whatsnew", "What's new"},
                    {"weblink", "External link"},
                    {"classheader", "Class Header"},
                    {"interface", "Interface"},
                    {"namespace", "Namespace"},
                    {"classmethod", "Class method"},
                    {"classproperty", "Class property"},
                    {"classfield", "Class field"},
                    {"classevent", "Class event"},
                    {"classconstructor", "Class constructor"},
                    {"enum", "Enumeration"},
                    {"delegate", "Delegate"},
                    {"webservice", "Web Service"},
                    {"database", "Database"},
                    {"datacolumn", "Data columns"},
                    {"datafunction", "Data function"},
                    {"datastoredproc", "Data stored procedure"},
                    {"datatable", "Data table"},
                    {"dataview", "Data view"}
                };
            }
        }

        /// <summary>
        /// Determines how HTML is rendered when rendering topics
        /// </summary>
        [JsonIgnore]
        [DefaultValue(HtmlRenderModes.Html)]
        public HtmlRenderModes ActiveRenderMode { get; set; }


        /// <summary>
        /// Configured Topic Types that can be used with this project
        /// </summary>        
        public Dictionary<string, string> TopicTypes { get; set; }


        public bool AutoSortTopics { get; set; }

        /// <summary>
        /// If true stores Yaml information in each topic
        /// </summary>
        public bool StoreYamlInTopics { get; set; }

        public bool RenderProjectTitle { get; set; }

        public bool ShowEstimatedReadingTime { get; set; }

        public bool DontAllowNestedTopicBodyScripts { get; set; }

   



        /// <summary>
        /// The Web Site's base URL to navigate to the home page.
        /// This will be an online URL like https://docs.west-wind.com.
        /// 
        /// If there's a subfolder, specify it in RelativeBaseUrl.
        /// 
        ///
        /// Url will auto-terminate with a trailing slash
        /// </summary>
        public string WebSiteBaseUrl
        {
            get => _webSiteBaseUrl;
            set
            {
                if (value == _webSiteBaseUrl) return;
                _webSiteBaseUrl = value;
                if (!string.IsNullOrEmpty(_webSiteBaseUrl))
                    _webSiteBaseUrl = StringUtils.TerminateString(_webSiteBaseUrl, "/");
                OnPropertyChanged();
            }
        }
        private string _webSiteBaseUrl;





        /// <summary>
        /// The project's relative base Url. Typically this is "/"
        /// If you need something different set it in this property
        /// for example it's common to use "/docs/".
        ///
        /// Path will be auto-terminated with "/"
        /// </summary>
        public string RelativeBaseUrl
        {
            get { return _relativeBaseUrl; }
            set
            {
                if (value == _relativeBaseUrl) return;
                _relativeBaseUrl = value;
                if (!string.IsNullOrEmpty(_relativeBaseUrl))
                    _relativeBaseUrl = StringUtils.TerminateString(RelativeBaseUrl, "/");
                else
                    _relativeBaseUrl = "/";

                OnPropertyChanged();
            }
        }
        private string _relativeBaseUrl = "/";

        public UploadSettings Upload { get; set; } = new UploadSettings();  


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

    public class UploadSettings : INotifyPropertyChanged
    {
        public string Hostname { get; set; }
        
        public string UploadFtpPath
        {
            get => _uploadFtpPath;
            set
            {
                if (value == _uploadFtpPath) return;
                value = StringUtils.TerminateString(value, "/");
                _uploadFtpPath = value;
                OnPropertyChanged();
            }
        }
        private string _uploadFtpPath;

        /// <summary>
        /// The Web Site Root Url - Should be be the base of the
        /// site. 
        /// </summary>
        public string WebSiteUrl { get; set; }

        public bool OpenWebSite { get; set;  } 

        public string Username { get; set; }

        [JsonIgnore]
        public string Password { get; set; }

        public bool UseTls { get; set; }

        public bool DeleteExtraFiles { get; set; }

        public override string ToString()
        {
            return $"{Hostname}";
        }


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
}
