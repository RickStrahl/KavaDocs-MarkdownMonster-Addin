using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using NUnit.Framework;

namespace DocumentationMonster.Core.Tests
{
    [TestFixture]
    class ProjectManagementTests
    {
        private string hbImportProjectFile = @"C:\Temp\markdownmonster_help\_toc_original.json";
        private string projectFile = @"C:\Temp\markdownmonster_help\_toc.json";

        [Test]
        public void LoadAndSaveFlatProjectTest()
        {
            var project = DocProject.LoadProject(hbImportProjectFile);
            project.GetTopicTreeFromFlatList(project.Topics);            
            WriteChildTopics(project.Topics,0);

            project.Filename = projectFile;
            project.SaveProject();
        }

        [Test]
        public void LoadProjectTest()
        {
            var project = DocProject.LoadProject(projectFile);
            project.GetTopicTree(project.Topics);
            WriteChildTopics(project.Topics, 0);

        }

        [Test]
        public void LoadAndSaveProjectTest()
        {
            var project = DocProject.LoadProject(projectFile);
            project.GetTopicTree(project.Topics);
            WriteChildTopics(project.Topics, 0);

            project.Filename = projectFile;
            project.SaveProject();
        }

        private void WriteChildTopics(ObservableCollection<DocTopic> topics, int level)
        {
            if (topics == null || topics.Count < 1)
                return;

            Console.WriteLine("    " + level);
            foreach (var topic  in topics)
            {
                Console.WriteLine(new string(' ', level * 2) +
                                  $"{topic.Title} - {topic.Id} {topic.ParentId} {topic.Project != null}");
                                  
                WriteChildTopics(topic.Topics, level + 1);
            }
        }
    }



}
