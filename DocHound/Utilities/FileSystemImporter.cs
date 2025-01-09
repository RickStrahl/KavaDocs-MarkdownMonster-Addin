using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Configuration;
using DocMonster.Interfaces;
using DocMonster.Model;
using Westwind.Utilities;

namespace DocMonster.Utilities
{
    public class FileSystemImporter
    {
        public string ErrorMessage { get; set; }

        public bool ImportFileSystem(string inputFolder, DocProjectCreator projectCreator)
        {

            if (Directory.Exists(projectCreator.ProjectFolder))
                Directory.Delete(projectCreator.ProjectFolder, true);

            var project = projectCreator.CreateProject();
            
            if (!string.IsNullOrEmpty(projectCreator.ErrorMessage))
            {
                this.ErrorMessage = projectCreator.ErrorMessage;
                return false;
            }
                
            
            var newTopics = new ObservableCollection<DocTopic>();
            project.Topics = newTopics;


            ParseFolder(inputFolder, project, null, inputFolder, project.ProjectDirectory);

            project.SaveProject();

            return true;
        }

        void ParseFolder(string folderName, DocProject project, DocTopic parentTopic,string inputRootFolder, string outputRootFolder)
        {
            foreach (var folder in Directory.GetDirectories(folderName).OrderBy(f=> f.ToLower()))
            {
                var justFolder = Path.GetFileName(folder);
                if (justFolder == ".git" || justFolder == "node_modules")
                    continue;

                var topic = new DocTopic(project)
                {
                    Title = Path.GetFileName(folder),
                    DisplayType = "header",
                    Type = TopicBodyFormats.Markdown,
                    Project = project,
                    ParentId = parentTopic?.Id,
                    Topics = new ObservableCollection<DocTopic>()
                };

                if (parentTopic != null)
                    parentTopic.Topics.Add(topic);
                else
                    project.Topics.Add(topic);
                
                var newFolder = Path.Combine(outputRootFolder, justFolder);
                var newFolderWWW = Path.Combine(outputRootFolder, "wwwroot" ,justFolder);

                //if (!Directory.Exists(newFolder))
                //    Directory.CreateDirectory(newFolder);
                if (!Directory.Exists(newFolderWWW))
                    Directory.CreateDirectory(newFolderWWW);

                ParseFolder(folder, project, topic,inputRootFolder,outputRootFolder);
            }

            foreach (var file in Directory.GetFiles(folderName))
            {
                var title = Path.GetFileNameWithoutExtension(file);
                var ext = Path.GetExtension(file).Replace(".","").ToLower();

                if (!KavaDocsConfiguration.AllowedTopicFileExtensions.Contains(ext))
                {
                    // copy all non-MD files
                    var targetFile = FileUtils.GetRelativePath(file, inputRootFolder);
                    targetFile = System.Net.WebUtility.UrlDecode(targetFile);
                    var newFile = Path.Combine(outputRootFolder,"wwwroot", targetFile);
                    if (newFile.ToLower() != project.Filename.ToLower()) 
                        File.Copy(file, newFile);
                    continue;
                }

                var topic = new DocTopic(project)
                {
                    Title = title,
                    DisplayType = "topic",
                    Type = TopicBodyFormats.Markdown,
                    Project = project,
                    ParentId = parentTopic?.Id
                };

                if (ext == ".htm" || ext == ".html")
                    topic.Type = TopicBodyFormats.Html;

                topic.Body = File.ReadAllText(file);
                
                if (parentTopic != null)
                    parentTopic.Topics.Add(topic);
                else
                    project.Topics.Add(topic);
            }
        }
        

    }
}
