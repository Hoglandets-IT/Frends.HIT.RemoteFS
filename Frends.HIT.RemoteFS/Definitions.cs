using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Frends.HIT.RemoteFS;

/// <summary>
/// Available encodings to use for reading and writing to files
/// </summary>
public enum FileEncodings
{
    [Display(Name = "UTF-8")]
    UTF_8,
    
    [Display(Name = "UTF-32")]
    UTF_32,
    
    [Display(Name = "ISO-8859-1")]
    ISO_8859_1,
    
    [Display(Name = "ASCII")]
    ASCII,
    
    [Display(Name = "Latin-1")]
    LATIN_1
}

/// <summary>
/// Available connection types
/// </summary>
public enum ConnectionTypes
{
    [Display(Name = "Samba/SMB/CIFS")]
    SMB,
    
    [Display(Name = "FTP")]
    FTP,
    
    [Display(Name = "SFTP")]
    SFTP
}

/// <summary>
/// Choice as to where to retrieve the server configuration, from a json string or manually
/// </summary>
public enum ConfigurationType
{
    [Display(Name = "JSON String")]
    Json,
    
    [Display(Name = "Samba/SMB/CIFS")]
    SMB,
    
    [Display(Name = "FTP")]
    FTP,
    
    [Display(Name = "SFTP")]
    SFTP
}

public enum FilterTypes
{
    /// <summary>
    /// Doesn't filter the results from the directory listing
    /// </summary>
    [Display(Name = "No Filtration")]
    None,
    
    /// <summary>
    /// Matches the filename/extension exactly
    /// </summary>
    [Display(Name = "Match Exact")]
    Exact,
    
    /// <summary>
    /// Check if the filename contains a given string
    /// </summary>
    [Display(Name = "Match Contains")]
    Contains,
    
    /// <summary>
    /// Matches the filename with one/multiple given wildcards (*)
    /// </summary>
    [Display(Name = "Match Wildcard (*)")]
    Wildcard,
    
    /// <summary>
    /// Matches the filename against a regular expression
    /// </summary>
    [Display(Name = "Match Regex")]
    Regex
    
}

/// <summary>
/// Configuration for connecting to the remote filesystem.
/// </summary>
public class ServerConfiguration
{
    /// <summary>
    /// The type of connection
    /// </summary>
    [DefaultValue(ConnectionTypes.SMB)]
    public ConnectionTypes ConnectionType { get; set; }

    /// <summary>
    /// The hostname or IP address of the server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    public string Address { get; set; }
    
    /// <summary>
    /// (optional) The domain of the SMB user for AD environments 
    /// </summary>
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SMB)]
    public string Domain { get; set; } = "";
    
    /// <summary>
    /// The username used for connecting to the remote server
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// The password used for connecting to the remote server
    /// </summary>
    [PasswordPropertyText]
    public string Password { get; set; } = "";

    /// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    public string PrivateKey { get; set; } = "";
    
    /// <summary>
    /// (optional) The password for the private key above
    /// </summary>
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    public string PrivateKeyPassword { get; set; } = "";
    
    /// <summary>
    /// (optional) The fingerprint of the SFTP server for verification
    /// </summary>
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    public string Fingerprint { get; set; } = "";

    /// <summary>
    /// Initialize a new ServerConfiguration object
    /// </summary>
    /// <param name="connectiontype">The type of connection (SMB/FTP/SFTP)</param>
    /// <param name="address">The Hostname/IP for the server</param>
    /// <param name="domain">The domain for the server (SMB only)</param>
    /// <param name="username">The username for the connection</param>
    /// <param name="password">The password for the connection</param>
    /// <param name="privatekey">The private key used for the connection (string, SFTP only, optional)</param>
    /// <param name="privatekeypassword">The password for the private key used for the connection (string, SFTP only, optional)</param>
    /// <param name="fingerprint">The remote fingerprint for verification (string, SFTP only, optional)</param>
    public ServerConfiguration(
        ConnectionTypes connectiontype,
        string address,
        string domain,
        string username,
        string password, 
        string privatekey,
        string privatekeypassword,
        string fingerprint
    )
    {
        ConnectionType = connectiontype;
        Address = address;
        Domain = domain;
        Username = username;
        Password = password;
        PrivateKey = privatekey;
        PrivateKeyPassword = privatekeypassword;
        Fingerprint = fingerprint;
    }

    /// <summary>
    /// Initialize a new ServerConfiguration object from a JSON block
    /// </summary>
    /// <param name="connectiontype">The type of connection (SMB/FTP/SFTP)</param>
    /// <param name="address">The Hostname/IP for the server</param>
    /// <param name="domain">The domain for the server (SMB only)</param>
    /// <param name="username">The username for the connection</param>
    /// <param name="password">The password for the connection</param>
    /// <param name="privatekey">The private key used for the connection (string, SFTP only, optional)</param>
    /// <param name="privatekeypassword">The password for the private key used for the connection (string, SFTP only, optional)</param>
    /// <param name="fingerprint">The remote fingerprint for verification (string, SFTP only, optional)</param>
    [JsonConstructor]
    public ServerConfiguration(
        string connectiontype,
        string address,
        string domain,
        string username,
        string password,
        string privatekey,
        string privatekeypassword,
        string fingerprint
    )
    {
        ConnectionType = (ConnectionTypes)Enum.Parse(typeof(ConnectionTypes), connectiontype);
        Address = address;
        Domain = domain;
        Username = username;
        Password = password;
        PrivateKey = privatekey;
        PrivateKeyPassword = privatekeypassword;
        Fingerprint = fingerprint;
    }

    public MethodInfo GetActionClass(string name)
    {
        return Helpers.GetSubclassMethod($"Frends.HIT.RemoteFS.{ConnectionType.ToString()}", name);
    }
}

