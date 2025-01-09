using System.Net;
using MarkdownMonster;
using Westwind.Utilities;

namespace DocMonster.MarkdownParser
{   

    /// <summary>
    /// Handles Mermaid charts based on one of two sytnax:
    ///
    /// * Converts ```mermaid syntax into div syntax
    /// * Adds the mermaid script from CDN
    /// </summary>
    public class MermaidRenderExtension : IMarkdownRenderExtension
    {
        public string Name { get; set; } = "MermaidRenderExtension";

        /// <summary>
        /// Check for ```markdown blocks and replace them with DIV blocks
        /// </summary>
        /// <param name="args"></param>
        public void BeforeMarkdownRendered(ModifyMarkdownArguments args)
        {
            if (string.IsNullOrEmpty(args.Markdown)) return;
                
            while (true)
            {
                string extract = StringUtils.ExtractString(args.Markdown, "```mermaid", "```", returnDelimiters: true);
                if (string.IsNullOrEmpty(extract))
                    break;

                string newExtract = WebUtility.HtmlEncode(extract);

                newExtract = newExtract.Replace("```mermaid", "<pre class=\"mermaid\">")
                    .Replace("```", "</pre>");

                args.Markdown = args.Markdown.Replace(extract, newExtract);
            }
        }

        /// <summary>
        /// Add script block into the document
        /// </summary>
        /// <param name="args"></param>
        public void AfterMarkdownRendered(ModifyHtmlAndHeadersArguments args)
        {
            if (string.IsNullOrEmpty(args.Markdown)) return;

            if (args.Markdown.Contains(" class=\"mermaid\"") || args.Markdown.Contains("```mermaid"))
                args.HeadersToEmbed = MermaidHeaderScript;
        }

        /// <summary>
        /// Embed the Mermaid script link into the head of the page
        /// </summary>
        /// <param name="args"></param>
        public void AfterDocumentRendered(ModifyHtmlArguments args)
        {
        }

        private static string MermaidHeaderScript = $"\n<script id=\"MermaidScript\" src=\"{mmApp.Configuration.Markdown.MermaidDiagramsUrl}\"></script>\n" +
@"
<style>
pre.mermaid {
    border: none !important;
}
</style>
<script>
mermaid.initialize({startOnLoad: true});
</script>

<script>
$(function() {
    function renderMermaid() {
        mermaid.init(undefined,'.mermaid');    
    }  
    $(document).on('previewUpdated', function() {
        renderMermaid();
    });
});
</script>";

    }
}
