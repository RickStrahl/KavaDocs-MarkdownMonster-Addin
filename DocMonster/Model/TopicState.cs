using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DocMonster.Annotations;


namespace DocMonster.Model
{

    public class TopicState : INotifyPropertyChanged
    {
        private DocTopic Topic;
        

        public TopicState(DocTopic topic)
        {
            Topic = topic;
            BodyState = new BodyState();
        }

        public BodyState BodyState { get; set; }
       
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
               _isSelected = value;
                OnPropertyChanged();
            }
        }
        private bool _isSelected;


        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (value == _isHidden) return;
                _isHidden = value;
                OnPropertyChanged();
            }
        }
        private bool _isHidden;


        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (value == _isEditing) return;
                _isEditing = value;
                OnPropertyChanged();
            }
        }
        private bool _isEditing;


        public bool NoAutoSave
        {
            get;set;
        }
        
        public bool IsDirty
        {
            get { return _IsDirty; }
            set
            {
                if (value == _IsDirty) return;
                _IsDirty = value;
                OnPropertyChanged(nameof(IsDirty));
            }
        }
        private bool _IsDirty = false;

      
        public bool IsPreview { get; set; }

        /// <summary>
        /// If Slug is changed the old value is stored here
        /// </summary>
        public string OldSlug { get; set; }

        /// <summary>
        /// If Link is changed the old value is stored here
        /// so we can detect a change
        /// </summary>
        public string OldLink { get; set; }


        public string ImageFilename
        {
            get
            {
                string outfolder = Topic.Project.ProjectDirectory;

                if (string.IsNullOrEmpty(outfolder))
                    return null;

                var type = Topic.DisplayType;
                if (type == null)
                {
                    if (Topic.Topics != null && Topic.Topics.Count > 0)
                        type = "header";
                    else
                        type = "topic";
                }
                
                return Path.Combine(outfolder, "_kavadocs", "icons", type.ToLower() + ".png");
            }
        }

        public string OpenImageFilename
        {
            get
            {
                string outfolder = Topic.Project.ProjectDirectory;

                if (string.IsNullOrEmpty(outfolder))
                    return null;

                var type = Topic.DisplayType;
                if (type == null)
                {
                    if (Topic.Topics != null && Topic.Topics.Count > 0)
                        type = "header";
                    else
                        type = "topic";
                }
                if( type == "header" || type== "index")
                    return Path.Combine(outfolder, "_kavadocs", "icons", type.ToLower() + "_open.png");

                return Path.Combine(outfolder, "_kavadocs", "icons", type.ToLower() + ".png");
            }
        }

        public bool IsToc { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BodyState
    {
        public string OriginalText { get; set; }

        public bool IsDirty { get; set; }
    }
}
