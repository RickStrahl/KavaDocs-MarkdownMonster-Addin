
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using FluentFTP;
using FluentFTP.Helpers;
using FluentFTP.Proxy.SyncProxy;
using File = System.IO.File;


namespace DocMonster.Ftp
{
    public class wwFtpClient : IDisposable
    {
        public string Hostname { get; set; }
        public int Port { get; set; } = 21;
        public bool UseTls { get; set; }

        public bool IgnoreCertificateErrors { get; set; }

        public string Username { get; set; }
        
        [JsonIgnore]
        public string Password { get; set; }

        /// <summary>
        /// Timeout in milliseconds
        /// </summary>
        public int Timeout { get; set; } = 10000;

        public string ErrorMessage { get; set; }
        public string LogFile { get; set;  }

        public bool EnableAsciiMode { get; set; } 

        public string ProxyHostname { get; set; }

        public int ProxyPort { get; set; } = 1080;

        /// <summary>
        /// If true creates remote directories when uploading files
        /// to folders that don't exist.
        /// </summary>
        public bool CreateRemoteDirectories { get; set; }   

        /// <summary>
        /// Pass the FoxPro wwFtp instance for progress updates
        /// </summary>
        public dynamic FoxProwwFtpClientForProgress { get; set; }

        public FtpClient FtpClient { get; set; }

        #region Initializion

        public wwFtpClient()
        {                       
        }


        public void Dispose()
        {
            Close();
        }
        #endregion

        #region Connect Disconnect

        /// <summary>
        /// Call this method before any other operations
        /// </summary>
        /// <returns></returns>
        public FtpClient Connect()
        {
            ErrorMessage = null;

            if (FtpClient != null)
                return FtpClient;            
                        
            try
            {
                FtpClient client;
                if (!string.IsNullOrEmpty(ProxyHostname))
                {
                    var profile = new FtpProxyProfile()
                    {
                        ProxyHost = ProxyHostname,
                        ProxyPort = ProxyPort
                    };                  
                    client = new FtpClientSocks4aProxy(profile);
                }
                else                
                    client = new FtpClient();
                                    
                if (!string.IsNullOrEmpty(Username))
                    client.Credentials = new NetworkCredential(Username, Password);

                client.Config.RetryAttempts = 2;

                if (EnableAsciiMode) 
                {
                    client.Config.DownloadDataType = FtpDataType.ASCII;
                    client.Config.UploadDataType = FtpDataType.ASCII;   
                }

                if (UseTls)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                    client.Config.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
                }                
                client.ValidateCertificate += new FtpSslValidation((control, e) =>
                {                    
                    if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        if (!IgnoreCertificateErrors)
                        {
                            e.Accept = false;
                            ErrorMessage = "The Certificate is invalid or missing. " + e.PolicyErrorMessage;
                        }
                        else
                        {
                            // ignore Cert error - not a good idea in production
                            e.Accept = true;
                        }
                    }
                    else
                    {
                        e.Accept = true;
                    }
                });

                if (!string.IsNullOrEmpty(LogFile))
                {
                    //client.Config.LogToConsole = true;
                    client.Logger = new FtpFileLogger(LogFile);                    
                }
                client.Config.ConnectTimeout = Timeout ;
                client.Config.ReadTimeout = Timeout ;


                if (Hostname.Contains(":"))
                {
                    var parts = Hostname.Split(':');
                    Hostname = parts[0];
                    Port = int.Parse(parts[1]);
                }
                client.Host = Hostname;
                client.Port = Port;

                client.Connect();

                FtpClient = client;                
                return client;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                    ErrorMessage = ex.Message;

