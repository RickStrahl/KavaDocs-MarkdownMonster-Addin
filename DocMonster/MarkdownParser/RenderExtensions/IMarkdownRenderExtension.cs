using Markdig.Syntax;

namespace DocMonster.MarkdownParser;

/// <summary>
/// Interface implemented for RenderExtensions that allow modification
/// of the inbound Markdown before rendering or outbound HTML after
/// rendering as well as any custom code that needs to be injected
/// into the document header prior to rendering.
///
/// Use the `RenderExtensionsManager.Current.RenderExtensions.Add()` to
/// add any custom extensions you create. Typically you do this in 
/// the `Addin.OnApplicationStart()` method.
/// </summary>
public interface IMarkdownRenderExtension
{
    /// <summary>
    /// The name of the Render Extension
    /// </summary>
    public string Name { get; set; }


    /// <summary>
    /// Method that is fired on the inbound pass before the document is rendered and that
    /// allows you to modify the *markdown* before it is sent out for rendering.
    ///
    /// Markdown text is in Linefeeds only (\n) mode.
    /// </summary>
    /// <param name="args">Arguments that allow you to update the markdown text
    ///  and also see the original document.
    /// </param>
    void BeforeMarkdownRendered(ModifyMarkdownArguments args);

    /// <summary>
    /// Fired after Markdown has been converted to HTML and allows you to modify
    /// the rendered HTML fragment generated by the markdown. Note that this
    /// method can only change the rendered Markdown html, not the entire document.
    ///
    /// You can change the passed in Html reference to make a change to the document.
    /// </summary>
    /// <param name="args">Arguments that let you modify the generated HTML before it's returned or written to disk
    /// </param>
    void AfterMarkdownRendered(ModifyHtmlAndHeadersArguments args);


    /// <summary>
    /// Fired after the document has been rendered to a complete HTML document
    /// using a Preview Template. Input HTML contains the final full HTML document.
    ///
    /// Note this is **not fired** if using just raw Markdown rendering 
    /// </summary>
    /// <param name="args">Arguments that let you modify HTML and Headers and
    /// let you view original Markdown and Document.</param>
    void AfterDocumentRendered(ModifyHtmlArguments args);


}

public class ModifyMarkdownArguments
{
    public ModifyMarkdownArguments(string markdown = null, MarkdownDocument doc = null)
    {
        Markdown = markdown;
        MarkdownDocument = doc;
    }

    public string Markdown { get; set; }

    public MarkdownDocument MarkdownDocument { get; }
    public bool IsPreview { get; set; }
}

public class ModifyHtmlAndHeadersArguments
{
    public ModifyHtmlAndHeadersArguments(string html = null, string markdown = null, MarkdownDocument doc = null)
    {
        Html = html;
        Markdown = markdown;
        MarkdownDocument = doc;
    }

    public string Html { get; set; }

    public string Markdown { get; }

    public string HeadersToEmbed { get; set; }

    public MarkdownDocument MarkdownDocument { get; }

    public bool IsPreview { get; set; }
}


/// <summary>
/// Data object that carries the original Markdown and generated HTML
/// </summary>
public class ModifyHtmlArguments
{
    public ModifyHtmlArguments(string html = null, string markdown = null)
    {
        Html = html;
        Markdown = markdown;
    }

    public string Html { get; set; }

    public string Markdown { get; }

}
