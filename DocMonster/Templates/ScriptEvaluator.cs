using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Westwind.Utilities;

namespace DocMonster.Templates
{
    /// <summary>
    /// Very basic script evaluator that allows expression expansion without
    /// compilation against a provided list of instance objects. Only works
    /// with {{ expr }} syntax and only with explicit objects instances that
    /// have been provided in the AllowedInstances dictionary.
    ///
    /// This allows for limited script evaluation in user provided content
    /// for example, inside of a body block of documentation.
    ///
    /// Support is limited to named instances  that you provide in the AllowedInstances
    /// dictionary. Any 'name' provided is matched to the expression instance.
    ///
    /// Method execution currently only works with non-parameter methods.
    ///
    /// TODO: Add limited support for string method parameters
    /// </summary>
    public class ScriptEvaluator
    {
        /// <summary>
        /// Instances that are allowed to be used in expressions. Specify the instance
        /// name that is in scope in expressions (ie. {{ Project.Title }}) == "Project")
        /// and the actual instance.
        /// </summary>
        public Dictionary<string, object> AllowedInstances { get;  } = new Dictionary<string, object>();

        /// <summary>
        /// Optional - Default expressions instance if the expression is not providing an
        /// instance prefix.
        /// </summary>
        public KeyValuePair<string, object> DefaultInstance { get; set; }



        /// <summary>
        /// Expands evaluated {{ expr }} expressions in a content block. Uses
        /// the AllowedInstances dictionary to evaluate expressions on
        /// allowed instances based on the instance name in the expressions
        /// (ie. {{ Project.Title }} is passing in 'Project' instance)
        /// </summary>
        /// <param name="content">Content with embedded script expressions</param>
        /// <returns></returns>
        public string Evaluate(string content)
        {
            var scripts = ParseScriptExpressions(content);
            return ExpandExpressions(content, scripts);
        }


        /// <summary>
        /// Expands expressions in a previously parsed string content block
        /// and replaces script expressions with evaluated content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="scripts"></param>
        /// <returns></returns>
        public string ExpandExpressions(string content, IList<ScriptExpression> scripts)
        {
            foreach (var script in scripts)
            {
                if (script.DontProcess ||
                    !AllowedInstances.TryGetValue(script.Instance, out var instance))
                    continue;

                try
                {
                    if (!script.IsMethod)
                        script.ResultValue = ReflectionUtils.GetProperty(instance, script.Property);
                    else
                    {
                        script.ResultValue = ReflectionUtils.CallMethod(instance, script.Method);
                        // TODO deal with parameters
                    }
                }
                catch (Exception ex)
                {
                    content = content.Replace(script.ScriptTag, $"{{ ERROR: {script.Property ?? script.Method} }}");
                }

                string evaled = script.ResultValue?.ToString();
                if (!script.ScriptTag.StartsWith("{{!") || script.ResultValue is IRawString)
                    evaled = System.Net.WebUtility.HtmlEncode(evaled);

                content = content.Replace(script.ScriptTag, evaled);
            }

            return content;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IList<ScriptExpression> ParseScriptExpressions(string content)
        {            
            var scripts = new List<ScriptExpression>();

            // allow Topic, Project, Helpers, Script
            var matches = Regex.Matches(content, "{{.*?}}", RegexOptions.Singleline | RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                string text = match.Value;
                var item = new ScriptExpression()
                {
                    ScriptTag = text
                };

                string code = StringUtils.ExtractString(text, "{{", "}}")?
                                         .TrimStart(':', '!', '@')  // strip expression modifiers
                                         .Trim();
                var tokens = code.Split('.');
                if (tokens.Length == 0)
                {
                    if (string.IsNullOrEmpty(DefaultInstance.Key))
                        item.Instance = DefaultInstance.Key;
                    else
                        item.DontProcess = true;
                }
                else
                {
                    item.Instance = tokens[0];
                    var member = tokens[1];
                    if (member.Contains("("))
                    {
                        item.IsMethod = true;
                        item.Method = member.Substring(0, member.IndexOf('('));
                        // TODO Parse Parameters -string only allowed
                    }
                    else
                    {
                        item.Property = member;
                    }
                }

                scripts.Add(item);
            }

            return scripts;
        }
    }
}


public class ScriptExpression
{
    public string ScriptTag { get; set; }

    public string Instance { get; set; }

    public string Property { get; set; }

    public string Method { get; set; }

    public List<string> MethodParameters { get; set; } = new List<string>();

    public object ResultValue { get; set; }

    public bool IsMethod { get; set; }

    public bool DontProcess { get; set; }
}
