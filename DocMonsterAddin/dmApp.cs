using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Configuration;
using DocMonsterAddin;
using MarkdownMonster;
using Westwind.Utilities.Configuration;

namespace DocMonsterAddin.Core.Configuration
{
    public class dmApp
    {

        /// <summary>
        /// Global configuration object
        /// </summary>
        public static DocMonsterConfiguration Configuration { get; set; }

        /// <summary>
        /// Active Help Context ID (html file) used to access online documentation
        /// </summary>
        public static string ActiveHelpContext { get; set; }

        public static string ApplicationName { get; set; } = "Documentation Monster Addin";

        public static DocMonsterModel Model { get; set; }

        public static AppModel AppModel { get; set;  }
        static dmApp()
        {
            Configuration = DocMonsterConfiguration.Current;
            AppModel = mmApp.Model;
            Model = DocMonsterAddin.Current.DocMonsterModel;
        }

    }
}
