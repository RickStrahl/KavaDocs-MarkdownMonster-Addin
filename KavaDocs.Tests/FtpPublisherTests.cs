using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster;
using DocMonster.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReverseMarkdown;

namespace DocumentationMonster.Core.Tests
{
    [TestClass]
    public class FtpPublisherTests
    {

        [TestMethod]
        public async Task PublishProjectTest()
        {
            var project = DocProjectManager.Current.LoadProject(@"d:\temp\websurge3_project\_toc.json");
            project.Settings.Upload.Hostname = "west-wind.com";
            project.Settings.Upload.Username = "rstrahl";
            project.Settings.Upload.Password = Environment.GetEnvironmentVariable("FTP_PASSWORD");
            project.Settings.Upload.UploadFtpPath = "/Westwind_sysroot/Web Sites/WebSurgeX/docs/";
            Assert.IsNotNull(project, DocProjectManager.Current.ErrorMessage);

            var upload = new FtpPublisher(project);
            upload.StatusUpdate = (s =>
            {
                Console.WriteLine(s.IsError + " " + s.SourceFileInfo.FullName + " " + s.Message);
                return true;
            });
            var result = upload.UploadProject();


            Assert.IsTrue(result, upload.ErrorMessage);



        }



    }
}
