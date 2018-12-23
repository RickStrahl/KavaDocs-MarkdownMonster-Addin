using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.RazorHosting;
using Westwind.Utilities;

namespace DocHound.Razor
{

    /// <summary>
    /// Renders string templates
    /// </summary>
    public class RazorStringTemplates
    {
        
        public RazorStringHostContainer<KavaDocsTemplate> RazorHost { get; set; }

        public string RenderTemplate(string templateText, object model, out string error)
        {            
            error = null;

            string result = RazorHost.RenderTemplate(templateText, model);
            if (result == null)
                result =
                    "<h3>Template Rendering Error</h3>\r\n<hr/>\r\n"  + 
                    "<pre>" + HtmlUtils.HtmlEncode(RazorHost.ErrorMessage) + "</pre>";

            return result;
        }

        public void StartRazorHost()
        {        
            StopRazorHost();

            var host = new RazorStringHostContainer<KavaDocsTemplate>()
            {                
                // *** Path to the Assembly path of your application
                BaseBinaryFolder = Environment.CurrentDirectory
            };

            // Add any assemblies that are referenced in your templates
            host.AddAssemblyFromType(typeof(RazorTemplates));
            host.AddAssemblyFromType(typeof(StringUtils));

            host.ReferencedNamespaces.Add("Doc");            
            host.ReferencedNamespaces.Add("DocumentationMonster.Core.Model");
            host.ReferencedNamespaces.Add("DocumentationMonster.Core.Utilities");
            host.ReferencedNamespaces.Add("DocumentationMonster.Core.Configuration");
            host.ReferencedNamespaces.Add("Westwind.Utilities");            

            // Always must start the host
            host.Start();

            RazorHost = host;
        }

        public void StopRazorHost()
        {
            RazorHost?.Stop();
        }
    }
}
