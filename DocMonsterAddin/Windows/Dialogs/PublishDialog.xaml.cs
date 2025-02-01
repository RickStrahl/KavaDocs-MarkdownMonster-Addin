using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using DocMonster;
using DocMonster.Annotations;
using DocMonster.Model;
using DocMonster.Utilities;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Windows;
using Westwind.Utilities;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for PublishDialog.xaml
    /// </summary>
    public partial class PublishDialog : MetroWindow
    {
        public PublishDialogModel Model { get; }

        StatusBarHelper Status { get;  }

        public PublishDialog(DocProject project = null)
        {
            Owner = mmApp.Model.Window;
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);
            
            Model = new PublishDialogModel(project ?? kavaUi.Model.ActiveProject);
            DataContext = Model;
            Status = new StatusBarHelper(StatusText, StatusIcon);
            
        }

        private async void Button_Publish(object sender, RoutedEventArgs e)
        {
            var publish = new FtpPublisher(Model.Project);
            Model.FtpPublisher = publish;
            publish.StatusUpdate = (status) =>
            {               
                Dispatcher.Invoke(() =>
                {
                    if (status.MessageType == UploadMessageTypes.Progress && status.BytesSent > 0)
                    {
                        decimal percent = (decimal)status.BytesSent / (decimal)status.TotalBytes * 100.01m;
                        StatusText2.Text = $"{status.SourceFileInfo.Name}:  {status.FilesSent} of {status.TotalFiles} sent. {percent:n0}%";
                    }
                    else
                    {
                        StatusText2.Text = status.Message;
                    }
                });

                return true;

            };

            DocProjectManager.Current.SaveProject(Model.Project, Model.Project.Filename);

            Status.ShowStatusProgress("Publishing to Ftp Server");

            bool result = false;
            try
            {
                Model.IsUploading = true;
                result = await publish.UploadProjectAsync();
            }
            catch(Exception ex)
            {
                Status.ShowStatusError("Project publishing failed." + ex.GetBaseException().Message);
                return;
            }
            finally
            {
                Model.IsUploading = false;
                StatusText2.Text = string.Empty;
            }
            
            if (result)
            {
                Status.ShowStatusSuccess("Project published.");                
                return;
            }
            Status.ShowStatusError("Project publishing failed.");
            


            WindowsNotifications.ShowInAppNotifications("Publishing failed", publish.ErrorMessage,
                icon: WindowsNotifications.NotificationInAppTypes.Warning, window: this);
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
            mmApp.Model.Window.Activate();
        }

        private void ButtonCancelUpload_Click(object sender, RoutedEventArgs e)
        {
            Model.FtpPublisher.IsCancelled = true;
        }

        private void TextBlock_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void WebSiteUrl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(Model.Project?.Settings?.Upload?.WebSiteUrl))
                ShellUtils.GoUrl(Model.Project.Settings.Upload.WebSiteUrl);
        }
    }

    public class PublishDialogModel : INotifyPropertyChanged
    {
        public PublishDialogModel(DocProject project)
        {
            Project = project;


       }

        public DocProject Project { get; set; }

        public FtpPublisher FtpPublisher { get; set; }

        public MainWindow Window  => mmApp.Model.Window;
        

        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                if (value == _isUploading) return;
                _isUploading = value;
                OnPropertyChanged();
            }
        }
        private bool _isUploading;


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
