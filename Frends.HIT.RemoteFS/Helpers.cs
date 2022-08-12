using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using FluentFTP;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Frends.HIT.RemoteFS;

class Helpers
{
    /// <summary>
    /// Get an exact match from a list of strings and a search input
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetExactMatch(List<string> input, string filter)
    {
        return input.Where(x => x == filter).ToList();
    }
    
    /// <summary>
    /// Get the items that contain a given string in a list of strings
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetContainsMatch(List<string> input, string filter)
    {
        return input.Where(x => x.Contains(filter)).ToList();
    }
    
    /// <summary>
    /// Get the items that contain a given string (with wildcards) in a list of strings
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetWildcardMatch(List<string> input, string filter)
    {
        filter = Regex.Escape(filter).Replace("\\*", ".*");
        return GetRegexMatch(input, $"^{filter}$");
    }
    
    /// <summary>
    /// Get the items that match a regular expression in a list of strings
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetRegexMatch(List<string> input, string filter)
    {
        return input.Where(x => Regex.IsMatch(x, filter)).ToList();
    }

    /// <summary>
    /// Filter a list of strings according to a given filter type and filter
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <param name="filterType"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetMatchingFiles(List<string> input, string filter, FilterTypes filterType)
    {
        switch (filterType)
        {
            case FilterTypes.Exact:
                return GetExactMatch(input, filter);
            case FilterTypes.Contains:
                return GetContainsMatch(input, filter);
            case FilterTypes.Wildcard:
                return GetWildcardMatch(input, filter);
            case FilterTypes.Regex:
                return GetRegexMatch(input, filter);
            default:
                return input;
        }
    }

