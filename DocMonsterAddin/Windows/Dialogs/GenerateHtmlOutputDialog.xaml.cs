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

        public GenerateHtmlOutputDialog(DocProject project = null)
        {
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Owner = mmApp.Model.Window;
            Model = new GenerateHtmlModel();
            if (project != null)
                Model.Project = project;

            DataContext = Model;            
        }

        private async void Button_GenerateOutput(object sender, RoutedEventArgs e)
        {
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


        public bool IsComplete
        {
            get => _isComplete;
            set
            {
                if (value == _isComplete) return;
                _isComplete = value;
                OnPropertyChanged();             
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
        


        public GenerateHtmlModel()
        {
            BrowserUrl = $"http://localhost:{kavaUi.Configuration.WebServerPort}{kavaUi.Model.ActiveProject.Settings.RelativeBaseUrl}";
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
