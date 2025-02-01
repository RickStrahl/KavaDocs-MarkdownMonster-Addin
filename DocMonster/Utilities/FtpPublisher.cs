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

        public bool IsCancelled { get; set; }

        /// <summary>
        /// Delete any files that are in the target folder but
        /// are not updated.
        /// </summary>
        public bool DeleteExtraFiles { get; set; }

        public List<FtpFileProgress> Errors { get; set; } = new List<FtpFileProgress>();

        public FtpPublisher(DocProject project)
        {
            Project = project;
            InitializeFtpClient(project.Settings.Upload);
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
                Timeout = 7000,                
            };
            DeleteExtraFiles = settings.DeleteExtraFiles;            

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
            IsCancelled = false;
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
                var progress = new FtpFileProgress
                {
                    IsError = false,
                    Message = "Consolidating local and server files.",
                    MessageType = UploadMessageTypes.Message
                };
                StatusUpdate?.Invoke(progress);
                var unchangedFiles = DiffAgainstOnlineFiles(files);

                if (unchangedFiles.Length < 1) 
                    return true;

                if (IsCancelled)
                {
                    SetError("Publishing has been cancelled.");
                    return false;
                }

                var totalFiles = unchangedFiles.Length;
                var totalBytes = unchangedFiles.Sum((f) => f.Length);
                int count = 0;
                long bytesSent = 0;
                foreach (var file in unchangedFiles) //.OrderBy(f => f.FullName))
                {
                    if (IsCancelled)
                    {
                        SetError("Publishing has been cancelled.");
                        return false;
                    }

                    var fname = file.FullName;
                    var ftpName = FileUtils.GetRelativePath(fname, Project.OutputDirectory).Replace("\\", "/");
                    ftpName = StringUtils.TerminateString(targetBasePath, "/") + ftpName.TrimStart('/');
                   
                    var result = Ftp.UploadFile(fname, ftpName);
                    

                    bytesSent += file.Length;

                    progress = new FtpFileProgress
                    {
                        IsError = result,                        
                        Message = result ? Ftp.ErrorMessage : null,
                        MessageType = !result ? UploadMessageTypes.Error : UploadMessageTypes.Progress,
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
                if (IsCancelled)
                    return [];

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

                if (DeleteExtraFiles)
                {
                    foreach (var file in ftpFiles)
                    {
                        if (file.IsDirectory) continue;                        

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

    public enum UploadMessageTypes
    {
        Message,
        Progress,        
        Error
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
        public string Message { get; set; }

        public UploadMessageTypes MessageType { get; set; } = UploadMessageTypes.Message;

    }
}
