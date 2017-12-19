namespace SftpWrapper.Sdk.Models
{
    public class ConnectionInfo
    {
        public string UserName { get; }
        public string Password { get; }
        public string Host { get;}
        public int Port { get; }

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
