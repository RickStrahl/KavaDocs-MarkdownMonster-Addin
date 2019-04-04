using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocHound.Model;
using KavaDocsAddin.Controls;
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Annotations;
using MarkdownMonster.Windows;
using Microsoft.Win32;
using Westwind.TypeImporter;

namespace KavaDocsAddin.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for ImportDotnetLibraryDialog.xaml
    /// </summary>
    public partial class ImportDotnetLibraryDialog : MetroWindow
    {
        public ImportDotnetLibraryModel Model;

        private StatusBarHelper StatusBar { get; set; }

        public ImportDotnetLibraryDialog()
        {

            InitializeComponent();
            mmApp.SetThemeWindowOverride(this);

            Model = new ImportDotnetLibraryModel()
            {
                AppModel = mmApp.Model,
                AddinModel = kavaUi.AddinModel,
                ParentTopic = kavaUi.AddinModel.ActiveTopic
            };

            Model.AssemblyPath =
                @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net46\Westwind.Utilities.dll";

            DataContext = Model;

            TopicPicker.SelectTopic(Model.AddinModel.ActiveTopic);

            TopicPicker.TopicSelected = TopicSelected;

            StatusBar = new StatusBarHelper(StatusText, StatusIcon);

        }

        

        void TopicSelected(DocTopic topic)
        {
            Model.ParentTopic = topic;
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static object TopicsCollectionLock = new object();
        private async void Button_ImportClick(object sender, RoutedEventArgs e)
        {
            if (Model.ParentTopic == null)
            {
                StatusBar.ShowStatusError("Please select a Parent Topic first.");
                return;
            }

            // make sure we have the same reference
            var parentTopic = Model.AddinModel.ActiveProject.FindTopicInTreeByValue(Model.ParentTopic, Model.AddinModel.ActiveProject.Topics);

            StatusBar.ShowStatusProgress("Generating class documentation...",200_000_000);


            var importRootTopic = parentTopic.Copy();
            importRootTopic.Topics = new System.Collections.ObjectModel.ObservableCollection<DocTopic>();
            
            //BindingOperations.EnableCollectionSynchronization(Model.ParentTopic.Topics, TopicsCollectionLock);

            try
            {
                await Task.Run(() =>
                {
                    var parser = new DocHound.Importer.TypeTopicParser(Model.AddinModel.ActiveProject, importRootTopic)
                    {
                        NoInheritedMembers = Model.NoInheritedMembers,
                        ClassesToImport = Model.ClassList
                    };
                    parser.ParseAssembly(Model.AssemblyPath, importRootTopic);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            foreach (var topic in importRootTopic.Topics)
                parentTopic.Topics.Add(topic);

            StatusBar.ShowStatusProgress("Saving project...");
            Model.AddinModel.ActiveProject.SaveProject();

            // Force the 
            Model.AddinModel.TopicsTree.Model.OnPropertyChanged(nameof(TopicsTreeModel.TopicTree));

            StatusBar.ShowStatusSuccess("Class import completed.",5000);

            //var parser = new TypeParser() { ParseXmlDocumentation = true,
            //    NoInheritedMembers = Model.NoInheritedMembers,
            //    ClassesToImport = Model.ClassList

            //};


            //var types = parser.GetAllTypes(Model.AssemblyPath);





            //RenderTypes(types);
        }



        void RenderTypes(List<DotnetObject> types)
        {
            foreach (var type in types)
            {
                Console.WriteLine($"{type} - {type.Signature}");

                if (type.Constructors.Count > 0)
                {
                    Console.WriteLine("  *** Constructors:");
                    foreach (var meth in type.Constructors)
                    {
                        Console.WriteLine($"\t{meth} - {meth.Signature}");

                    }
                }

                if (type.Methods.Count > 0)
                {
                    Console.WriteLine("  *** Methods:");
                    foreach (var meth in type.Methods)
                    {
                        Console.WriteLine($"\t{meth}  - {meth.Signature}");
                        Console.WriteLine($"\t{meth.HelpText}");
                        foreach (var parm in meth.ParameterList)
                        {
                            Console.WriteLine($"\t\t{parm.ShortTypeName} {parm.Name}");
                        }
                    }
                }

                if (type.Properties.Count > 0)
                {
                    Console.WriteLine("  *** Properties:");
                    foreach (var prop in type.Properties)
                    {
                        Console.WriteLine($"\t{prop}  - {prop.Signature}");
                    }
                }

                if (type.Fields.Count > 0)
                {
                    Console.WriteLine("  *** Fields:");
                    foreach (var prop in type.Fields)
                    {
                        Console.WriteLine($"\t{prop}  - {prop.Signature}");
                    }
                }

                if (type.Events.Count > 0)
                {
                    Console.WriteLine("  *** Events:");
                    foreach (var ev in type.Events)
                    {
                        Console.WriteLine($"\t{ev}  - {ev.Signature}");
                    }
                }

            }
        }

        private void ButtonGetDirectory_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                DefaultExt = ".dll",
                Filter = "dll - .NET assembly  files (*.dll)|*.dll|" +
                         "All files (*.*)|*.*",
                CheckFileExists = true,
                RestoreDirectory = true,
                Multiselect = false,
                Title = "Open .NET Assembly File"
            };

            var result = openFileDialog.ShowDialog(this);
            if (!result.Value)
                return;

            Model.AssemblyPath = openFileDialog.FileName;
        }
    }

    public class ImportDotnetLibraryModel : INotifyPropertyChanged
    {
        public KavaDocsModel AddinModel { get; set; }

        public AppModel AppModel { get; set; }

        /// <summary>
        /// Topic under which the class or Namespaces are imported
        /// </summary>
        public DocTopic ParentTopic { get; set; }

        /// <summary>
        /// The path to the assembly on disk. Automatically picks up the
        /// XML documentation if it exists in the same location.
        /// </summary>
        public string AssemblyPath
        {
            get { return _AssemblyPath; }
            set
            {
                if (value == _AssemblyPath) return;
                _AssemblyPath = value;
                OnPropertyChanged(nameof(AssemblyPath));
            }
        }
        private string _AssemblyPath;


        /// <summary>
        /// Comma delimited list of classes that are to be imported from
        /// the .NET Assembly
        /// </summary>
        public string ClassList
        {
            get { return _classList; }
            set
            {
                if (value == _classList) return;
                _classList = value;
                OnPropertyChanged(nameof(ClassList));
            }
        }
        private string _classList;


        public bool Overwrite
        {
            get { return _Overwrite; }
            set
            {
                if (value == _Overwrite) return;
                _Overwrite = value;
                OnPropertyChanged(nameof(Overwrite));
            }
        }
        private bool _Overwrite;


        /// <summary>
        /// If true doesn't import any inherited members
        /// in the class structure
        /// </summary>
        public bool NoInheritedMembers
        {
            get { return _noInheritedMembers; }
            set
            {
                if (value == _noInheritedMembers) return;
                _noInheritedMembers = value;
                OnPropertyChanged(nameof(NoInheritedMembers));
            }
        }
        private bool _noInheritedMembers;


        public ImportModes ImportMode
        {
            get { return _ImportMode; }
            set
            {
                if (value == _ImportMode) return;
                _ImportMode = value;
                OnPropertyChanged(nameof(ImportMode));
            }
        }
        private ImportModes _ImportMode;


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ImportModes
    {
        PublicOnly,
        PublicAndInherited      
    }
}