/// <summary>
/// Parameters for setting up the remote server connection
/// </summary>
[DisplayName("Connection")]
public class ServerParams
{
    /// <summary>
    /// Whether to get the configuration from a json string or enter manually
    /// </summary>
    [DefaultValue(ConfigurationType.Json)]
    public ConfigurationType ConfigurationSource { get; set; }

    /// <summary>
    /// The server configuration in Json format
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Expression")]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.Json)]
    public string JsonConfiguration { get; set; } = "";

    /// <summary>
    /// The hostname or IP address of the server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    public string Address { get; set; } = "";
    
    /// <summary>
    /// (optional) The domain of the SMB user for AD environments 
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SMB)]
    public string Domain { get; set; } = "";

    /// <summary>
    /// The username used for connecting to the remote server
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    public string Username { get; set; } = "";

    /// <summary>
    /// The password used for connecting to the remote server
    /// </summary>
    [PasswordPropertyText]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    public string Password { get; set; } = "";

    /// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    public string PrivateKey { get; set; } = "";
    
	/// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    public string PrivateKeyPassword { get; set; } = "";

    /// <summary>
    /// (optional) The fingerprint of the SFTP server for verification
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    public string Fingerprint { get; set; } = "";

    public ServerParams Create(
        ConfigurationType configurationtype,
        string jsonconfiguration,
        string address = "",
        string domain = "",
        string username = "",
        string password = "",
        string privatekey = "",
        string privatekeypassword = "",
        string fingerprint = ""
    )
    {
        ConfigurationSource = configurationtype;
        JsonConfiguration = jsonconfiguration;
        Address = address;
        Domain = domain;
        Username = username;
        Password = password;
        PrivateKey = privatekey;
        PrivateKeyPassword = privatekeypassword;
        Fingerprint = fingerprint;

        return this;
    }

    public ServerConfiguration GetServerConfiguration()
    {
        if (ConfigurationSource == ConfigurationType.Json)
        {
            return JsonConvert.DeserializeObject<ServerConfiguration>(JsonConfiguration);
        }
        
        return new ServerConfiguration(
            connectiontype: (ConnectionTypes)Enum.Parse(typeof(ConnectionTypes), ConfigurationSource.ToString()),
            address: Address,
            domain: Domain,
            username: Username,
            password: Password,
            privatekey: PrivateKey,
            privatekeypassword: PrivateKeyPassword,
            fingerprint: Fingerprint
        );
    }
}


/// <summary>
/// Parameters for retrieving a directory listing from the remote server
/// </summary>
[DisplayName("Parameters")]
public class ListParams
{
    /// <summary>
    /// The path to the folder of which to retrieve the listing
    /// </summary>
    [DefaultValue(null)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; }
    
    /// <summary>
    /// Choose whether and how to filter the results of the listing
    /// </summary>
    [DefaultValue(FilterTypes.None)]
    public FilterTypes Filter { get; set; }
    
    /// <summary>
    /// The pattern to use to match the files against
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Pattern { get; set; }
    
