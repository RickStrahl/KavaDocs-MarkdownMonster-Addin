using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Westwind.TypeParser.Tests
{
    [TestFixture]
    public class BasicTypeParserTests
    {
        string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");
        string inputAssembly = @"C:\projects2010\MarkdownMonster\MarkdownMonster\bin\Release\MarkdownMonster.exe";

        //[Test]
        //public void GetClassicTypesTest()
        //{
        //    var importer = new TypeImporter.TypeParserLegacy();
        //    importer.Filename = inputAssembly;
        //    importer.RetrieveDeclaredMembersOnly = true;

        //    int typeCount = importer.GetAllObjects();

        //    Assert.IsTrue(typeCount > 0);

        //}

        [Test]
        public void GetMMTypesTest()
        {
            var parser = new TypeImporter.TypeParser();
            var types = parser.GetAllTypes(assemblyPath: inputAssembly);

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0,"Count shouldn't be 0");

            foreach (var type in types)
            {
                Console.WriteLine($"{type}");
            }
           
        }

        [Test]
        public void GetTypesUtilitiesTest()
        {
            var parser = new TypeImporter.TypeParser();
            var types = parser.GetAllTypes(assemblyPath: @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net45\Westwind.Utilities.dll");

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0, "Count shouldn't be 0");
            
            foreach (var type in types)
            {
                Console.WriteLine($"{type}");
            }
        }

        [Test]
        public void GetTypesNetCoreTest()
        {
            var parser = new TypeImporter.TypeParser();
            var types = parser.GetAllTypes(assemblyPath: @"C:\projects2010\Westwind.AspNetCore\Westwind.AspNetCore\bin\Release\netstandard2.0\Westwind.AspNetCore.dll");

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0, "Count shouldn't be 0");

            foreach (var type in types)
            {
                Console.WriteLine($"{type} ");
            }
        }
    }
}
