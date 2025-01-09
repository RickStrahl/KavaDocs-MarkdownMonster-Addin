using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Configuration;
using DocMonsterAddin;
using Westwind.Utilities.Configuration;

namespace DocMonsterAddin.Core.Configuration
{
    public class KavaApp
    {

        /// <summary>
        /// Global configuration object
        /// </summary>
        public static DocMonsterConfiguration Configuration { get; set; }

        /// <summary>
        /// Active Help Context ID (html file) used to access online documentation
        /// </summary>
        public static string ActiveHelpContext { get; set; }

        public static string ApplicationName { get; set; } = "Kava Docs Addin";


        static KavaApp()
        {
            Configuration = DocMonsterConfiguration.Current;

        }

    }
}