    /// <summary>
    /// Checks if the given string is a valid, non-empty/null string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsValidString(string input)
    {
        return !string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input) && input.Length > 0;
    }
    
    /// <summary>
    /// Get a connection string for SMB connections
    /// </summary>
    /// <param name="server"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="domain"></param>
    /// <param name="path"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public static string GetSMBConnectionString(ServerConfiguration connection, string path, string file)
    {
        
        var sb = new StringBuilder("smb://");

        path = path.Replace("\\", "/");
        file = file.Replace("\\", "/");
        
        if (IsValidString(connection.Domain))
        {
            sb.Append($"{connection.Domain};");
        }

        if (IsValidString(connection.Username))
        {
            sb.Append($"{connection.Username}");
            
            if (IsValidString(connection.Password))
            {
                sb.Append($":{connection.Password}");
            }
            
            sb.Append("@");
        }
        
        sb.Append(connection.Address);

        string actualPath = JoinPath("/", path, file); 
                
        if (!path.StartsWith("/"))
        {
            sb.Append("/");
        }
        
        sb.Append(actualPath);
        return sb.ToString();
    }
    
    /// <summary>
    /// Saves a temporary copy of the private key string to file for usage
    /// </summary>
    /// <param name="key">The private key</param>
    /// <returns>The path to the private key</returns>
    private static String SaveTemporaryFile(String key)
    {
        var decodedKey = Convert.FromBase64String(key);
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, Encoding.UTF8.GetString(decodedKey));
        return tempFile;
    }
    
    /// <summary>
    /// Retrieve a ConnectionInfo object for a given server
    /// </summary>
    /// <returns>The connection parameters</returns>
    public static ConnectionInfo GetSFTPConnectionInfo(ServerConfiguration input)
    {
        var port = 22;
        
        string[] split = input.Address.Split(':');
        string host = split[0];
        
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }
        PrivateKeyFile privateKey;

        var authMethods = new List<AuthenticationMethod>();
        
        if (String.IsNullOrEmpty(input.PrivateKey) == false)
        {
            string tempKeyLocation = SaveTemporaryFile(input.PrivateKey);
            if (String.IsNullOrEmpty(input.PrivateKeyPassword) == false)
            {
                privateKey = new PrivateKeyFile(tempKeyLocation, input.PrivateKeyPassword);
            }
            else
            {
                privateKey = new PrivateKeyFile(tempKeyLocation);    
            }

            File.Delete(tempKeyLocation);
            authMethods.Add(new PrivateKeyAuthenticationMethod(input.Username, privateKey));
        }

        if (String.IsNullOrEmpty(input.Password) == false)
        {
            authMethods.Add(new PasswordAuthenticationMethod(input.Username, input.Password));
        }
        
        return new ConnectionInfo(
            host: host,
            port: port,
            username: input.Username,
            authenticationMethods: authMethods.ToArray()
        );
    }

    public static FtpClient GetFTPConnection(ServerConfiguration input)
    {
        Int32 port = 21;
        string[] split = input.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }

        return new FtpClient(host, port, input.Username, input.Password);
    }

    /// <summary>
    /// Verifies that the server fingerprint matches the expected print
    /// </summary>
    /// <param name="client"></param>
    /// <param name="expectedFp"></param>
    /// <returns></returns>
    public static void VerifyFingerprint(SftpClient client, string expectedFp)
    {
        string actual = "";
        if (Helpers.IsValidString(expectedFp))
        {
            try
            {
                client.HostKeyReceived += delegate(object sender, HostKeyEventArgs e)
                {
                    var b = expectedFp.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
                    actual = BitConverter.ToString(e.FingerPrint).Replace("-", ":");
                    e.CanTrust = e.FingerPrint.SequenceEqual(b);
                };
            }
            catch (SshConnectionException ex)
            {
                throw new Exception($"Failed to verify fingerprint (expected {expectedFp}, got {actual})", ex);
            }    
        }
    }


    /// <summary>
    /// Finds a method for a given class/typename string and method name string
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="methodName"></param>
    /// <returns></returns>
    public static MethodInfo? GetSubclassMethod(string typeName, string methodName)
    {
        var type = Type.GetType(typeName);
        if (type == null)
        {
            return null;
        }
        return type.GetMethod(methodName);
    }
    
    /// <summary>
    /// Get the encoding corresponding to FileEncodings enum
    /// </summary>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static Encoding EncodingFromEnum(FileEncodings encoding) {
        Encoding enc = Encoding.GetEncoding(
            Enum.GetName(
                typeof(FileEncodings), encoding
            ).Replace(
                "_", "-"
            )
        );
        return enc;
    }
    
    /// <summary>
    /// Substitutes parts of a filename with given/generated values
    /// </summary>
    /// <param name="input"></param>
    /// <param name="sourceFilename"></param>
    /// <param name="objectGuid"></param>
    /// <param name="incremental"></param>
    /// <returns></returns>
    public static string FilenameSubstitutions(
        string input, 
        string sourceFilename = "", 
        string objectGuid = "", 
        int incremental = 0
    )
    {
        if (String.IsNullOrEmpty(input))
        {
            return "";
        }
        
        string sb = input;
        var split = sourceFilename.Split('.');

        var filenameParts = new string[split.Length - 1];
        var extension = split[split.Length - 1];
        
        Array.Copy(split, 0, filenameParts, 0, filenameParts.Length);

        // Insert filename and/or extension
        sb = sb.Replace("{source_filename}", string.Join('.', filenameParts));
        sb = sb.Replace("{source_extension}", extension);
        
        // Insert date/timestamp
        sb = sb.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
        sb = sb.Replace("{time}", DateTime.Now.ToString("hh-mm-ss"));
        
        // Insert GUID
        sb = sb.Replace("{guid}", objectGuid);
        
        // Insert incremental number
        sb = sb.Replace("{incremental}", incremental.ToString());


        return sb;
    }

    public static string OSDirSeparator = Path.DirectorySeparatorChar.ToString();
    
    /// <summary>
    /// Joins a list of strings together with a separator (path)
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string JoinPath(string separator, params string[] parts)
    {
        var cleanParts = new List<string>();
        foreach (var part in parts)
        {
            if (IsValidString(part))
            {
                cleanParts.Add(part);
            }
        }

        string retnString = string.Join(separator, cleanParts);
        
        while (retnString.Contains(separator+separator))
        {
            retnString = retnString.Replace(separator + separator, separator);
        }
        
        return retnString;
    }
}