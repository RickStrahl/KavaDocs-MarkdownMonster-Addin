using System.IO;
using System.Windows;
using System.Windows.Input;
using DocMonster.Model;
using MarkdownMonster;
using MarkdownMonster.Windows;

namespace DocMonsterAddin.Controls
{
    /// <summary>
    /// Interaction logic for TopicEditor.xaml
    /// </summary>
    public partial class TopicEditor
    {
        private TopicEditorModel Model;

        public MarkdownDocumentEditor ActiveMarkdownEditor => mmApp.Model.ActiveEditor;

        #region Initialization

        public TopicEditor()
        {            
            InitializeComponent();
            Model = new TopicEditorModel();
            DataContext = Model;
            Loaded += TopicEditor_Loaded;               
        }

        private void TopicEditor_Loaded(object sender, RoutedEventArgs e)
        {            
            //kavaUi.AddinModel.PropertyChanged += AddinModel_PropertyChanged;            
        }

        public void LoadTopic(DocTopic topic)
        {
            Model.DocMonsterModel.ActiveTopic = topic;
            topic.TopicState.IsSelected = true;
        }

        #endregion

        
        protected override void OnMouseLeave(MouseEventArgs e)
        {            
            var pos = Mouse.GetPosition(this);
            if (pos.X > 5)
                return;

            WindowUtilities.FixFocus(Model.AppModel.Window, TextSortOrder);

            if (SaveProjectFileForTopic(Model.Topic, Model.Project))
                Model.AppModel.Window.ShowStatus("Topic saved.", 3000);

            e.Handled = true;
        }

        public bool SaveProjectFileForTopic(DocTopic topic, DocProject project = null)
        {
            if (topic == null)
                return false;

            if (!topic.TopicState.IsDirty)
                return false;

            if (!string.IsNullOrEmpty(topic.TopicState.OldLink) && topic.TopicState.OldLink != topic.Link)
            {
                if (MessageBox.Show(
                        $@"Link has changed from {topic.TopicState.OldLink} to {
                                topic.Link
                            }.\r\n\rnDo you want to fix up the link and file?",
                        "Topic Link Changed",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var oldFile = topic.GetTopicFileName(topic.TopicState.OldLink);
                    topic.SaveTopicFile(); // save new file
                    File.Delete(oldFile);  
                }

            }


            if (project == null)
                project = kavaUi.Model.ActiveProject;

            project.SaveProjectAsync();
            return true;            
        }

        private void ButtonTopicId_Click(object sender, RoutedEventArgs e)
        {
            ClipboardHelper.SetText(Model.Topic.Id);
            mmApp.Model.Window.ShowStatusSuccess("Topic Id copied to clipboard.");
        }
    }
}
