using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DocHound.Configuration;
using KavaDocsAddin.Core.Configuration;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MarkdownMonster;

namespace KavaDocsAddin
{
    public class kavaUi
    {
        /// <summary>
        /// This addin's model
        /// </summary>
        public static AddinModel AddinModel { get; set; }

                
        /// <summary>
        /// MarkdownMonster Model
        /// </summary>
        public static AppModel MarkdownMonsterModel { get; set; }

        /// <summary>
        /// Instance of the Addin
        /// </summary>
        public static KavaDocsAddin Addin { get; set; }


        public static KavaDocsConfiguration Configuration { get; set; }


        static kavaUi()
        {
            MarkdownMonsterModel = mmApp.Model;
            AddinModel = new AddinModel(mmApp.Model.Window);
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
