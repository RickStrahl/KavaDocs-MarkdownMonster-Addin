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
using MahApps.Metro.Controls;
using MarkdownMonster;

namespace KavaDocsAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for PasteTopicBookmark.xaml
    /// </summary>
    public partial class PasteTopicBookmark : MetroWindow
    {

        public DocTopic SelectedTopic { get; set; }

        public bool Cancelled { get; set; } = true;

        public PasteTopicBookmark()
        {
            InitializeComponent();

            TopicPicker.TopicSelected += (selectedTopic) => { LinkSelected(); };

            TopicPicker.TextSearchText.Focus();

            TopicPicker.TreeTopicBrowser.SelectedItemChanged += TreeTopicBrowser_SelectedItemChanged;
        }

        private void TreeTopicBrowser_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedTopic = TopicPicker.SelectedTopic;
            if (SelectedTopic == null)
                return;

            var doc = new MarkdownDocument();
            doc.Load(SelectedTopic.GetTopicFileName());
            doc.RenderHtmlToFile();

            PreviewBrowser.Navigate(doc.HtmlRenderFilename);
        }

        private void Button_EmbedLink(object sender, RoutedEventArgs e)
        {
            LinkSelected();
        }

        public void LinkSelected()
        {
            SelectedTopic = TopicPicker.SelectedTopic;
            if (SelectedTopic == null)
                Cancelled = true;

            Cancelled = false;
            Close();
        }
    }    
}
