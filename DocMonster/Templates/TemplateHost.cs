using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Westwind.Scripting;
using Westwind.Utilities;

namespace DocMonster.Templates
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
            script.ScriptEngine.AddNamespace("Westwind.Utilities");
            script.ScriptEngine.AddNamespace("DocMonster");
            script.ScriptEngine.AddNamespace("DocMonster.Model");            
            script.ScriptEngine.AddNamespace("DocMonster.Templates");

            script.ScriptEngine.SaveGeneratedCode = true;
            script.ScriptEngine.CompileWithDebug = true;
            script.AdditionalMethodHeaderCode =
                """

                DocTopic Topic = Model.Topic;
                var Project = Model.Project;
                var Configuration = Model.Configuration;
                var Helpers = Model.Helpers;
                var BasePath = new Uri(FileUtils.NormalizePath( Project.ProjectDirectory + "\\") );

                """;
            return script;
        }

        /// <summary>
        /// Make sure the topic.TopicState is set to determine whether to render in
        /// preview mode or Html
        /// </summary>
        /// <param name="templateText"></param>
        /// <param name="model"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public string RenderTemplate(string templateText, object model, out string error)
        {
            error = null;

            string result = Script.ExecuteScript(templateText, model);

            if (Script.Error)
            {
                result =
                    "<h3>Template Rendering Error</h3>\r\n<hr/>\r\n" +
                    "<pre>" + WebUtility.HtmlEncode(Script.ErrorMessage) + "\n" + Script.GeneratedClassCodeWithLineNumbers + "</pre>";


                error = Script.ErrorMessage;
            }
            
            return result;

        }

        public string RenderTemplateFile(string templateFile, RenderTemplateModel model, out string error)
        {
            error = null;

            Script.ScriptEngine.ObjectInstance = null; // make sure we don't cache

            var basePath = model.Project.ProjectDirectory;            

            string result = Script.ExecuteScriptFile(templateFile, model, basePath: basePath);

            if (Script.Error)
            {
                result = ErrorHtml();
                error = Script.ErrorMessage + "\n\n" + Script.GeneratedClassCodeWithLineNumbers;
            }

            return result;
        }

        public string ErrorHtml(string errorMessage = null, string code = null)
        {            
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = Script.ErrorMessage;
            if (string.IsNullOrEmpty(code))
                code = Script.GeneratedClassCodeWithLineNumbers;

            string result =
                    "<style>" +
                    "body { background: white; color; black; font-family: sans;}" +
                    "</style>" +
                    "<h1>Template Rendering Error</h3>\r\n<hr/>\r\n" +
                    "<pre style='font-weight: 600;margin-bottom: 2em;'>" + WebUtility.HtmlEncode(errorMessage) + "</pre>\n\n" +
                    "<pre>" + WebUtility.HtmlEncode(code) + "</pre>";                   

            return result;
        }

    }
}