    public ListParams Create(
        string path,
        FilterTypes filter,
        string pattern
    )
    {
        Path = path;
        Filter = filter;
        Pattern = pattern;
        return this;
    }
}

/// <summary>
/// Parameters for reading file contents on a remote server
/// </summary>
[DisplayName("Parameters")]
public class ReadParams
{
    
    /// <summary>
    /// The path to the file for which to retrieve the content
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; }
    
    /// <summary>
    /// The name of the file
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string File { get; set; }
    
    /// <summary>
    /// The encoding to use when reading the file, default UTF-8
    /// </summary>
    [DefaultValue(FileEncodings.UTF_8)]
    public FileEncodings Encoding { get; set; }

    public ReadParams Create(
        string path,
        string file,
        FileEncodings encoding
    )
    {
        Path = path;
        File = file;
        Encoding = encoding;
        return this;
    }
}

[DisplayName("Parameters")]
public class WriteParams
{
    /// <summary>
    /// The path to where the file is to be written
    /// </summary>
    [DefaultValue(null)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; }
    
    /// <summary>
    /// The name of the file
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string File { get; set; }
    
    /// <summary>
    /// The content to write to the file
    /// </summary>
    [DefaultValue(null)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Content { get; set; }
    
    /// <summary>
    /// Whether to overwrite the file if it already exists
    /// </summary>
    [DefaultValue(null)]
    public bool Overwrite { get; set; }
    
    /// <summary>
    /// The encoding to use for writing to the file
    /// </summary>
    [DefaultValue(FileEncodings.UTF_8)]
    public FileEncodings Encoding { get; set; }
    
    public WriteParams Create(
        string path,
        string file,
        string content,
        bool overwrite,
        FileEncodings encoding
    )
    {
        Path = path;
        File = file;
        Content = content;
        Overwrite = overwrite;
        Encoding = encoding;

        return this;
    }
}

public class CreateDirParams
{
    [DefaultValue(null)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; }
    
    [DefaultValue(false)]
    public bool Recursive { get; set; }
    
    public CreateDirParams Create(
        string path,
        bool recursive
    )
    {
        Path = path;
        Recursive = recursive;
        return this;
    }
}

/// <summary>
/// Parameters for retrieving a directory listing from the remote server
/// </summary>
[DisplayName("Parameters")]
public class DeleteParams
{
    /// <summary>
    /// The path to the folder from which to delete files
    /// </summary>
    [DefaultValue(null)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; }

    /// <summary>
    /// The file in the folder above to delete
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string File { get; set; }
    
    public DeleteParams Create(
        string path,
        string file
    )
    {
        Path = path;
        File = file;
        return this;
    }
}

[DisplayName("General Configuration")]
public class BatchConfigParams
{
    /// <summary>
    /// Whether this set of batches is enabled or not
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Whether to store configuration on a server (e.g. for sequential filenames)
    /// </summary>
    [DefaultValue(true)]
    public bool UseConfigServer { get; set; }
    
    /// <summary>
    /// The configuration for the server storing the config information
    /// </summary>
    [DefaultValue(null)]
    [UIHint(nameof(UseConfigServer), "", true)]
    [DisplayFormat(DataFormatString = "Expression")]
    public string ConfigServer { get; set; }
    
    /// <summary>
    /// The path on the server where the config information is stored (not automatically created)
    /// </summary>
    [DefaultValue(null)]
    [UIHint(nameof(UseConfigServer), "", true)]
    public string ConfigPath { get; set; }
    
    /// <summary>
    /// Whether to backup the files copied from source to destination
    /// </summary>
    [DefaultValue(false)]
    public bool BackupFiles { get; set; }
    
    /// <summary>
    /// Whether to backup the files to the same server used for the configuration above
    /// </summary>
    [DefaultValue(true)]
    [UIHint(nameof(BackupFiles), "", true)]
    public bool BackupToConfigServer { get; set; }
    
    /// <summary>
    /// The server to use for backing up the files
    /// </summary>
    [UIHint(nameof(BackupToConfigServer), "", false)]
    [DisplayFormat(DataFormatString = "Expression")]
    public string BackupServer { get; set; }
    
    /// <summary>
    /// The path on the server to use for backing up the files
    /// </summary>
    [DefaultValue(null)]
    [UIHint(nameof(BackupFiles), "", true)]
    public string BackupPath { get; set; }
    
