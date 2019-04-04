using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocHound.Annotations;
using DocHound.Model;
using MarkdownMonster;

namespace KavaDocsAddin.Controls
{
    public class TopicEditorModel : INotifyPropertyChanged
    {
        

        public KavaDocsModel KavaDocsModel
        {
            get { return _appModel; }
            set
            {
                if (Equals(value, _appModel)) return;
                _appModel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Topic));
                OnPropertyChanged(nameof(Project));
            }
        }
        private KavaDocsModel _appModel;

        public AppModel AppModel
        {
            get; set;
        }

        public DocTopic Topic
        {
            get { return KavaDocsModel.ActiveTopic; }
        }

     
        public DocProject Project
        {
            get { return KavaDocsModel.ActiveProject; }
        }


        
        public List<DisplayTypeItem> DisplayTypesList
        {
            get
            {
                if (Project == null)
                    return null;

                var list = new List<DisplayTypeItem>();

                foreach (var type in Project.ProjectSettings.TopicTypes)
                {
                    var item = new DisplayTypeItem()
                    {
                        DisplayType = type.Key                        
                    };
                    list.Add(item);
                }
                return list;
            }
        }

        #region Display Behavior Only Properties


        public bool IsClassPanelVisible
        {
            get
            {
                if (KavaDocsModel.ActiveTopic == null)
                    return false;
                var type = KavaDocsModel.ActiveTopic.DisplayType;
                if (type.StartsWith("class", StringComparison.InvariantCultureIgnoreCase))
                    return true;

                return false;
            }
        }

        public bool IsClass
        {
            get
            {
                if (KavaDocsModel.ActiveTopic == null)
                    return false;
                var type = KavaDocsModel.ActiveTopic.DisplayType;
                if (type ==  "classheader" || type == "database" || type== "webservice")
                    return true;

                return false;
            }
        }

        public bool IsMethod
        {
            get
            {
                if (KavaDocsModel.ActiveTopic == null)
                    return false;
                var type = KavaDocsModel.ActiveTopic.DisplayType;
                if (type == "classmethod" || type == "classevent")
                    return true;

                return false;
            }
        }

        public bool IsProperty
        {
            get
            {
                if (KavaDocsModel.ActiveTopic == null)
                    return false;
                var type = KavaDocsModel.ActiveTopic.DisplayType;
                if (type == "classproperty" || type == "classfield" || type == "databasefield")
                    return true;

                return false;
            }
        }


        #endregion


        public TopicEditorModel()
        {
            KavaDocsModel = kavaUi.AddinModel;
            AppModel = kavaUi.MarkdownMonsterModel;

            KavaDocsModel.PropertyChanged += KavaDocsModel_PropertyChanged;
        }

        private void KavaDocsModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KavaDocsModel.ActiveTopic))
            {
                OnPropertyChanged(nameof(Topic));
                //OnPropertyChanged(nameof(DisplayTypesList));
            }
            if (e.PropertyName == nameof(KavaDocsModel.ActiveProject))
            {
                OnPropertyChanged(nameof(Project));
                OnPropertyChanged(nameof(Topic));
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            if (propertyName == nameof(Topic))
            {
                OnPropertyChanged(nameof(IsClassPanelVisible));
                OnPropertyChanged(nameof(IsClass));
                OnPropertyChanged(nameof(IsMethod));
                OnPropertyChanged(nameof(IsProperty));
            }
        }

        #endregion
    }

    [DebuggerDisplay("{DisplayType}")]
    public class DisplayTypeItem
    {
        public string DisplayType { get; set; }

        public string ImageFile
        {
            get
            {
                if (DisplayType == null || kavaUi.AddinModel.ActiveProject == null)
                    return null;

                return Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory, "_kavadocs", "icons", DisplayType + ".png");
            }
        }

    }
}
