using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
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

    public ServerConfiguration GetServerConfiguration()
    {
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