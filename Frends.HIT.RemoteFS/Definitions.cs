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
    /// <summary>
    /// Raw Bytes
    /// </summary>
    [Display(Name = "Raw")]
    RAW,

    /// <summary>
    /// UTF-8 Encoding
    /// </summary>
    [Display(Name = "UTF-8")]
    UTF_8,
    
    /// <summary>
    /// UTF-32 Encoding
    /// </summary>
    [Display(Name = "UTF-32")]
    UTF_32,
    
    /// <summary>
    /// ISO8859-1 Encoding
    /// </summary>
    [Display(Name = "ISO-8859-1")]
    ISO_8859_1,
    
    /// <summary>
    /// ASCII Encoding
    /// </summary>
    [Display(Name = "ASCII")]
    ASCII,
    
    /// <summary>
    /// Latin-1 Encoding
    /// </summary>
    [Display(Name = "Latin-1")]
    LATIN_1
}


/// <summary>
/// Types of objects to retrieve (files, directories or both
/// </summary>
public enum ObjectTypes
{
    /// <summary>
    /// Retrieve only files
    /// </summary>
    [Display(Name = "Files")]
    Files,
    
    /// <summary>
    /// Retrieve only directories
    /// </summary>
    [Display(Name = "Directories")]
    Directories,

    /// <summary>
    /// Retrieve both files and directories
    /// </summary>
    [Display(Name = "Both")]
    Both
}


/// <summary>
/// Available connection types
/// </summary>
public enum ConnectionTypes
{
    /// <summary>
    /// SMB/Samba (former CIFS), Windows fileshare
    /// </summary>
    [Display(Name = "Samba/SMB/CIFS")]
    SMB,
    
    /// <summary>
    /// FTP, File Transfer Protocol
    /// </summary>
    [Display(Name = "FTP")]
    FTP,
    
    /// <summary>
    /// SFTP, Secure File Transfer Protocol
    /// </summary>
    [Display(Name = "SFTP")]
    SFTP,
    
    /// <summary>
    /// Local Storage (On agent server/in agent pod)
    /// </summary>
    [Display(Name = "Local Storage")]
    LocalStorage
}

/// <summary>
/// Choice as to where to retrieve the server configuration, from a json string or manually
/// </summary>
public enum ConfigurationType
{
    /// <summary>
    /// From a Json configuration string
    /// </summary>
    [Display(Name = "JSON String")]
    Json,
    
    /// <summary>
    /// Manual Config - SMB/CIFS
    /// </summary>
    [Display(Name = "Samba/SMB/CIFS")]
    SMB,
    
    /// <summary>
    /// Manual Config - FTP
    /// </summary>
    [Display(Name = "FTP")]
    FTP,
    
    /// <summary>
    /// Manual Config - SFTP
    /// </summary>
    [Display(Name = "SFTP")]
    SFTP,
    
    /// <summary>
    /// Manual Config - Local Storage
    /// </summary>
    [Display(Name = "Local Storage")]
    LocalStorage
}

