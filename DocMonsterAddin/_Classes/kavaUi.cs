using System.IO;
using System.Reflection;
using System.Windows;
using DocMonster.Configuration;
using DocMonsterAddin.Core.Configuration;
using MarkdownMonster;

namespace DocMonsterAddin
{
    public class kavaUi
    {
        /// <summary>
        /// This addin's model
        /// </summary>
        public static DocMonsterModel AddinModel { get; set; }

                
        /// <summary>
        /// MarkdownMonster Model
        /// </summary>
        public static AppModel MarkdownMonsterModel { get; set; }

        /// <summary>
        /// Instance of the Addin
        /// </summary>
        public static DocMonsterAddin Addin { get; set; }

        public static DocMonsterConfiguration Configuration { get; set; }

        static kavaUi()
        {
            MarkdownMonsterModel = mmApp.Model;
            AddinModel = new DocMonsterModel(mmApp.Model.Window);
            Configuration = KavaApp.Configuration;
            
        }

        public static void NotImplementedDialog(string title = null, string additionalText = null)
        {
            if (string.IsNullOrEmpty(title))
                title = KavaApp.ApplicationName;


            if (!string.IsNullOrEmpty(additionalText))
                additionalText = "\r\n\r\n" + additionalText;


            MessageBox.Show("This feature is not implemented yet." + additionalText,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        }

    }
}
