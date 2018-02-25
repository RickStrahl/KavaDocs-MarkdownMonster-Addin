using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Model;
using DocHound.Utilities;
using NUnit.Framework;
using Westwind.Utilities;


namespace Westwind.HtmlHelpBuilder.Tests
{
    [TestFixture]
    public class FileSystemImporterTests
    {
        [Test]
        public void ImportWikiData()
        {
            string outputFolder = @"c:\temp\CodeFrameworkDocs_help";
            if (Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.Delete(outputFolder,true);
                }
                catch
                {
                }
            }

            var projectCreator = new DocProjectCreator
            {
                Company = "EPS Software",
                Filename = "_toc.json",
                ProjectFolder = outputFolder,                
                Title = "Code Framework Documentation",

                InstallFolder = "C:\\projects2010\\DocumentationMonster\\DocumentationMonster\\bin\\Debug"
            };

            var importer = new FileSystemImporter();
            Assert.True(importer.ImportFileSystem(@"C:\Temp\CODEFrameworkDocs", projectCreator));
        }
    }
}
