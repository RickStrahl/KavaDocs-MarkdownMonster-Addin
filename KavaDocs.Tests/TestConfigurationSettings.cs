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
        public string hbImportProjectFile = @"d:\Temp\markdownmonster_help\_toc_original.json";
        public string WebSurgeProjectFile = @"d:\temp\websurge3_project\_toc.json";
    }
}
