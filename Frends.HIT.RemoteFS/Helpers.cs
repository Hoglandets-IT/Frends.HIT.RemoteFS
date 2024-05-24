using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using EzSmb.Params;
using FluentFTP;
using RenciRoot = Renci.SshNet;
using RenciCommon = Renci.SshNet.Common;
using RenciSftp = Renci.SshNet.Sftp;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using Newtonsoft.Json;


namespace Frends.HIT.RemoteFS;

class Helpers
{
    public static string GetVaultSecret(string path) {
        var VaultAddr = Environment.GetEnvironmentVariable("VAULT_ADDR");
        var VaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        var VaultStore = Environment.GetEnvironmentVariable("VAULT_STORE");

        IAuthMethodInfo authMethod = new TokenAuthMethodInfo(VaultToken);
        var vaultClientSettings = new VaultClientSettings(VaultAddr, authMethod);


        IVaultClient vaultClient = new VaultClient(vaultClientSettings);

        var kv22 = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: VaultStore);

        Secret<SecretData> kv2Secret = kv22.Result;

        if (kv2Secret.Data.Data.Count == 0) {
            throw new Exception("Vault secret " + path + " not found");
        }
        
        if (kv2Secret.Data.Data.Count == 1) {
            throw new Exception("Invalid vault secret " + path + ": Needs to contain at least connectiontype, address, username and password fields");
        }

        var rtnData = new Dictionary<string,string>();

        foreach (var x in kv2Secret.Data.Data) {
            rtnData[x.Key] = x.Value.ToString();
        }

        return JsonConvert.SerializeObject(rtnData);
    }


    /// <summary>
    /// Remove all . and .. matches from a result
    /// </summary>
    /// <param name="input" />
    /// <returns>List<string /></returns>
    public static List<string> StripDirResults(List<string> input)
    {
        return input.Where(
            x => (x != ".") && (x != "..")
        ).ToList();
    }

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
    /// Get only files (pattern *.*) from the listing
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns>List of strings</returns>
    public static List<string> GetFilesOnly(List<string> input, string filter)
    {
        return GetRegexMatch(input, ".+\\..+");
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
        List<string> Result = new List<string>();

        switch (filterType)
        {
            case FilterTypes.Exact:
                Result = GetExactMatch(input, filter);
                break;
            case FilterTypes.Contains:
                Result = GetContainsMatch(input, filter);
                break;
            case FilterTypes.Wildcard:
                Result = GetWildcardMatch(input, filter);
                break;
            case FilterTypes.Regex:
                Result = GetRegexMatch(input, filter);
                break;
            case FilterTypes.FilesOnly:
                Result = GetFilesOnly(input, filter);
                break;
            default:
                Result = input;
                break;
        }

        return StripDirResults(Result);
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
    /// Get SMB Connection params
    /// </summary>
    public static ParamSet GetSMBConnectionParams(ServerConfiguration connection)
    {
        var par = new ParamSet()
        {
            UserName=connection.Username,
            Password=connection.Password
        };
        if (connection.Domain != null && connection.Domain != "")
        {
            par.DomainName = connection.Domain;
        }

        return par;
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
    public static RenciRoot.ConnectionInfo GetSFTPConnectionInfo(ServerConfiguration input)
    {
        // return new RenciRoot.ConnectionInfo("localhost", "username", new RenciRoot.AuthenticationMethod);
        var port = 22;
        
        string[] split = input.Address.Split(':');
        string host = split[0];
        
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }
        RenciRoot.PrivateKeyFile privateKey;

        var authMethods = new List<RenciRoot.AuthenticationMethod>();
        
        if (String.IsNullOrEmpty(input.PrivateKey) == false)
        {
            string tempKeyLocation = SaveTemporaryFile(input.PrivateKey);
            if (String.IsNullOrEmpty(input.PrivateKeyPassword) == false)
            {
                privateKey = new RenciRoot.PrivateKeyFile(tempKeyLocation, input.PrivateKeyPassword);
            }
            else
            {
                privateKey = new RenciRoot.PrivateKeyFile(tempKeyLocation);    
            }

            File.Delete(tempKeyLocation);
            authMethods.Add(new RenciRoot.PrivateKeyAuthenticationMethod(input.Username, privateKey));
        }

        if (String.IsNullOrEmpty(input.Password) == false)
        {
            authMethods.Add(new RenciRoot.PasswordAuthenticationMethod(input.Username, input.Password));
        }
        
        return new RenciRoot.ConnectionInfo(
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
    public static void VerifyFingerprint(RenciRoot.SftpClient client, string expectedFp)
    {
        string actual = "";
        if (Helpers.IsValidString(expectedFp))
        {
            try
            {
                client.HostKeyReceived += delegate(object sender, RenciCommon.HostKeyEventArgs e)
                {
                    var b = expectedFp.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
                    actual = BitConverter.ToString(e.FingerPrint).Replace("-", ":");
                    e.CanTrust = e.FingerPrint.SequenceEqual(b);
                };
            }
            catch (RenciCommon.SshConnectionException ex)
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

    public static string[] JoinPath(string separator, bool separateLastPart, params string[] parts)
    {
        string joinedPath = JoinPath(separator, parts);
        if (separateLastPart == true)
        {
            var split = joinedPath.Split(separator).ToList();
            var path = split.GetRange(0, split.Count - 1);
            var file = split.GetRange(split.Count - 1, 1);
            return new string[] {string.Join(separator, path), file[0]};
        }

        return new string[] { joinedPath };
    }
}