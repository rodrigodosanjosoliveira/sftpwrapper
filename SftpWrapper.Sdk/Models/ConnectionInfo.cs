namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Represents remote connection information class.
    /// </summary>
    public class ConnectionInfo
    {
        public string UserName { get; }
        public string Password { get; }
        public string Host { get;}
        public int Port { get; }

        /// <summary>
        /// Initializes a new instance of the SftpWrapper.Sdk.Models.ConnectionInfo
        /// </summary>
        /// <param name="username">Authentication username.</param>
        /// <param name="password">Authentication password.</param>
        /// <param name="host">Connection host.</param>
        /// <param name="port">Connection port</param>
        public ConnectionInfo(string username, string password, string host, 
                             int port = 22)
        {
            UserName = username;
            Password = password;
            Host = host;
            Port = port;
        }
    }
}
