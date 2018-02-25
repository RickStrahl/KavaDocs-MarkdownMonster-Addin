using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DocHound.Annotations;


namespace DocHound.Model
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


        public bool IsPreview { get; set; }


        public string ImageFilename
        {
            get
            {
                string outfolder = Topic.Project.OutputDirectory;

                if (string.IsNullOrEmpty(Topic.Project.OutputDirectory))
                    return null;

                return Path.Combine(Topic.Project.OutputDirectory, "icons", Topic.Type.ToLower() + ".png");
            }
        }


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