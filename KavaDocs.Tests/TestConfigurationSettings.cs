using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentationMonster.Core.Tests
{
    public class TestConfiguration
    {
        public static TestConfiguration Current = new TestConfiguration();

        public PathConfiguration Paths = new PathConfiguration();


    }

    public class PathConfiguration
    {
        public string hbImportProjectFile = @"C:\Temp\markdownmonster_help\_toc_original.json";
        public string projectFile = @"C:\Temp\markdownmonster_help\_kavadocs-project.json";
    }
}
