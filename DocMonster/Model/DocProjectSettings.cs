using System.Collections.Generic;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocMonster.Model
{
    /// <summary>
    /// Client project settings for a KavaDocs project
    /// </summary>
    public class DocProjectSettings
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
        public Dictionary<string, string> TopicTypes { get; set;  }
        

        public bool AutoSortTopics { get; set;  }
        
        /// <summary>
        /// If true stores Yaml information in each topic
        /// </summary>
        public bool StoreYamlInTopics { get; set; }

        public bool RenderProjectTitle { get; set; }
                
        public bool ShowEstimatedReadingTime { get; set;  }

        public bool DontAllowNestedTopicBodyScripts { get; set;  }

    }
}
