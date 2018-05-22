using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DocHound;
using DocHound.Annotations;
using DocHound.Configuration;
using DocHound.Model;
using FontAwesome.WPF;
using KavaDocsAddin;
using KavaDocsAddin.Controls;
using KavaDocsAddin.Core.Configuration;
using MarkdownMonster;
using Westwind.Utilities;

namespace KavaDocsAddin
{
    public class KavaDocsModel : INotifyPropertyChanged
    {

        #region Top Level Model Properties

        /// <summary>
        /// An instance of the main Markdown Monster application WPF form
        /// </summary>
        public MainWindow Window { set; get; }

        public KavaDocsAddin Addin { get; set; }

        public TopicsTree TopicsTree
        {
            get { return Addin.Tree; }
        }

        public TopicEditor TopicEditor { get; set; }

        /// <summary>
        /// The application's main configuration object
        /// </summary>
        public KavaDocsConfiguration Configuration { get; set; }

        public ApplicationConfiguration MarkdownMonsterConfiguration { get; set; }

        public double LastScrollPosition { get; set; }

        /// <summary>
        /// The Markdown Monster Model
        /// </summary>
        public AppModel Model { get; set; }



        /// <summary>
        /// Determines whether a project is active
        /// </summary>
        public bool IsProjectActive => ActiveProject != null;

        /// <summary>
        /// The currently Active Project 
        /// </summary>
        public DocProject ActiveProject
        {
            get { return _activeProject; }
            set
            {
                if (Equals(value, _activeProject)) return;
                _activeProject = value;
                OnPropertyChanged(nameof(ActiveProject));
                OnPropertyChanged(nameof(ActiveTopic));
                OnPropertyChanged(nameof(IsProjectActive));
            }
        }


        /// <summary>
        /// The currently Active Topic
        /// </summary>
        public DocTopic ActiveTopic
        {
            get { return ActiveProject?.Topic; }
            set
            {

                if (ActiveProject == null)
                    return;

                ActiveProject.Topic = value;

                // always load the topic file
                ActiveTopic?.LoadTopicFile();

                OnPropertyChanged();
            }
        }

        private DocProject _activeProject;


        public DocTopic LastTopic
        {
            get { return _lastTopic; }
            set
            {
                if (Equals(value, _lastTopic)) return;
                _lastTopic = value;
                OnPropertyChanged();
            }
        }

        private DocTopic _lastTopic;

        public ObservableCollection<DocTopic> RecentTopics { get; set; } = new ObservableCollection<DocTopic>();

        /// <summary>
        /// Returns the active Markdown Editor
        /// </summary>
        public MarkdownDocumentEditor ActiveMarkdownEditor => mmApp.Model.ActiveEditor;

        public AppCommands Commands { get; set; }

        #endregion






        #region Initialization

        public KavaDocsModel(MainWindow window)
        {
            Configuration = KavaApp.Configuration;
            Window = window;
            Model = window.Model;
            Commands = new AppCommands(this);
            kavaUi.AddinModel = this;
        }



        #endregion


        #region Navigation


        /// <summary>
        /// Makes a topic the active topic
        /// </summary>
        /// <param name="topic"></param>
        public void LoadTopic(DocTopic topic)
        {
            ActiveTopic = topic;
        }


        /// <summary>
        /// Loads a topic by its topic Id 
        /// </summary>
        /// <param name="topicId"></param>
        public void LoadTopicById(string topicId)
        {
            ActiveTopic = ActiveProject.LoadTopic(topicId);
        }

        /// <summary>
        /// Loads or reloads a project by filename
        /// </summary>
        /// <param name="activeProjectFilename"></param>
        public void LoadProject(string activeProjectFilename)
        {
            var proj = DocProject.LoadProject(activeProjectFilename);
            TopicsTree.LoadProject(proj);
            ActiveProject = proj;
        }

        #endregion

        #region Project Operations

        public void OpenProject(string projectFile, bool noErrorDisplay = false)
        {

            Window.ShowStatus("Opening project file...");
            ActiveProject = null;

            DocProject project = null;

            if (string.IsNullOrEmpty(projectFile))
            {
                /// TODO: Need Wizard here
                project = DocProjectManager.CreateProject(new DocProjectCreator()
                {
                    Filename = System.IO.Path.GetFileName(projectFile),
                    ProjectFolder = System.IO.Path.GetDirectoryName(projectFile),
                    Title = "New Project",
                    Owner = "West Wind Technologies"
                });
            }
            else
            {
                project = DocProject.LoadProject(projectFile);
                if (project == null)
                {
                    if (!noErrorDisplay)
                        MessageBox.Show("Failed to load project.",
                            KavaApp.ApplicationName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    else
                    {
                        Window.ShowStatus("Failed to load project: " + projectFile,
                            KavaApp.Configuration.StatusMessageTimeout);
                        Window.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Red);
                    }

                    Window.ShowStatus();
                    return;
                }
            }

            Window.ShowStatus("Generating project tree...");

            ActiveProject = project;
            TopicsTree.LoadProject(project);

            PreviewTopic();

            Window.ShowStatus($"Project '{project.Title}' opened.", KavaApp.Configuration.StatusMessageTimeout);
        }

        // IMPORTANT: for browser COM CSE errors which can happen with script errors
        [HandleProcessCorruptedStateExceptions]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void PreviewTopic(bool keepScrollPosition = false, bool usePragmaLines = true,
            bool showInBrowser = false)
        {
            var topic = ActiveTopic;

            try
            {
                // only render if the preview is actually visible and rendering in Preview Browser
                if (!Window.Model.IsPreviewBrowserVisible)
                    return;

                if (ActiveTopic == null)
                    return;

                topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Preview;
                topic.TopicState.IsPreview = usePragmaLines;

                string renderedHtml = topic.RenderTopicToFile(addPragmaLines: usePragmaLines);

                topic.TopicState.IsPreview = false;
                topic.Project.ProjectSettings.ActiveRenderMode = HtmlRenderModes.Html;

                if (renderedHtml == null)
                {
                    Window.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Red, false);
                    Window.ShowStatus($"Access denied: {System.IO.Path.GetFileName(topic.RenderTopicFilename)}", 5000);
                    return;
                }

                string extracted = StringUtils.ExtractString(renderedHtml,
                    "<!-- Documentation Monster Content -->",
                    "<!-- End Documentation Monster Content -->");
                if (!string.IsNullOrEmpty(extracted))
                    renderedHtml = extracted;

                // if content contains <script> tags we must do a full page refresh
                bool forceRefresh = renderedHtml != null && renderedHtml.Contains("<script ");


                Window.PreviewMarkdown(keepScrollPosition: keepScrollPosition, showInBrowser: showInBrowser,
                    renderedHtml: renderedHtml);
            }
            catch (Exception ex)
            {
                //mmApp.Log("PreviewMarkdown failed (Exception captured - continuing)", ex);
                Debug.WriteLine("PreviewMarkdown failed (Exception captured - continuing)", ex);
            }
        }

        #endregion




        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
