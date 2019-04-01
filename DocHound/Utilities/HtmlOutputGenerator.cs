using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using Westwind.Utilities;

namespace DocHound.Utilities
{
    public class HtmlOutputGenerator
    {
        public DocProject Project { get; set; }


        public string OutputPath { get; set; }

        public string SourcePath { get; set; }

        public HtmlOutputGenerator(DocProject project)
        {
            Project = project;
            
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
                if (ext == ".md" || ext == ".json" || ext == ".cshtml")
                    continue;

                string target = FileUtils.GetRelativePath(file.FullName, SourcePath);
                target = Path.Combine(OutputPath, target);                
                File.Copy(file.FullName, target, true);
            }
        }

        public void GenerateHtml()
        {
            Project.WalkTopicsHierarchy( Project.Topics, (topic,project)=>
            {
                topic.Project = project;
                topic.RenderTopicToFile();
            });            
        }

        

        public void GenerateTableOfContents()
        {

            if (string.IsNullOrEmpty(OutputPath))
                OutputPath = Project.OutputDirectory;

            var rootTopic = Project.Topics[0];
            rootTopic.Project = Project;
            
            string error;
            string html = Project.TemplateRenderer.RenderTemplate("TableOfContents.cshtml", rootTopic, out error);            
            File.WriteAllText(Path.Combine(OutputPath, "TableOfContents.html"),html); 
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
