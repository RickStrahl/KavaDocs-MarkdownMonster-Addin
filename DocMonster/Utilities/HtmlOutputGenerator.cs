using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Model;
using DocMonster.Templates;
using Westwind.Utilities;

namespace DocMonster.Utilities
{
    public class HtmlOutputGenerator
    {
        public DocProject Project { get; set; }


        public string OutputPath { get; set; }

        public string SourcePath { get; set; }

        public HtmlOutputGenerator(DocProject project)
        {
            Project = project;

            OutputPath = Project.OutputDirectory;
            SourcePath = Project.ProjectDirectory;
        }

        public void Generate()
        {
            if (string.IsNullOrEmpty(OutputPath))
                OutputPath = Project.OutputDirectory;

            if (string.IsNullOrEmpty(SourcePath))
                SourcePath = Project.ProjectDirectory;

            CopyFoldersAndStaticFiles();
            GenerateHtml();
            GenerateTableOfContents();
        }

        public void CopyFoldersAndStaticFiles()
        {
            if (string.IsNullOrEmpty(OutputPath))
                OutputPath = Project.OutputDirectory;

            if (Directory.Exists(OutputPath))
            {
                try
                {
                    Directory.Delete(OutputPath, true);
                }
                catch { }
            }

            Directory.CreateDirectory(OutputPath);

            var di = new DirectoryInfo(SourcePath);
            var folders = di.GetDirectories("*.*",  SearchOption.AllDirectories);
            foreach (var folder in folders)
            {
                
                if (string.Equals(folder.Name,".git",StringComparison.InvariantCulture) ||                    
                    string.Equals(folder.Name, "wwwroot"))                    
                    continue;

                string target = FileUtils.GetRelativePath(folder.FullName, SourcePath);
                target = Path.Combine(OutputPath, target);
                Directory.CreateDirectory(target);                
            }

            var files = di.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Name);
                if (string.IsNullOrEmpty(ext))
                    ext = ext.ToLower();
                if (ext == ".md" || ext == ".cshtml" || ext == ".bak" || ext == ".tmp")
                    continue;

                string target = FileUtils.GetRelativePath(file.FullName, SourcePath);
                target = Path.Combine(OutputPath, target);                
                File.Copy(file.FullName, target, true);
            }
        }

        public void CopyScriptsAndTemplates()
        {
            var outputPath = Path.Combine(OutputPath, "_kavadocs");
            var sourcePath = Path.Combine(SourcePath, "_kavadocs");

            if (string.IsNullOrEmpty(outputPath))
                outputPath = Path.Combine(Project.OutputDirectory, "_kavadocs");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var di = new DirectoryInfo(sourcePath);
            var folders = di.GetDirectories("*.*", SearchOption.AllDirectories);
            foreach (var folder in folders)
            {
                string target = FileUtils.GetRelativePath(folder.FullName, sourcePath);
                target = Path.Combine(outputPath, target);
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);
            }

            var files = di.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Name);
                if (string.IsNullOrEmpty(ext))
                    ext = ext.ToLower();
                if (ext == ".md" || ext == ".cshtml" || ext == ".bak" || ext == ".tmp")
                    continue;

                string target = FileUtils.GetRelativePath(file.FullName, sourcePath);
                target = Path.Combine(outputPath, target);
                File.Copy(file.FullName, target, true);
            }
        }


        public void GenerateHtml()
        {
            Project.WalkTopicsHierarchy( Project.Topics, (topic,project)=>
            {
                if (topic.DontRenderTopic)
                    return;

                topic.Project = project;
                topic.TopicState.IsPreview = false;  // so topics can see           
                topic.RenderTopicToFile();
            });

            var rootTopic = Project.LookupTopic("index";
            if (rootTopic == null && Project.Topics.Count > 0)
                rootTopic = Project.Topics[0];
            if (rootTopic == null)
                return;

            // Create Default Page (same as the Index)
            rootTopic.TopicState.IsToc = false;
            rootTopic.TopicState.Data = false;
            var file = Path.Combine(OutputPath, "index.html");
            rootTopic.RenderTopicToFile(file, TopicRenderModes.Html);            
        }

        public void GenerateTableOfContents()
        {

            if (string.IsNullOrEmpty(OutputPath))
                OutputPath = Project.OutputDirectory;

            var rootTopic = Project.LoadTopic("index", true);
            if (rootTopic == null)
                rootTopic = Project.Topics[0];
            rootTopic.Project = Project;
            rootTopic.TopicState.IsPreview = false;
            rootTopic.TopicState.IsToc = true;

            var sb = new StringBuilder();
            RenderTocLevel(Project.Topics, sb, 0);

            rootTopic.TopicState.Data = sb.ToString();

            string file = Path.Combine(Project.ProjectDirectory,"_kavadocs","Themes", "TableOfContents.html");
            string html = rootTopic.RenderTopic(renderMode: TopicRenderModes.Html);            
            File.WriteAllText(Path.Combine(OutputPath, "TableOfContents.html"),html);

            rootTopic.TopicState.IsToc = false;
        }

        public void RenderTocLevel(IList<DocTopic> topics, StringBuilder sb, int level = 0 )
        {
            if (level > 0)
                sb.AppendLine("<ul>");

            foreach (var topic in topics)
            {
                if (topic.DontRenderTopic)
                    continue;

                string pad = string.Empty;

                if(topic.IsLink && !string.IsNullOrEmpty(topic.Body))
                {
                    var html =
                        $"""
                         <li>
                             <div>
                                 <img src="_kavadocs/icons/{topic.DisplayType}.png">
                                 <a href="{topic.Body}" id="{topic.Id.ToLower()}" target="_blank">{topic.Title}</a> <i class="fas fa-arrow-up-right-from-square info small"></i>
                             </div>
                         </li>
                         """;
                    sb.AppendLine(html);
                }
                else if (topic.Topics.Count > 0)
                {
                    if (!topic.Id.Equals("index", StringComparison.OrdinalIgnoreCase))
                        topic.IsExpanded = false;

                    // nesting 
                    var html =
$"""
<li>
    <i class="fa fa-caret-{(topic.IsExpanded ? "right" : "down")}"></i>
    <div>
        <img src="_kavadocs/icons/{topic.DisplayType}.png">
        <a href="{topic.Slug}.html" id="{topic.Id.ToLower()}">{topic.Title}</a>
    </div>

""";
                    sb.AppendLine(html);
                    RenderTocLevel(topic.Topics, sb, level +1);
                    sb.AppendLine("</li>");
                }
                else
                {
                    var html =
$"""
<li>
    <div>
        <img src="_kavadocs/icons/{topic.DisplayType}.png">
        <a href="{topic.Slug}.html" id="{topic.Id.ToLower()}">{topic.Title}</a>
    </div>
</li>
""";
                    sb.AppendLine(html);
                }
            }

            if (level > 0)
            sb.AppendLine("</ul>");

        }

        public void WriteStatus()
        {

        }

        public void WriteStatusError()
        {

        }

        #region Error

        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }

        #endregion
    }
}
