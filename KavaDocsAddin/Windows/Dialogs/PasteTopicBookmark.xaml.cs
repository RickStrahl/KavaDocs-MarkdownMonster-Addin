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
