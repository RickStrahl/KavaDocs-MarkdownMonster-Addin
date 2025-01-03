using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Westwind.Scripting;

namespace DocHound.Templates
{
    public class TemplateHost
    {

        /// <summary>
        /// Cached Script Execution engine
        /// </summary>
        public static ScriptParser Script
        {
            get
            {
                if (_script == null)
                    _script = CreateScriptParser();

                return _script;
            }
            set
            {
                _script = value;
            }
        }
        private static ScriptParser _script;


        public static ScriptParser CreateScriptParser()
        {
            var script = new ScriptParser();
            script.ScriptEngine.AddDefaultReferencesAndNamespaces();
            script.ScriptEngine.AddLoadedReferences();
            script.ScriptEngine.SaveGeneratedCode = true;

            return script;
        }


        public string RenderTemplate(string templateText, object model, out string error)
        {
            error = null;

            string result = Script.ExecuteScript(templateText, model);

            if (Script.Error)
            {
                result =
                    "<h3>Template Rendering Error</h3>\r\n<hr/>\r\n" +
                    "<pre>" + WebUtility.HtmlEncode(Script.ErrorMessage) + "</pre>";

                error = Script.ErrorMessage;
            }

            return result;

        }

        public string RenderTemplateFile(string templateFile, object model, out string error)
        {
            error = null;

            string result = Script.ExecuteScriptFile(templateFile, model);

            if (Script.Error)
            {
                result =
                    "<h3>Template Rendering Error</h3>\r\n<hr/>\r\n" +
                    "<pre>" + WebUtility.HtmlEncode(Script.ErrorMessage) + "</pre>";

                error = Script.ErrorMessage;
            }

            return result;

        }
    }
}
