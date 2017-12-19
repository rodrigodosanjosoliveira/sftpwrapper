using System;
using System.Net.Sockets;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    public class Download : IDisposable
    {
        readonly SftpClient _client;
        public bool DownloadSuccess {get; set;}
        protected SftpFile File {get; set;}

        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath)
        {
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();
            File = new SftpFile(Valid(sourcePath), destinationPath);
        }

        string Valid(string sourcePath)
        {
            if(_client.Exists(sourcePath))
                return sourcePath;
            
            throw new SftpPathNotFoundException("File not found.");
        }
        
        #region Private Methods

        private void DeleteSourceFolder()
        {
            if (!DownloadSuccess) return;
            if (!_client.Exists(File.SourcePath)) return;
            try
            {
                _client.Delete(File.SourcePath);
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
        
        void Connect()
        {
            _client.Connect();
        }

        void Disconnect()
        {
            if(_client != null && _client.IsConnected)
                _client.Disconnect();
        }

        #endregion

        public void DownloadFromSftp()
        {
            try
            {
                using(var fs = System.IO.File.OpenWrite(File.DestinationPath))
                {
                    _client.DownloadFile(File.SourcePath, fs);
                }
                DownloadSuccess = System.IO.File.Exists($"{File.DestinationPath}");
                if(DownloadSuccess)
                    DeleteSourceFolder();
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