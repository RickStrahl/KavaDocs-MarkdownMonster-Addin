using System.Linq;
using System.Text.RegularExpressions;

namespace DocMonster.MarkdownParser
{
    /// <summary>
    /// Base class that includes various fix up methods for custom parsing
    /// that can be called by the specific implementations.
    /// </summary>
    public abstract class MarkdownParserBase : IMarkdownParser
    {
        protected static Regex strikeOutRegex = 
            new Regex("~~.*?~~", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Parses markdown
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public abstract string Parse(string markdown);
        
        /// <summary>
        /// Parses strikeout text ~~text~~. Single line (to linebreak) allowed only.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected string ParseStrikeout(string html)
        {
            if (html == null)
                return null;

            var matches = strikeOutRegex.Matches(html);
            foreach (Match match in matches)
            {
                string val = match.Value;

                if (match.Value.Contains('\n'))
                    continue;

                val = "<del>" + val.Substring(2, val.Length - 4) + "</del>";
                html = html.Replace(match.Value, val);
            }

            return html;
        }

        //static readonly Regex YamlExtractionRegex = new Regex("^---[\n,\r\n].*?^---[\n,\r\n]", RegexOptions.Singleline | RegexOptions.Multiline);

        ///// <summary>
        ///// Strips 
        ///// </summary>
        ///// <param name="markdown"></param>
        ///// <returns></returns>
        //public string StripFrontMatter(string markdown)
        //{
        //    string extractedYaml = null;
        //    var match = YamlExtractionRegex.Match(markdown);
        //    if (match.Success)
        //        extractedYaml = match.Value;

        //    if (!string.IsNullOrEmpty(extractedYaml))
        //        markdown = markdown.Replace(extractedYaml, "");

        //    return markdown;
        //}

        /// <summary>
        /// Parses out script tags that might not be encoded yet
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected string ParseScript(string html)
        {
            html = html.Replace("<script", "&lt;script");
            html = html.Replace("</script", "&lt;/script");
            html = html.Replace("javascript:", "javaScript:");
            return html;
        }


        /// <summary>
        /// Replaces all links with target="top" links
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected string ParseExternalLinks(string html)
        {            
            return html.Replace("<a href=\"http", "<a target=\"top\" href=\"http");
        }

    }
}