/// <summary>
/// Types of filters used for finding and deleting files in directories
/// </summary>
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
    [Display(Name = "Remote Server Type")]
    public ConnectionTypes ConnectionType { get; set; }
    
    /// <summary>
    /// The hostname or IP address of the server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    [Display(Name = "Hostname/IP Address")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SMB, ConnectionTypes.SFTP, ConnectionTypes.FTP)]
    public string Address { get; set; }

    /// <summary>
    /// (optional) The domain of the SMB user for AD environments 
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SMB)]
    [Display(Name = "LDAP/AD Domain")]
    public string Domain { get; set; }
    
    /// <summary>
    /// The username used for connecting to the remote server
    /// </summary>
    [Display(Name = "Username")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SMB, ConnectionTypes.SFTP, ConnectionTypes.FTP)]
    public string Username { get; set; }

    /// <summary>
    /// The password used for connecting to the remote server
    /// </summary>
    [DefaultValue("")]
    [PasswordPropertyText]
    [Display(Name = "Password")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SMB, ConnectionTypes.SFTP, ConnectionTypes.FTP)]
    public string Password { get; set; }

    /// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    [Display(Name = "OpenSSH Private Key")]
    public string PrivateKey { get; set; }
    
    /// <summary>
    /// (optional) The password for the private key above
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    [Display(Name = "Private Key Password/Passphrase")]
    public string PrivateKeyPassword { get; set; }
    
    /// <summary>
    /// (optional) The fingerprint of the SFTP server for verification (Empty for no verification)
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(ConnectionType), "", ConnectionTypes.SFTP)]
    [Display(Name = "Verify Fingerprint")]
    public string Fingerprint { get; set; }

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
        if (!Enum.TryParse(typeof(ConnectionTypes), (string)connectiontype, true, out var tester ))
        {
            throw new ArgumentException($"Invalid connection type ({connectiontype})");
        }
        
        ConnectionType = (ConnectionTypes)Enum.Parse(typeof(ConnectionTypes), connectiontype);
        Address = address;
        Domain = domain;
        Username = username;
        Password = password;
        PrivateKey = privatekey;
        PrivateKeyPassword = privatekeypassword;
        Fingerprint = fingerprint;
    }

    /// <summary>
    /// Get the method for a given string operation
    /// </summary>
    /// <param name="name">The name of the method</param>
    /// <returns>Method</returns>
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
    [Display(Name = "Json Server Configuration")]
    public string JsonConfiguration { get; set; } = "";

    /// <summary>
    /// The hostname or IP address of the server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    [Display(Name = "Hostname/IP Address")]
    public string Address { get; set; } = "";
    
    /// <summary>
    /// (optional) The domain of the SMB user for AD environments 
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SMB)]
    [Display(Name = "AD/LDAP Domain")]
    public string Domain { get; set; } = "";

    /// <summary>
    /// The username used for connecting to the remote server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    /// <summary>
    /// The password used for connecting to the remote server
    /// </summary>
    [PasswordPropertyText]
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.FTP, ConfigurationType.SFTP, ConfigurationType.SMB)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    /// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    [Display(Name = "OpenSSH Private Key")]
    [DisplayFormat(DataFormatString = "Text")]
    public string PrivateKey { get; set; } = "";
    
	/// <summary>
    /// The PrivateKey used for connecting to the remote SFTP server
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    [Display(Name = "Private Key Password")]
    [DisplayFormat(DataFormatString = "Text")]
    public string PrivateKeyPassword { get; set; } = "";

    /// <summary>
    /// (optional) The fingerprint of the SFTP server for verification
    /// </summary>
    [UIHint(nameof(ConfigurationSource), "", ConfigurationType.SFTP)]
    [Display(Name = "Fingerprint")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Fingerprint { get; set; } = "";

    /// <summary>
    /// Get the server configuration object from the parameters
    /// </summary>
    /// <returns>ServerConfiguration</returns>
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
    public FilterTypes Filter { get; set; } = FilterTypes.None;

    /// <summary>
    /// The pattern to use to match the files against
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Pattern { get; set; } = "";
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
    [DefaultValue("")]
    [UIHint(nameof(Encoding), "", FileEncodings.ASCII, FileEncodings.ISO_8859_1, FileEncodings.LATIN_1, FileEncodings.UTF_32, FileEncodings.UTF_8)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Content { get; set; }
    
    /// <summary>
    /// The content to write to the file
    /// </summary>
    [DefaultValue(new byte[]{})]
    [UIHint(nameof(Encoding), "", FileEncodings.RAW)]
    [DisplayFormat(DataFormatString = "Text")]
    public byte[] ByteContent { get; set; }
    
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
}

/// <summary>
/// Parameters for the destination of the Copy function (WriteParams minus Content)
/// </summary>
public class CopyDestParams
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
    /// Whether to overwrite the file if it already exists
    /// </summary>
    [DefaultValue(null)]
    public bool Overwrite { get; set; }
    
    /// <summary>
    /// The encoding to use for writing to the file
    /// </summary>
    [DefaultValue(FileEncodings.UTF_8)]
    public FileEncodings Encoding { get; set; }
    
    /// <summary>
    /// Gets the WriteParams object from the class parameters and content
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public WriteParams GetWriteParams(string content, byte[] bytecontent)
    {
        return new WriteParams(){
            Path=Path,
            File=File,
            Content=content,
            ByteContent=bytecontent,
            Overwrite=Overwrite,
            Encoding=Encoding
        };
    }
}

