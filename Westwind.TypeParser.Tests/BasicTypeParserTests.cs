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



        [Test]
        public void GetMMTypesTest()
        {
            var parser = new TypeImporter.TypeParser() {ParseXmlDocumentation = true};
            var types = parser.GetAllTypes(assemblyPath: inputAssembly);

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0,"Count shouldn't be 0");

            RenderTypes(types);

        }

        [Test]
        public void GetTypesUtilitiesTest()
        {
            var parser = new TypeImporter.TypeParser() {ParseXmlDocumentation =  true};
            var types = parser.GetAllTypes(assemblyPath: @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\net45\Westwind.Utilities.dll");

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0, "Count shouldn't be 0");

            RenderTypes(types);
        }

        [Test]
        public void GetTypesNetCoreTest()
        {
            var parser = new TypeImporter.TypeParser() { ParseXmlDocumentation = true};
            var types = parser.GetAllTypes(assemblyPath: @"C:\projects2010\Westwind.Utilities\Westwind.Utilities\bin\Release\netStandard2.0\Westwind.Utilities.dll");

            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count > 0, "Count shouldn't be 0");

            foreach (var type in types)
            {
                Console.WriteLine($"{type} ");
            }
        }


        

        void RenderTypes(List<DotnetObject> types)
        {
            foreach (var type in types)
            {
                Console.WriteLine($"{type} -  - {type.Signature}");

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
                    foreach (var prop in type.Properties )
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

    }
}
