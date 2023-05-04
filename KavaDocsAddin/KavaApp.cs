using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Configuration;
using KavaDocsAddin;
using Westwind.Utilities.Configuration;

namespace KavaDocsAddin.Core.Configuration
{
    public class KavaApp
    {

        /// <summary>
        /// Global configuration object
        /// </summary>
        public static KavaDocsConfiguration Configuration { get; set; }

        /// <summary>
        /// Active Help Context ID (html file) used to access online documentation
        /// </summary>
        public static string ActiveHelpContext { get; set; }

        public static string ApplicationName { get; set; } = "Kava Docs Addin";


        static KavaApp()
        {
            Configuration = KavaDocsConfiguration.Current;

        }

    }
}
