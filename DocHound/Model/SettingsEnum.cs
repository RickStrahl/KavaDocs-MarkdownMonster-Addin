using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DocHound.Interfaces
{
    /// <summary>All available settings supported by the system either as global configuration or as settings on a TOC or an invidual Topic</summary>
    /// <remarks>Settings in JSON files are usually set in camelCase, but our system can handle settings in a non-case-sensitive fashion.</remarks>
    public enum SettingsEnum
    {
        Unknown,
        RepositoryType,
        SqlConnectionString,

        // GitHub settings
        GitHubProject,
        GitHubMasterUrl,

        // VSTS settings
        VstsInstance,
        VstsPat,
        VstsDocsFolder,
        VstsProjectName,
        [DefaultValue("4.1-preview")] VstsApiVersion,

        // General topic toggle setting 
        [DefaultValue(false)] RequireAuthentication,
        [DefaultValue(true)] UseSyntaxHighlighting,
        [DefaultValue(false)] AllowThemeSwitching,
        [DefaultValue("")] AllowableThemeColors,
        [DefaultValue(true)] AllowThemeColorSwitching,
        [DefaultValue(true)] AllowSyntaxHighlightingThemeSwitching,
        [DefaultValue("")] AllowableSyntaxHighlightingThemes,
        [DefaultValue(true)] RequireHttps,
        [DefaultValue(TrueFalseAuto.Auto)] RenderTitleInTopic,

        // Content and theme settings
        [DefaultValue("~/Images/SiteIcon.png")] SiteIcon,
        [DefaultValue("_meta/_logo.png")] LogoPath,
        [DefaultValue("<p>Documentation engine provided by <a href=\"https://kavadocs.com\">Kava Docs</a>.<br /><i class=\"fa fa-heart\"></i> Made with Aloha in Maui, Hawaii.</p>")]
        FooterHtml,
        [DefaultValue("Default")] Theme,
        [DefaultValue("Default")] ThemeColors,
        [DefaultValue("")] CustomCssPath,
        [DefaultValue("KavaDocs")] SyntaxTheme,
        [DefaultValue(true)] ShowEstimatedReadingTime,

        // Markdown related settings
        [DefaultValue(true)] UseAbbreviations,
        [DefaultValue(true)] UseAutoIdentifiers,
        [DefaultValue(true)] UseAutoLinks,
        [DefaultValue(true)] UseCitations,
        [DefaultValue(true)] UseCustomContainers,
        [DefaultValue(false)] UseDiagramsMermaid,
        [DefaultValue(false)] UseDiagramsNomnoml,
        [DefaultValue(true)] UseEmojiAndSmiley,
        [DefaultValue(true)] UseEmphasisExtras,
        [DefaultValue(true)] UseFigures,
        [DefaultValue(true)] UseFootnotes,
        [DefaultValue(true)] UseGenericAttributes,
        [DefaultValue(true)] UseGridTables,
        [DefaultValue(true)] UseListExtras,
        [DefaultValue(false)] UseMathematics,
        [DefaultValue(true)] UseMediaLinks,
        [DefaultValue(true)] UsePipeTables,
        [DefaultValue(true)] UsePragmaLines,
        [DefaultValue(true)] UseSmartyPants,
        [DefaultValue(true)] UseTaskLists,
        [DefaultValue(true)] UseYamlFrontMatter,
        [DefaultValue(true)] UseFontAwesomeInMarkdown,

        // KavaDocs Client
        [DefaultValue(true), Description("Determines whether topics are sorted in the tree")]
        AutoSortTopics,

        [DefaultValue(true), Description("Determines whether all related meta data is stored as YAML in the Markdown file in addition to the JSON properties.")]
        StoreYamlInTopics,


        [DefaultValue(null),Description("Allowable Topic types dictionary for topics.")]
        TopicTypes

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    sealed class DocumentScopeAttribute : Attribute
    {
        public DocumentScopes Scope { get; set; }

        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        public DocumentScopeAttribute(DocumentScopes scope = DocumentScopes.Markdown)
        {
            Scope = scope;
        }
    }



    public enum DocumentScopes
    {
        Unknown,
        Markdown,
        Text
    }

    public enum TrueFalseAuto
    {
        Auto,
        True,
        False
    }



    public class TopicBodyFormats
    {
        public static List<string> TopicBodyFormatsList { get; } = new List<string>();

        static TopicBodyFormats()
        {
            var pi = typeof(TopicBodyFormats).GetProperties(System.Reflection.BindingFlags.Static |
                                                            System.Reflection.BindingFlags.Public |
                                                            System.Reflection.BindingFlags.GetProperty).Where(p =>
                p.Name != nameof(TopicBodyFormatsList))
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var p in pi)
            {
                TopicBodyFormatsList.Add(p.GetValue(null).ToString());
            }
        }

        public static string Markdown => "markdown";
        public static string Html => "html";
        public static string ImageUrl => "imageurl";
        public static string HelpBuilder => "helpbuilder";
        public static string VstsWorkItemQueries => "vsts-workitem-queries";
        public static string VstsWorkItem => "vsts-workitem";
        public static string VstsWorkItemQuery => "vsts-workitem-query";
        public static string VstsWorkItemQueryToc => "vsts-workitem-query:toc";
        public static string VstsWorkItemQueriesToc => "vsts-workitem-queries:toc";
    }

}
