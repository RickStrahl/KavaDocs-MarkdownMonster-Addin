using System;
using System.Collections.Generic;
using NUnit.Framework;
using Westwind.TypeImporter;

namespace Westwind.TypeParser.Tests
{
    [TestFixture]
    public class BasicTypeParserTests
    {
        string outputFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "_TopicsFileText.json");
        string inputAssembly = @"C:\projects2010\MarkdownMonster\MarkdownMonster\bin\Release\MarkdownMonster.exe";

        private string wwutilsAssembly =
            @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net46\Westwind.Utilities.dll";


        [Test]
        public void BasicParsingWithoutMembersTest()
        {
            var parser = new TypeImporter.TypeParser()
            {
                ParseXmlDocumentation = true,
                NoInheritedMembers = true,
                AssemblyFilename = wwutilsAssembly
            };
            var types = parser.GetAllTypes(assemblyPath: wwutilsAssembly);

            Assert.IsNotNull(types,parser.ErrorMessage);
            Assert.Greater(types.Count,0,"Count shouldn't be 0");

            RenderTypes(types);

        }

        [Test]
        public void GetTypesWithMembersTest()
        {
            var parser = new TypeImporter.TypeParser()
            {
                ParseXmlDocumentation = true,
                NoInheritedMembers = false,
                AssemblyFilename = wwutilsAssembly
            };
            var types = parser.GetAllTypes(assemblyPath:  wwutilsAssembly  );

            Assert.IsNotNull(types,parser.ErrorMessage);
            Assert.Greater(types.Count,0, "Count shouldn't be 0");

            RenderTypes(types);
        }

        [Test]
        public void GetTypesNetCoreTest()
        {
            var parser = new TypeImporter.TypeParser() { ParseXmlDocumentation = true};
            var types = parser.GetAllTypes(assemblyPath: wwutilsAssembly);

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0, "Count shouldn't be 0");

            foreach (var type in types)
            {
                Console.WriteLine($"{type} ");
            }
        }


        [Test]
        public void GetConfigurationPropertiesTest()
        {
            var appConfigType = typeof(MarkdownMonster.ApplicationConfiguration);

            var typeParser = new TypeImporter.TypeParser()
            {
                ParseXmlDocumentation = true,
            };
            var dotnetObject = typeParser.ParseObject(appConfigType);

            var list = new List<DotnetObject>();
            list.Add(dotnetObject);

            RenderTypes(list);
        }

        

        void RenderTypes(List<DotnetObject> types)
        {
            foreach (var type in types)
            {
                Console.WriteLine($"{type} -  - {type.Signature}");


                if (type.Properties.Count > 0)
                {
                    Console.WriteLine("  *** Properties:");
                    foreach (var prop in type.Properties )
                    {
                        Console.WriteLine($"\t{prop}  - {prop.Signature}");
                        Console.WriteLine($"\t{prop.HelpText}");
                        Console.WriteLine();
                    }
                }


            }
        }

    }
}