                FtpClient = null;
                return null;
            }

        }

        /// <summary>
        /// Adds a client Certificate to the connection
        /// 
        /// Must be called Before call to connect
        /// </summary>
        /// <param name="certificateFile"></param>
        /// <returns></returns>
        public bool AddCertificate(string certificateFile)
        {
            if (!File.Exists(certificateFile))
            {
                ErrorMessage = "Certificate file not found.";
                return false;
            }

            try
            {
                FtpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                //FtpClient.Config.SocketKeepAlive = false;
                FtpClient.Config.ClientCertificates.Add(new X509Certificate(certificateFile));
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to import certificate file: " + ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Make sure to call this method to shut down the connection
        /// </summary>
        public void Close()
        {            
            if (FtpClient != null)
            {
                try
                {
                    if (FtpClient.IsConnected)
                    {
                        FtpClient.Disconnect();
                        FtpClient.Dispose();                        
                    }
                }
                catch
                {
                }
                FtpClient = null;
            }
        }



        #endregion



        #region File Operations

        /// <summary>
        /// High level do everything download function.
        /// Make sure you call Connect() before you call
        /// this function.
        /// </summary>
        /// <param name="remoteFilename"></param>
        /// <param name="localFilename"></param>
        /// <returns></returns>
        public bool DownloadFile(string remoteFilename, string localFilename)
        {

            if (FtpClient == null)
            {
                if (Connect() == null)
                    return false;
            }

            try
            {                
                System.IO.File.Delete(localFilename);
                var status = FtpClient.DownloadFile(localFilename, remoteFilename, verifyOptions: FtpVerify.Retry | FtpVerify.Throw,  progress: DownloadProgress );
                return status.IsSuccess();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

        }

        /// <summary>
        /// Upload a file from the local machine to the server
        /// </summary>
        /// <param name="localFilename"></param>
        /// <param name="remoteFilename"></param>
        /// <returns></returns>
        public bool UploadFile(string localFilename, string remoteFilename)
        {
            if (string.IsNullOrEmpty(localFilename))
            {
                ErrorMessage = "Local Filename is required";
                return false;
            }

            localFilename = Path.GetFullPath(localFilename);
            if (!File.Exists(localFilename))
            {
                ErrorMessage = "Local file does not exist.";
                return false;
            }

            if (FtpClient == null)
            {
                if (Connect() == null)
                    return false;
            }

            try
            {
                var status = FtpClient.UploadFile(localFilename, remoteFilename, verifyOptions: FtpVerify.Retry | FtpVerify.Throw, progress: UploadProgress, createRemoteDir: true);
                return status.IsSuccess();
                
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.GetBaseException().Message;
                return false;
            }
        }

        void DownloadProgress(FtpProgress progress)
        {
            if (FoxProwwFtpClientForProgress != null)
            {
                FoxProwwFtpClientForProgress.OnFtpBufferUpdate(Convert.ToInt32(progress.Progress), (int)progress.TransferredBytes, progress.RemotePath, "download");                
            }
        }

        void UploadProgress(FtpProgress progress)
        {
            if (FoxProwwFtpClientForProgress != null)
            {
                FoxProwwFtpClientForProgress.OnFtpBufferUpdate(Convert.ToInt32(progress.Progress), (int)progress.TransferredBytes, progress.RemotePath, "upload");                
            }
        }


        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="remoteFilename"></param>
        /// <returns></returns>
        public bool DeleteFile(string remoteFilename)
        {
            try
            {
                FtpClient.DeleteFile(remoteFilename);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Renames a file on the server. Specify old and new path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool RenameFile(string source, string target)
        {
            try
            {
                FtpClient.MoveFile(source, target);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            return true;
        }

        #endregion

        #region Directory Operations


        /// <summary>
        /// Get a directory listing of files 
        /// 
        /// returns object list (Name, FullName, Length, LastWriteTime)
        /// 
        /// Make sure to call Connect() before calling this method
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<FtpFile> ListFiles(string path)
        {
            try
            {                
                var files = FtpClient.GetListing(path);

                var fileList = new List<FtpFile>();
                foreach (var sfile in files)
                {
                    fileList.Add(new FtpFile()
                    {
                        FullName = sfile.FullName,
                        Name = sfile.Name,
                        Length = Convert.ToInt32(sfile.Size),
                        LastWriteTime = sfile.Modified,
                        IsDirectory = sfile.Type == FtpObjectType.Directory
                    });
                }

                return fileList;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Creates a new directory on the server
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public bool CreateDirectory(string remotePath)
        {
            try
            {
                FtpClient.CreateDirectory(remotePath, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }


        /// <summary>
        /// Deletes a directory on the server
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public bool DeleteDirectory(string remotePath)
        {
            try
            {
                FtpClient.DeleteDirectory(remotePath);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }


        /// <summary>
        /// Changes the remote path 
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public bool ChangeDirectory(string remotePath)
        {
            try
            {
                FtpClient.SetWorkingDirectory(remotePath);
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }            
        }

        /// <summary>
        /// Changes the remote path 
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string GetDirectory(string remotePath)
        {
            try
            {
                return FtpClient.GetWorkingDirectory();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }


        }

        /// <summary>
        /// Checks to see if a file exists
        /// </summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public bool Exists(string remotePath)
        {
            try
            {
                ErrorMessage = null;
                return  FtpClient.FileExists(remotePath);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }
        #endregion

        #region FTP Commands

        public bool ExecuteCommand(string command)
        {
            try
            {
                FtpClient.Execute(command);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }

        public bool ExecuteDownloadCommand(string command)
        {
            try
            {
                FtpClient.ExecuteDownloadText(command);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            return true;
        }

        #endregion
    }

    public class FtpLogger : IFtpLogger
    {
        readonly StringBuilder _LogText = new StringBuilder();
        bool _logConsole = false;
        public FtpLogger()
        {
            
        }

        public FtpLogger(bool logConsole)
        {
            _logConsole = logConsole;
        }
        public void Log(FtpLogEntry entry)
        {
            _LogText.AppendLine( DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " - "  + entry.Severity + " - " +  entry.Message);                        
        }

        public string GetLogText()
        {
            return _LogText.ToString();
        }

        public void Clear()
        {
            _LogText.Clear();
        }
    }


    /// <summary>
    /// Logger used for SFTP logging which doesn't support internal file logging,
    /// so we manually log for each operation.
    /// </summary>

    public class FtpFileLogger : IFtpLogger
    {
        string _filename { get; set; }
        bool _logConsole = false;

        public FtpFileLogger(string filename)
        {
            _filename = filename;
        }

        
        public void Log(FtpLogEntry entry)
        {
            try
            {
                File.AppendAllText(_filename, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} - {entry.Severity} - {entry.Message}\n");                
            }
            catch 
            {
                throw new Exception("Error writing to log file: " + _filename);
            }
        }

    }


    public class FtpFile
    {
        public string FullName { get; set; }
        public string Name { get; set; }

        public int Length { get; set; }

        public DateTime LastWriteTime { get; set; }

        ///// <summary>
        ///// 16 - directory, 128 - file. Match WinInet convention
        ///// </summary>
        //public int FileAttribute { get; set; }

        public bool IsDirectory { get; set; }

        public override string ToString() => $"{FullName}";
    }
}
