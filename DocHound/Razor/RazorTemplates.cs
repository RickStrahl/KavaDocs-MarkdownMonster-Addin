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
    public class RazorTemplates
    {
        public RazorFolderHostContainer<KavaDocsTemplate> RazorHost { get; set; }

        public string RenderTemplate(string template, object model, out string error)
        {                        
            if (RazorHost == null)
                throw new InvalidOperationException(
                    "Can't render template - RazorHost not loaded. Please make sure to call RazorTemplates.StartRazorHost() before calling this method.");

            error = null;
            string result = RazorHost.RenderTemplate(template, model);
            if (result == null)
            {
                result =
                    "<h3>Template Rendering Error</h3>\r\n<hr/>\r\n" +
                    "<pre>" + HtmlUtils.HtmlEncode(RazorHost.ErrorMessage) + "</pre>";
                error = RazorHost.ErrorMessage;
            }

            return result;
        }

        public void StartRazorHost(string projectFolder)
        {        
            StopRazorHost();

            var host = new RazorFolderHostContainer<KavaDocsTemplate>()
            {
                // *** Set your Folder Path here - physical or relative ***
                TemplatePath = Path.GetFullPath(Path.Combine(projectFolder,"_kavadocs","themes")),
                // *** Path to the Assembly path of your application
                BaseBinaryFolder = Environment.CurrentDirectory
            };

            // Add any assemblies that are referenced in your templates
            host.AddAssemblyFromType(typeof(RazorTemplates)); // Dochound
            host.AddAssemblyFromType(typeof(StringUtils));  // Westwind.Utilities
            host.AddAssemblyFromType(typeof(Markdig.Markdown));
            host.AddAssemblyFromType(typeof(Newtonsoft.Json.JsonConvert));
            host.AddAssemblyFromType(typeof(MarkdownMonster.App));

            host.ReferencedNamespaces.Add("DocHound");            
            host.ReferencedNamespaces.Add("DocHound.Model");
            host.ReferencedNamespaces.Add("DocHound.Utilities");
            host.ReferencedNamespaces.Add("DocHound.Configuration");
            host.ReferencedNamespaces.Add("Westwind.Utilities");            

            // Always must start the host
            host.Start();

            RazorHost = host;
        }

        public void StopRazorHost()
        {
            if (RazorHost == null)
                return;

            RazorHost.Stop();
            RazorHost.Dispose();
            RazorHost = null;
        }
    }
}
