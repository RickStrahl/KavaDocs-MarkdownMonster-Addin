using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocMonster.Ftp;
using DocMonster.Model;
using FluentFTP;
using MarkdownMonster;
using Westwind.Utilities;


namespace DocMonster.Utilities
{
    public class FtpPublisher
    {
        public DocProject Project { get; }

        public wwFtpClient Ftp { get; set; }

        /// <summary>
        /// Status update callback. Return true to continue, false to cancel
        ///
        /// If you use UI in this function make sure to apply a dispatcher to
        /// marshal to the UI thread.
        /// </summary>
        public Func<FtpFileProgress, bool> StatusUpdate { get; set; }

        public bool CancelOperation { get; set; }

        /// <summary>
        /// Delete any files that are in the target folder but
        /// are not updated.
        /// </summary>
        public bool DeleteOldFiles { get; set; }

        public List<FtpFileProgress> Errors { get; set; } = new List<FtpFileProgress>();

        public FtpPublisher(DocProject project)
        {
            Project = project;
            InitializeFtpClient(project.Settings.Upload);
        }


        public bool InitializeFtpClient(string ipOrDomain, int port = 0, string username = null, string password = null, bool useSsl = false)
        {
            Ftp = new wwFtpClient()
            {
                Hostname = ipOrDomain,
                Port = port,
                Username = username,
                Password = password,
                UseTls = useSsl,
                IgnoreCertificateErrors = true,
                Timeout = 7000,
                CreateRemoteDirectories = true
            };

            return true;
        }

        public bool InitializeFtpClient(UploadSettings settings)
        {
            Ftp = new wwFtpClient()
            {
                Hostname = settings.Hostname,
                Username = settings.Username,
                Password = settings.Password,
                UseTls = settings.UseTls,
                IgnoreCertificateErrors = true,
                Timeout = 7000
            };

            return true;
        }

        public async Task<bool> UploadProjectAsync(string sourceFolder = null, string targetBasePath = null)
        {
            return await Task.Run( ()=> UploadProject(sourceFolder, targetBasePath));
            
        }


        /// <summary>
        /// Uploads all files from the project directory to the server
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="targetBasePath"></param>
        /// <returns>Returns a list of non-sucessful upload items or an empty collection.</returns>
        public bool UploadProject(string sourceFolder = null, string targetBasePath = null)
        {
            CancelOperation = false;
            Errors = new List<FtpFileProgress>();

            if (string.IsNullOrEmpty(sourceFolder))
                sourceFolder = Project.OutputDirectory;
            if (string.IsNullOrEmpty(targetBasePath))
                targetBasePath = Project.Settings.Upload.UploadFtpPath;

            if (Ftp == null)
            {
                InitializeFtpClient(Project.Settings.Upload);
            }

            using (Ftp)
            {

                if (Ftp.Connect() == null)
                {
                    SetError(Ftp.ErrorMessage);
                    return false;
                }

                var dirInfo = new DirectoryInfo(Project.OutputDirectory);
                if (!dirInfo.Exists)
                    return false;

                var files = dirInfo.GetFiles("*.*", new EnumerationOptions {
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 9999,
                    AttributesToSkip = FileAttributes.Hidden 
                });

                var unchangedFiles = DiffAgainstOnlineFiles(files);

                if (files.Length > 0) 
                    return true;

                var totalFiles = files.Length;
                var totalBytes = files.Sum((f) => f.Length);
                int count = 0;
                long bytesSent = 0;
                foreach (var file in files) //.OrderBy(f => f.FullName))
                {
                    var fname = file.FullName;
                    var ftpName = FileUtils.GetRelativePath(fname, Project.OutputDirectory).Replace("\\", "/");
                    ftpName = StringUtils.TerminateString(targetBasePath, "/") + ftpName.TrimStart('/');
                   
                    //var result = Ftp.UploadFile(fname, ftpName);
                    var result = true;

                    bytesSent += file.Length;

                    var progress = new FtpFileProgress
                    {
                        IsError = result,
                        ErrorMessage = result ? Ftp.ErrorMessage : null,
                        SourceFileInfo = file,
                        UploadFtpPath = ftpName,
                        TotalFiles = totalFiles,
                        FilesSent = count++,
                        TotalBytes = totalBytes,
                        BytesSent = bytesSent
                    };

                    StatusUpdate?.Invoke(progress);

                    if (progress.IsError)
                        Errors.Add(progress);
                }
            }

            return true;
        }

        public FileInfo[] DiffAgainstOnlineFiles(FileInfo[] files)
        {
            var updateList = new List<FileInfo>();

            var directories = files.Select( fi => System.IO.Path.GetDirectoryName(fi.FullName)).Distinct().ToList();
            foreach (var dir in directories)
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists) continue;

                var relativePath = FileUtils.GetRelativePath(dir, Project.OutputDirectory);
                if (relativePath.StartsWith(".."))
                    relativePath = string.Empty;
                var ftpPath = StringUtils.TerminateString(Project.Settings.Upload.UploadFtpPath, "/") + relativePath.TrimStart('/').Replace("\\", "/");

                var ftpFiles = Ftp.ListFiles(ftpPath);
                if (ftpFiles == null)
                    continue;

                var dirFiles = di.GetFiles();
                foreach (var fi in dirFiles)
                {
                    var ftpFile = ftpFiles.FirstOrDefault(ftp => ftp.Name == fi.Name);
                    if (ftpFile == null)
                    {
                        // doesn't exist just add
                        updateList.Add(fi);
                        continue;
                    }

                    if (fi.Length == ftpFile.Length)
                        continue;

                    updateList.Add(fi);
                }

                if (DeleteOldFiles)
                {
                    foreach (var file in ftpFiles)
                    {
                        var localFile = dirFiles.FirstOrDefault(f => f.Name == file.Name);
                        if (localFile == null)
                        {
                            Ftp.DeleteFile(file.FullName);
                        }
                    }
                }
            }

            return updateList.ToArray();    
        }



        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
            {
                ErrorMessage = string.Empty;
            }
            else
            {
                Exception e = ex;
                if (checkInner)
                    e = e.GetBaseException();

                ErrorMessage = e.Message;
            }
        }


    }

    public class FtpFileProgress
    {
        public FileInfo SourceFileInfo { get; set; }

        public string SourceFile => SourceFileInfo.FullName;


        public string UploadFtpPath { get; set; }
        public long BytesTransferred { get; set; }
        public int PercentComplete { get; set; }
        public bool IsComplete { get; set; }

        public int TotalFiles { get; set; }
        public int FilesSent { get; set; }
        public long TotalBytes { get; set; }
        public long BytesSent { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        
    }
}
