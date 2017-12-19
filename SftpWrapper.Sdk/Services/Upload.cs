using System;
using System.IO;
using System.Net.Sockets;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    public class Upload : IDisposable
    {
        readonly SftpClient _client;
        public bool UploadSuccess { get; set; }
        protected SftpFile File { get; set; }

        public Upload(Models.ConnectionInfo info, string sourcePath,
                     string destinationPath)
        {
            _client = new SftpClient(info.Host, info.Port, info.UserName,
                                     info.Password);
            File = new SftpFile(Valid(sourcePath), destinationPath);
        }

        public void UploadToSftp()
        {
            try
            {
                Connect();
                _client.ChangeDirectory(File.DestinationPath);
                using (var fs = new FileStream(File.SourcePath, FileMode.Open))
                {
                    _client.BufferSize = 4 * 1024;
                    _client.UploadFile(fs, File.Name);
                }
                UploadSuccess = _client.Exists($"{File.DestinationPath}{File.Name}");
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

        #region Private Methods

        void Connect()
        {
            _client.Connect();
        }

        void Disconnect()
        {
            _client.Disconnect();
        }

        string Valid(string sourcePath)
        {
            if (System.IO.File.Exists(sourcePath))
                return sourcePath;
            throw new ArgumentException("Invalid path or file not found.");
        }

        void DeleteSourceFolder()
        {
            if (!UploadSuccess) return;
            if (System.IO.File.Exists(File.SourcePath))
                System.IO.File.Delete(File.SourcePath);
        }

        #endregion

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            DeleteSourceFolder();
            Disconnect();
            if (!disposing) return;
            _client?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Upload()
        {
            Dispose(false);
        }

        #endregion
    }
}
