using System;
using System.Collections.Generic;
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
    public partial class TopicEditor : UserControl
    {
        private TopicEditorModel Model;

        public MarkdownDocumentEditor BodyEditor { get; set; }

        public MarkdownDocumentEditor ActiveMarkdownEditor => BodyEditor;

        #region Initialization

        public TopicEditor()
        {            
            InitializeComponent();
            

            Model = new TopicEditorModel();
            DataContext = Model;

            BodyEditor = new MarkdownDocumentEditor(TopicEditorBrowser);

            Loaded += TopicEditor_Loaded;            
        }


        private void TopicEditor_Loaded(object sender, RoutedEventArgs e)
        {
            BodyEditor.LoadDocument();
            kavaUi.AddinModel.PropertyChanged += ActiveTopic_PropertyChanged;
            TextTitle.Focus();
        }
        
        #endregion

        public void FocusEditor()
        {
            BodyEditor.SetEditorFocus();
        }

        private void ActiveTopic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AddinModel.ActiveTopic))
            {
                BodyEditor.SetMarkdown();                
            }
        }

    }
}
