using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocHound.Model;
using MarkdownMonster;

namespace KavaDocsAddin.Controls
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
            Model.KavaDocsModel.ActiveTopic = topic;
            topic.TopicState.IsSelected = true;
        }

        #endregion

        
        private void AddinModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KavaDocsModel.ActiveTopic))
            {                                                                         
                //BodyEditor.Identifier = "KavaDocsDocument";
                //var topicId = Model.AppModel.ActiveTopic?.Id;
                //if (topicId != null)
                //    BodyEditor.Properties["TopicId"] = topicId;
                //else
                //{
                //    topicId = Model.AppModel.ActiveTopic?.Slug;
                //    BodyEditor.Properties["TopicSlug"] = topicId;
                //}
                //BodyEditor.SetMarkdown();                
            }
        }

        //private void TopicEditor_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    SaveProjectFileForTopic(Model.Topic,Model.Project);
        //    Model.AppModel.Window.ShowStatus("Topic saved.",2000);
        //    e.Handled = true;
        //}


        protected override void OnMouseLeave(MouseEventArgs e)
        {
            var pos = Mouse.GetPosition(this);
            if (pos.X > 5)
                return;
        
            if (SaveProjectFileForTopic(Model.Topic, Model.Project))
                Model.AppModel.Window.ShowStatus("Topic saved.", 2000);

            e.Handled = true;
        }

        public bool SaveProjectFileForTopic(DocTopic topic, DocProject project = null)
        {
            if (topic == null)
                return false;

            if (!topic.TopicState.IsDirty)
                return false;

            if (project == null)
                project = kavaUi.AddinModel.ActiveProject;

            project.SaveProjectAsync();
            return true;            
        }
    }
}
