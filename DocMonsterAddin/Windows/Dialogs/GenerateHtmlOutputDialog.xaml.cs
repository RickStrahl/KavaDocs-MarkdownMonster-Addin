using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using DocMonster.Model;
using DocMonster.Utilities;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Utilities;
using Westwind.Utilities;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for GenereHtmlOutputDialog.xaml
    /// </summary>
    public partial class GenerateHtmlOutputDialog : MetroWindow
    {
        public GenerateHtmlModel Model { get; }

        public GenerateHtmlOutputDialog(Window owner)
        {
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Owner = owner;
            Model = new GenerateHtmlModel();

            DataContext = Model;            
        }

        private async void Button_GenerateOutput(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                var output = new HtmlOutputGenerator(Model.Project);
                output.Generate();

               
            });

            if (Model.OpenInBrowser)
            {
                ShellUtils.GoUrl(Model.BrowserUrl);
                mmApp.Model.Window.ShowStatusSuccess("Project output has been generated.");
            }
            if (Model.OpenFolder)
            {
                ShellUtils.OpenFileInExplorer(Model.Project.OutputDirectory);
            }
                
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;

            mmApp.Model.Window.Activate();
        }
    }

    public class GenerateHtmlModel
    {
        public DocProject Project { get; set; } = kavaUi.Model.ActiveProject;

        public bool OpenInBrowser { get; set; } = true;
        public bool OpenFolder { get; set; }

        public string BrowserUrl { get; set;  }

        public GenerateHtmlModel()
        {
            BrowserUrl = $"http://localhost:{kavaUi.Configuration.WebServerPort}{kavaUi.Model.ActiveProject.Settings.RelativeBaseUrl}";
        }
    }
}
