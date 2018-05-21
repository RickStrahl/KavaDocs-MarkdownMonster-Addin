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
using MarkdownMonster.Windows;

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
                MessageBox.Show($"Link has changed from {topic.TopicState.OldLink} to {topic.Link}");
            }


            if (project == null)
                project = kavaUi.AddinModel.ActiveProject;

            project.SaveProjectAsync();
            return true;            
        }
    }
}