/// <summary>
/// Properties for the Create directory function
/// </summary>
public class CreateDirParams
{
    /// <summary>
    /// The path to the directory/-ies to create
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; } = "";

    /// <summary>
    /// Whether to create all directories in the tree automatically
    /// </summary>
    [DefaultValue(false)]
    public bool Recursive { get; set; } = false;
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
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string Path { get; set; } = "";

    /// <summary>
    /// The file in the folder above to delete
    /// </summary>
    [DefaultValue("")]
    [DisplayFormat(DataFormatString = "Text")]
    public string File { get; set; } = "";
    
    
}

/// <summary>
/// Configuration for batch transfers between servers
/// </summary>
[DisplayName("General Configuration")]
public class BatchConfigParams
{
    /// <summary>
    /// Whether this set of batches is enabled or not
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Whether to store configuration on a server (e.g. for sequential filenames)
    /// </summary>
    [DefaultValue(false)]
    public bool UseConfigServer { get; set; } = false;
    
    /// <summary>
    /// The configuration for the server storing the config information
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(UseConfigServer), "", true)]
    [DisplayFormat(DataFormatString = "Expression")]
    public string ConfigServer { get; set; } = "";
    
    /// <summary>
    /// The path on the server where the config information is stored (not automatically created)
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(UseConfigServer), "", true)]
    [DisplayFormat(DataFormatString = "Text")]
    public string ConfigPath { get; set; } = "";
    
    /// <summary>
    /// Whether to backup the files copied from source to destination
    /// </summary>
    [DefaultValue(false)]
    public bool BackupFiles { get; set; } = false;

    /// <summary>
    /// Backup files to a sub-folder in the source location
    /// </summary>
    [DefaultValue(false)]
    [UIHint(nameof(BackupFiles), "", true)]
    public bool BackupToSubfolder { get; set; } = false;

    /// <summary>
    /// Sub-folder name to use
    /// </summary>
    [DefaultValue("Archive")]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(BackupToSubfolder), "", true)]
    public string SubfolderName { get; set; } = "Archive";

    /// <summary>
    /// Whether to backup the files to the same server used for the configuration above
    /// </summary>
    [DefaultValue(true)]
    [UIHint(nameof(BackupFiles), "", true)]
    public bool BackupToConfigServer { get; set; } = true;
    
    /// <summary>
    /// The server to use for backing up the files
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(BackupToConfigServer), "", false)]
    [DisplayFormat(DataFormatString = "Expression")]
    public string BackupServer { get; set; } = "";
    
    /// <summary>
    /// The path to use for backing up the files 
    /// </summary>
    [DefaultValue("")]
    [UIHint(nameof(BackupFiles), "", true)]
    [DisplayFormat(DataFormatString = "Text")]
    public string BackupPath { get; set; } = "";
    
    /// <summary>
    /// The filename to set when backing up the files
    /// All substitutions available in the batch configuration are available in the filename
    /// </summary>
    [DefaultValue(null)]
    [UIHint(nameof(BackupFiles), "", true)]
    [DisplayFormat(DataFormatString = "Text")]
    public string BackupFilename { get; set; } = "";
    
    
    /// <summary>
    /// Get the parameters for the configuration server
    /// </summary>
    /// <returns>ServerParams</returns>
    public ServerParams? GetConfigurationServerParams()
    {
        if (Enabled && UseConfigServer)
        {
            return new ServerParams(){
                ConfigurationSource=ConfigurationType.Json,
                JsonConfiguration=ConfigServer
            };
        }

        return null;
    }
    
    /// <summary>
    /// Get the parameters for the backup server
    /// </summary>
    /// <returns></returns>
    public ServerParams? GetBackupServerParams()
    {
        if (BackupFiles)
        {
            if (BackupToConfigServer)
            {
                return GetConfigurationServerParams();
            }

            if (!BackupToSubfolder)
            {
                return new ServerParams(){
                    ConfigurationSource=ConfigurationType.Json,
                    JsonConfiguration=BackupServer
                };
            }
        }

        return null;
    }
}

