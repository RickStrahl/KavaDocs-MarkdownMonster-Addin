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
using System.Windows.Shapes;
using DocMonster;
using DocMonster.Annotations;
using DocMonster.Model;
using DocMonster.Utilities;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Windows;

namespace DocMonsterAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for PublishDialog.xaml
    /// </summary>
    public partial class PublishDialog : MetroWindow
    {
        public PublishDialogModel Model { get; }

        public PublishDialog(DocProject project = null)
        {
            Owner = mmApp.Model.Window;
            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);
            
            Model = new PublishDialogModel(project ?? kavaUi.Model.ActiveProject);
            DataContext = Model;
        }

        private void Button_Publish(object sender, RoutedEventArgs e)
        {
            var publish = new FtpPublisher(Model.Project);
            var result  = publish.UploadProject();

            if (result)
            {
                Model.Window.ShowStatusSuccess("Project published.");
                return;
            }

            DocProjectManager.Current.SaveProject(Model.Project, Model.Project.Filename);

            WindowsNotifications.ShowInAppNotifications("Publishing failed", publish.ErrorMessage,
                icon: WindowsNotifications.NotificationInAppTypes.Warning, window: this);
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
            mmApp.Model.Window.Activate();
        }
    }

    public class PublishDialogModel
    {
        public PublishDialogModel(DocProject project)
        {
            Project = project;


        }

        public DocProject Project { get; set; }

        public MainWindow Window  => mmApp.Model.Window;
    }
}