    /// <summary>
    /// The filename to set when backing up the files
    /// All substitutions available in the batch configuration are available in the filename
    /// </summary>
    [DefaultValue(null)]
    [UIHint(nameof(BackupFiles), "", true)]
    public string BackupFilename { get; set; }
    
    public ServerParams? GetConfigurationServerParams()
    {
        if (Enabled && UseConfigServer)
        {
            return new ServerParams().Create(ConfigurationType.Json, ConfigServer);
        }

        return null;
    }
    
    public ServerParams? GetBackupServerParams()
    {
        if (BackupFiles)
        {
            if (BackupToConfigServer)
            {
                return GetConfigurationServerParams();
            }

            return new ServerParams().Create(ConfigurationType.Json, BackupServer);
        }

        return null;
    }
}

[DisplayName("Batches")]
public class BatchParams
{
    public string ObjectGuid { get; set; }
    public string SourceServer { get; set; }
    public string SourcePath { get; set; }
    public FilterTypes SourceFilterType { get; set; }
    public string SourceFilterPattern { get; set; }
    
    public FileEncodings SourceEncoding { get; set; }
    
    public string DestinationServer { get; set; }
    public string DestinationPath { get; set; }
    public string DestinationFilename { get; set; }
    
    public FileEncodings DestinationEncoding { get; set; }
    
    public bool Overwrite { get; set; }
    public bool DeleteSource { get; set; }

    public ServerParams? GetSourceServerParams()
    {
        return new ServerParams().Create(ConfigurationType.Json, SourceServer);
    }

    public ServerParams? GetDestinationServerParams()
    {
        return new ServerParams().Create(ConfigurationType.Json, DestinationServer);
    }
}

/// <summary>
/// The result for a directory listing operation
/// </summary>
public class ListResult
{
    /// <summary>
    /// A count of the files found matching the given input
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// A list of the files found matching the given input
    /// </summary>
    public List<string> Files { get; set; }

    public ListResult(
        int count,
        List<string> files
    )
    {
        Count = count;
        Files = files;
    }
}

/// <summary>
/// The result for a file read operation
/// </summary>
public class ReadResult
{
    /// <summary>
    /// The content of the read file
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// The path to the file that was read
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// The encoding used to read the file contents
    /// </summary>
    public FileEncodings Encoding { get; set; }

    public ReadResult(
        string content,
        string path,
        FileEncodings encoding
    )
    {
        Content = content;
        Path = path;
        Encoding = encoding;
    }
}

/// <summary>
/// The result for a file write operation
/// </summary>
public class WriteResult
{
    /// <summary>
    /// Whether the write was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The path to the file that was written to
    /// </summary>
    public string Path { get; set; }    
    
    /// <summary>
    /// The encoding used to write to the file
    /// </summary>
    public FileEncodings Encoding { get; set; }

    public WriteResult(
        bool success,
        string path,
        FileEncodings encoding
    )
    {
        Success = success;
        Path = path;
        Encoding = encoding;
    }
}

public class CreateDirResult
{
    /// <summary>
    /// Whether the directory creation was successful
    /// </summary>
    public bool Success { get; set; }
    
    public CreateDirResult(bool success) => Success = success;
}

public class CopyResult
{
    /// <summary>
    /// Whether the write was successful
    /// </summary>
    public bool Success { get; set; }
    
    public CopyResult(bool success) => Success = success;
}

public class DeleteResult
{
    /// <summary>
    /// Whether the write was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The path that was deleted
    /// </summary>
    public string Path { get; set; }
    
    public DeleteResult(bool success, string path)
    {
        Success = success;
        Path = path;
    }
}

public class BatchResult
{
    public string ObjectGuid { get; set; }
    public string SourceFile { get; set; }
    public string DestinationFile { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    
    public BatchResult(
        string objectGuid,
        string sourceFile,
        string destinationFile,
        bool success,
        string message,
        DateTime timestamp
    )
    {
        ObjectGuid = objectGuid;
        SourceFile = sourceFile;
        DestinationFile = destinationFile;
        Success = success;
        Message = message;
        Timestamp = timestamp;
    }
}

public class BatchResults
{
    /// <summary>
    /// Number of transfers completed successfully
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// The list of results for each transfer
    /// </summary>
    public List<BatchResult> Results { get; set; }
    
    public BatchResults(
        int count,
        List<BatchResult> results
    )
    {
        Count = count;
        Results = results;
    }
}