/// <summary>
/// Configuration for the files to be transferred for each batch
/// </summary>
[DisplayName("Batches")]
public class BatchParams
{
    /// <summary>
    /// The GUID/Unique identifier for the batch item
    /// </summary>
    [DefaultValue("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]
    [DisplayFormat(DataFormatString = "Text")]
    public string ObjectGuid { get; set; }
    
    /// <summary>
    /// The source server configuration (JSON)
    /// </summary>
    [DisplayFormat(DataFormatString = "Expression")]
    public string SourceServer { get; set; }
    
    /// <summary>
    /// The path on the source server to check
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    public string SourcePath { get; set; }
    
    /// <summary>
    /// The filter type to use for finding the files on the source server
    /// </summary>
    public FilterTypes SourceFilterType { get; set; }
    
    /// <summary>
    /// The pattern to apply on the files on the source server
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    public string SourceFilterPattern { get; set; } = "";
    
    /// <summary>
    /// The encoding to use when reading the file from the source server
    /// </summary>
    public FileEncodings SourceEncoding { get; set; }
    
    /// <summary>
    /// The configuration for the destination server (JSON)
    /// </summary>
    [DisplayFormat(DataFormatString = "Expression")]
    public string DestinationServer { get; set; }
    
    /// <summary>
    /// The path on the destination server to copy the files to
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    public string DestinationPath { get; set; }
    
    /// <summary>
    /// The filename to set for the files copied to the destination server
    /// Substitutions are available, check documentation
    /// </summary>
    [DisplayFormat(DataFormatString = "Text")]
    public string DestinationFilename { get; set; }
    
    /// <summary>
    /// The encoding to use when writing the file to the destination server
    /// </summary>
    public FileEncodings DestinationEncoding { get; set; }
    
    /// <summary>
    /// Whether to overwrite the file if it exists on the destination server
    /// </summary>
    public bool Overwrite { get; set; }
    
    /// <summary>
    /// Whether to delete the source file if the transfer was successful
    /// </summary>
    public bool DeleteSource { get; set; }

    /// <summary>
    /// Get the ServerParams for the source server
    /// </summary>
    /// <returns>ServerParams</returns>
    public ServerParams? GetSourceServerParams()
    {
        return new ServerParams(){
            ConfigurationSource=ConfigurationType.Json,
            JsonConfiguration=SourceServer
        };
    }

    /// <summary>
    /// Get the ServerParams for the destination server
    /// </summary>
    /// <returns>ServerParams</returns>
    public ServerParams? GetDestinationServerParams()
    {
       return new ServerParams(){
            ConfigurationSource=ConfigurationType.Json,
            JsonConfiguration=DestinationServer
        };
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

    /// <summary>
    /// Create a new ListResult object
    /// </summary>
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
    /// The content of the read file
    /// </summary>
    public byte[] ByteContent { get; set; }
    
    /// <summary>
    /// The path to the file that was read
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// The encoding used to read the file contents
    /// </summary>
    public FileEncodings Encoding { get; set; }

    /// <summary>
    /// Create a new ReadResult object
    /// </summary>
    public ReadResult(
        string content,
        byte[] bytecontent,
        string path,
        FileEncodings encoding
    )
    {
        Content = content;
        Path = path;
        Encoding = encoding;
        ByteContent = bytecontent;
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

    /// <summary>
    /// Create a new WriteResult object
    /// </summary>
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
    
    /// <summary>
    /// Create a new CreateDirResult object
    /// </summary>
    public CreateDirResult(bool success) => Success = success;
}

public class CopyResult
{
    /// <summary>
    /// Whether the write was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Create a new CopyResult object
    /// </summary>
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
    
    /// <summary>
    /// Create a new DeleteResult object
    /// </summary>
    public DeleteResult(bool success, string path)
    {
        Success = success;
        Path = path;
    }
}

public class BatchResult
{
    /// <summary>
    /// The Guid of the Batch item
    /// </summary>
    public string ObjectGuid { get; set; }
    
    /// <summary>
    /// The source file that was moved
    /// </summary>
    public string SourceFile { get; set; }
    
    /// <summary>
    /// The destination file that was moved
    /// </summary>
    public string DestinationFile { get; set; }
    
    /// <summary>
    /// Whether the transfer was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The error message in case the move was not successful
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// The timestamp when the transfer took place
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Create a new BatchResult object
    /// </summary>
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

/// <summary>
/// The results for batch task
/// </summary>
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
    
    /// <summary>
    /// Create a new BatchResults object
    /// </summary>
    public BatchResults(
        int count,
        List<BatchResult> results
    )
    {
        Count = count;
        Results = results;
    }
}