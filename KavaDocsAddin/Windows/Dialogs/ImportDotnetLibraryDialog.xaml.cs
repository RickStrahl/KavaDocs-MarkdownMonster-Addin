using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MahApps.Metro.Controls;
using MarkdownMonster;
using MarkdownMonster.Annotations;
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

        public ImportDotnetLibraryDialog()
        {

            InitializeComponent();

            Model = new ImportDotnetLibraryModel()
            {
                AppModel = mmApp.Model,
                AddinModel = kavaUi.AddinModel
            };

            DataContext = Model;
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_ImportClick(object sender, RoutedEventArgs e)
        {
            var parser = new TypeParser() { ParseXmlDocumentation = true };



            var types = parser.GetAllTypes(assemblyPath: @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net45\Westwind.Utilities.dll");

          

            RenderTypes(types);
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
