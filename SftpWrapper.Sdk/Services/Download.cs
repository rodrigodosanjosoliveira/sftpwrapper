using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    public class Download : IDisposable
    {
        private readonly SftpClient _client;
        public bool DownloadSuccess { get; set; }
        public SftpFileInfo File { get; set; }
        public List<SftpFileInfo> Files { get; set; }

        #region Public Methods

        public Download(Models.ConnectionInfo info)
        {
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();
        }

        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath)
        {
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();

            File = new SftpFileInfo(ValidPath(sourcePath, ref destinationPath), destinationPath);
        }

        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath, IEnumerable<string> validExtensions)
        {
            Files = new List<SftpFileInfo>();
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();
            var allFiles = _client.ListDirectory(sourcePath).ToList().OrderByDescending(f => f.LastWriteTime).ToList();
            var filePaths = new List<string>();

            foreach (var extension in validExtensions)
            {
                filePaths.AddRange(allFiles.Where(f => f.Name.EndsWith(extension)).Select(f => f.FullName).ToList());
            }

            foreach (var filePath in filePaths)
            {
                Files.Add(new SftpFileInfo(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath))));
            }
        }

        public void DownloadFromSftp()
        {
            try
            {
                using (var fs = System.IO.File.OpenWrite(File.DestinationPath))
                {
                    _client.DownloadFile(File.SourcePath, fs);
                }
                DownloadSuccess = System.IO.File.Exists($"{File.DestinationPath}");
                if (DownloadSuccess)
                    DeleteSourceFolder(File);
            }
            catch (InvalidOperationException ioe)
            {
                throw new InvalidOperationException(ioe.Message, ioe.InnerException);
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
            catch (SshConnectionException sce)
            {
                throw new SshConnectionException(sce.Message);
            }
            catch (SshAuthenticationException sae)
            {
                throw new SshAuthenticationException(sae.Message, sae.InnerException);
            }
        }

        public void DownloadManyFromSftp()
        {
            try
            {
                if (!Files.Any())
                    throw new SftpPathNotFoundException("No files found to download.");

                foreach (var file in Files)
                {
                    using (var fs = System.IO.File.OpenWrite(file.DestinationPath))
                    {
                        _client.DownloadFile(file.SourcePath, fs);
                    }

                    DownloadSuccess = System.IO.File.Exists($"{file.DestinationPath}");
                    if (DownloadSuccess)
                        DeleteSourceFolder(file);
                }

            }
            catch (InvalidOperationException ioe)
            {
                throw new InvalidOperationException(ioe.Message, ioe.InnerException);
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
            catch (SshConnectionException sce)
            {
                throw new SshConnectionException(sce.Message);
            }
            catch (SshAuthenticationException sae)
            {
                throw new SshAuthenticationException(sae.Message, sae.InnerException);
            }
            catch (ApplicationException ape)
            {
                throw new ApplicationException(ape.Message);
            }
        }

        public string GetFileName()
        {
            try
            {
                var files = _client.ListDirectory(File.SourcePath);
                var sftpFiles = files.ToList();
                var l = sftpFiles.FirstOrDefault(sf => !sf.Name.StartsWith("."));

                return l?.Name;
            }
            catch (ArgumentNullException ane)
            {
                throw new ArgumentNullException(ane.ParamName, ane.Message);
            }
            catch (SshConnectionException sce)
            {
                throw new SshConnectionException(sce.Message, sce.DisconnectReason);
            }
            catch (SftpPermissionDeniedException spde)
            {
                throw new SftpPermissionDeniedException(spde.Message, spde.InnerException);
            }
            catch (SshException se)
            {
                throw new SshException(se.Message, se.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #endregion

        
        #region Private Methods

        private string ValidPath(string sourcePath, ref string destinationPath)
        {
            var files = _client.ListDirectory(sourcePath).Where(f => !f.Name.StartsWith(".")).ToList();
            if (!files.Any()) throw new SftpPathNotFoundException("File not found.");
            if (!_client.Exists(Path.Combine(sourcePath, files.First().Name)))
                throw new SftpPathNotFoundException("File not found.");
            destinationPath = Path.Combine(destinationPath, files.First().Name);
            return Path.Combine(sourcePath, files.First().Name);

        }

        private void DeleteSourceFolder(SftpFileInfo file)
        {
            if (!DownloadSuccess) return;
            if (!_client.Exists(file.SourcePath)) return;
            try
            {
                _client.Delete(file.SourcePath);
            }
            catch (ArgumentNullException ane)
            {
                throw new ArgumentNullException(ane.ParamName, ane.InnerException);
            }
            catch (SshConnectionException sce)
            {
                throw new SshConnectionException(sce.Message, sce.DisconnectReason, sce.InnerException);
            }
            catch (SftpPathNotFoundException spnfe)
            {
                throw new SftpPathNotFoundException(spnfe.Message, spnfe.InnerException);
            }
            catch (ObjectDisposedException ode)
            {
                throw new ObjectDisposedException(ode.ObjectName, ode.InnerException);
            }
        }

        private void Disconnect()
        {
            if (_client != null && _client.IsConnected)
                _client.Disconnect();
        }

        #endregion


        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            Disconnect();
            if (!disposing) return;
            _client?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Download()
        {
            Dispose(false);
        }

        #endregion
    }
}