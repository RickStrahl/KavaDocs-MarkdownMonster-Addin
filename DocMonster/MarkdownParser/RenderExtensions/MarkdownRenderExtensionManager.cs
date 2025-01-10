using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using MarkdownMonster;

//using MarkdownMonster;

namespace DocMonster.MarkdownParser;

/// <summary>
/// Manages RenderExtensions that are executed during Markdown rendering.
/// You can intercept markdown on pre-rendering and html after rendering
/// and modify by implementing custom IRenderExtension implementations
/// and adding to MarkdownRenderExtensionManager.Current.RenderExtensions.
/// </summary>
public class MarkdownRenderExtensionManager
{
    /// <summary>
    /// Signleton instance of the MarkdownRenderExtensionManager that
    /// globally manages RenderExtensions during the rendering process
    /// </summary>
    public static MarkdownRenderExtensionManager Current { get; set; } 

    /// <summary>
    /// A list of MarkdownRenderExtensions that are executed before and after rendering
    /// and that allow for customization of the rendered Markdown output
    /// </summary>
    internal List<IMarkdownRenderExtension> RenderExtensions { get; set; } = new();

    static MarkdownRenderExtensionManager()
    {
        Current = new MarkdownRenderExtensionManager();
        LoadDefaultExtensions();
    }


    /// <summary>
    /// Default extensions that are loaded when the class is instantiated.
    ///
    /// You can remove these or add to them.
    /// </summary>
    public static void LoadDefaultExtensions()
    {
            // Explicitly reference namespaces to ensure we're using the DocMonster extensions not the default MM ones
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.AddRenderExtension(new DocMonster.MarkdownParser.FontAwesomeRenderExtension());
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.AddRenderExtension(new DocMonster.MarkdownParser.PlantUmlMarkdownRenderExtension());
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.AddRenderExtension(new DocMonster.MarkdownParser.MermaidRenderExtension());
            DocMonster.MarkdownParser.MarkdownRenderExtensionManager.Current.AddRenderExtension(new DocMonster.MarkdownParser.MathRenderExtension());
    }

    public void ProcessAllBeforeMarkdownRenderedHooks(ModifyMarkdownArguments args)
    {
        foreach (var extension in RenderExtensions)
        {
            try
            {
                extension.BeforeMarkdownRendered(args);
            }
            catch (Exception ex)
            {
                mmApp.Log($"BeforeMarkdownRendered RenderExtension failed: {extension.GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    /// Processed after Markdown has been rendered into HTML, but not been
    /// merged into the template.
    ///
    /// You can modify the HTML and also add headers to be rendered into the HEAD
    /// of the template here.
    /// </summary>
    /// <param name="args"></param>
    public void ProcessAllAfterMarkdownRenderedHooks(DocMonster.MarkdownParser.ModifyHtmlAndHeadersArguments args)
    {
        foreach (var extension in RenderExtensions)
        {
            args.HeadersToEmbed = null;

            // update html content using the ref HTML parameter
            try
            {
                extension.AfterMarkdownRendered(args);
            }
            catch (Exception ex)
            {
                
                //mmApp.Log($"AfterMarkdownRendered RenderExtension failed: {extension.GetType().Name}", ex);
            }

        }
    }

    /// <summary>
    /// Returns a RenderExtension by name
    /// </summary>
    /// <param name="renderExtensionName"></param>
    /// <returns></returns>
    public IMarkdownRenderExtension this[string renderExtensionName]
    {
        get
        {
            return RenderExtensions.FirstOrDefault(e => e.Name.Equals(renderExtensionName, System.StringComparison.OrdinalIgnoreCase));
        }
    }
    /// <summary>
    /// Returns a RenderExtension by name
    /// </summary>
    /// <param name="renderExtensionName"></param>
    /// <returns></returns>

    public IMarkdownRenderExtension this[IMarkdownRenderExtension renderExtension]
    {
        get { return RenderExtensions.FirstOrDefault(e => e == renderExtension); }
    }

    /// <summary>
    /// Return render extensions as a read-only collection
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IMarkdownRenderExtension> GetRenderExtensions()
    {
        return RenderExtensions.AsEnumerable();
    }

    /// <summary>
    /// Adds a new render extension to the list of render extensions   
    /// </summary>
    /// <param name="renderExtension"></param>
    public void AddRenderExtension( IMarkdownRenderExtension renderExtension)
    {
        if (RenderExtensions == null)
            RenderExtensions = new List<IMarkdownRenderExtension>();

        if(RenderExtensions.Count > 1 && RenderExtensions.Contains(renderExtension) )
        {
            // remove and re-add to ensure it's at the end of the list
            RenderExtensions.Remove(renderExtension);
        }

        RenderExtensions.Add(renderExtension);
    }

    /// <summary>
    /// Add multiple RenderExtensions
    /// </summary>
    /// <param name="renderExtensions"></param>
    public void AddRenderExtensions(IEnumerable<IMarkdownRenderExtension> renderExtensions)
    {
        foreach(var ext in renderExtensions)
        {
            AddRenderExtension(ext);
        }        
    }

    /// <summary>
    /// Removes a render extension
    /// </summary>
    /// <param name="renderExtension"></param>
    public void RemoveRenderExtension(IMarkdownRenderExtension renderExtension)
    {
        if (RenderExtensions == null)
            return;
        if(RenderExtensions.Contains(renderExtension))
            RenderExtensions.Remove(renderExtension);
    }

    public void RemoveRenderExtension(string renderExtensionName)
    {
        if (RenderExtensions == null || string.IsNullOrEmpty(renderExtensionName))
            return;

        var ext = RenderExtensions.FirstOrDefault(e => e.Name.Equals(renderExtensionName, System.StringComparison.OrdinalIgnoreCase));
        if (ext != null)
         RenderExtensions.Remove(ext);
    }
}
