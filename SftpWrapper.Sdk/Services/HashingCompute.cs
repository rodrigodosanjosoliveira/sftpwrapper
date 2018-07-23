using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SftpWrapper.Sdk.Services
{
    public class HashingCompute
    {
        public static string GetMd5HashFromFile(string fileName)
        {
            var file = new FileStream(fileName, FileMode.Open);
            var md5 = new MD5CryptoServiceProvider();
            var retVal = md5.ComputeHash(file);
            file.Close();

            var sb = new StringBuilder();
            foreach (var t in retVal)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string GetSha1HashFromFile(string fileName)
        {
            var file = new FileStream(fileName, FileMode.Open);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var retVal = sha1.ComputeHash(file);
            file.Close();

            var sb = new StringBuilder();
            foreach (var t in retVal)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
