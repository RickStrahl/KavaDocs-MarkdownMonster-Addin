using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        

        public string TopicType
        {
            get
            {                
                return Topic?.BodyFormat.ToString();               
            }
            set
            {
                if (value == _topicType) return;
                _topicType = value;
                OnPropertyChanged();

                if (Topic == null)
                    return;

                TopicBodyFormats format; 
                Enum.TryParse<TopicBodyFormats>(value,out format);
                Topic.BodyFormat = format;
            }
        }
        private string _topicType;

        public DocProject Project
        {
            get { return KavaDocsModel.ActiveProject; }
        }

        public TopicEditorModel()
        {
            KavaDocsModel = kavaUi.AddinModel;
            AppModel = kavaUi.MarkdownMonsterModel;

            KavaDocsModel.PropertyChanged += KavaDocsModel_PropertyChanged;
        }

        private void KavaDocsModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveTopic")
            {
                OnPropertyChanged(nameof(Topic));
                OnPropertyChanged(nameof(TopicTypesList));
            }
            if (e.PropertyName == "ActiveProject")
            {
                OnPropertyChanged(nameof(Project));
                OnPropertyChanged(nameof(Topic));
            }
        }

        public List<TopicTypeListItem> TopicTypesList
        {
            get
            {
                if (Project == null)
                    return null;

                var list = new List<TopicTypeListItem>();

                foreach (var type in Project.TopicTypes)
                {
                    var item = new TopicTypeListItem()
                    {
                        Type = type.Key
                    };
                    list.Add(item);
                }
                return list;
            }
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class TopicTypeListItem
    {

        public string Type { get; set; }

        public string ImageFile
        {
            get
            {
                if (Type == null || kavaUi.AddinModel.ActiveProject == null)
                    return null;

                return Path.Combine(kavaUi.AddinModel.ActiveProject.ProjectDirectory, "wwwroot", "icons", Type + ".png");
            }
        }

    }
}
