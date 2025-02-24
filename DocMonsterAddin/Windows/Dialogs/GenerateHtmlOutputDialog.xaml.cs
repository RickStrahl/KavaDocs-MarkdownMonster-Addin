using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using DocMonsterAddin.WebServer;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Utilities;
using MarkdownMonster.Windows;
using Westwind.Utilities;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for GenereHtmlOutputDialog.xaml
    /// </summary>
    public partial class GenerateHtmlOutputDialog : MetroWindow
    {
        public GenerateHtmlModel Model { get; }

        StatusBarHelper Status { get; set; }


        public GenerateHtmlOutputDialog(DocProject project = null)
        {
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Owner = mmApp.Model.Window;
            Model = new GenerateHtmlModel();
            if (project != null)
                Model.Project = project;

            Status = new StatusBarHelper(StatusText, StatusIcon);

            DataContext = Model;            
        }

        
        private async void Button_GenerateOutput(object sender, RoutedEventArgs e)
        {

            try
            {
                Status.ShowStatusProgress("Generating Html output...");


                Model.IsComplete = false;
                await Task.Run(() =>
                {
                    Model.IsComplete = false;
                    Model.IsRunning = true;
                    var output = new HtmlOutputGenerator(Model.Project);
                    output.Generate();
                    Model.IsRunning = false;
                    Model.IsComplete = true;
                });
            }
            catch(Exception ex)
            {
                Status.ShowStatusError("Html output generation failed: " + ex.Message);
            }
            finally
            {
               Status.ShowStatusSuccess("Html output has been generated.");
            }

            if (Model.OpenInBrowser)
            {
                if(!Model.DontStartInternalWebServer)
                    Model.Model.Addin.StartWebServer();

                ShellUtils.GoUrl(Model.BrowserUrl);
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

        private void Button_PublishClick(object sender, RoutedEventArgs e)
        {
            var publishDialog = new PublishDialog(Model.Project);
            publishDialog.Show();

            Close();
        }
    }

    public class GenerateHtmlModel : INotifyPropertyChanged
    {
        private DocProject _project = kavaUi.Model.ActiveProject;

        public DocMonsterModel Model = kavaUi.Model;

        public DocProject Project
        {
            get => _project;
            set
            {
                if (Equals(value, _project)) return;
                _project = value;
                OnPropertyChanged();
                
            }
        }

        private bool _openInBrowser = true;

        public bool OpenInBrowser
        {
            get => _openInBrowser;
            set
            {
                if (value == _openInBrowser) return;
                _openInBrowser = value;
                OnPropertyChanged();
            }
        }

        public bool OpenFolder
        {
            get => _openFolder;
            set
            {
                if (value == _openFolder) return;
                _openFolder = value;
                OnPropertyChanged();
            }
        }

        private bool _openFolder;


        public string BrowserUrl
        {
            get => _browserUrl;
            set
            {
                if (value == _browserUrl) return;
                _browserUrl = value;
                OnPropertyChanged();
            }
        }
        private string _browserUrl;

        
        public bool DontStartInternalWebServer
        {
            get => Model.Configuration.DontStartInternalWebServer;
            set => Model.Configuration.DontStartInternalWebServer = value;
        }


        public bool IsComplete
        {
            get => _isComplete;
            set
            {
                if (value == _isComplete) return;
                _isComplete = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowStartWebServer));
            }
        }
        private bool _isComplete;


        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (value == _isRunning) return;
                _isRunning = value;
                OnPropertyChanged();
            }
        }
        private bool _isRunning;

        private bool _showStartWebServer;

        public bool ShowStartWebServer
        {
            get {
                if (!IsComplete ||
                    Model?.ActiveProject == null ||
                    SimpleHttpServer.Current != null &&
                    SimpleHttpServer.Current.IsRunning)
                    return false;

                return true;
            }
            set
            {
                if (value == _showStartWebServer) return;
                _showStartWebServer = value;
                OnPropertyChanged();
            }
        }


        public GenerateHtmlModel()
        {
            BrowserUrl = $"http://localhost:{kavaUi.Configuration.LocalWebServerPort}{kavaUi.Model.ActiveProject.Settings.RelativeBaseUrl}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
