# Frends.HIT.RemoteFS
Task for Frends Integration Platform for managing files, directories on and transferring files between remote filesystems

## Changelog
### 0.10.1
- Added the option to list only files or folders on all backends
- Added the recursive list task
- Added the option to get secrets from Hashicorp Vault (Note: See documentation for requirements)
### 0.9.1
- Added the option to retrieve files from Pulsencombine (e.g. financialfiles/generalledger)
### 0.8.1
- Removed connection cache due to issues with concurrent integration runs
### 0.6.1
- Added global SFTP connection object to minimize amount of connections with rate limited servers

## Supported Remotes
### FTP
### SFTP
### SMB
### SCP (Upcoming)
### HTTP/s (Upcoming)

## Installation
Download the package from the Releases section to the right, or from our Nuget server (API key required, contact us for more information, URLs and keys).


## Server Configuration
Server configuration identifies the hostname/IP and account information for connecting to a specific server. For List, Read, Write, Delete and Create Folder there are two options while Batch Transfer requires the JSON Configuration to be placed in an environment variable.

### Fields
|Property|Type|Applies to|Example Value|Description|
|---|---|---|---|---|
|Address|string|All (Mandatory)|ftp.example.com:21, sftp.example.com, integrations.pulsencombine-a.se|The address of the remote server, with optional port.|
|Username|string|All (Mandatory)|someusername|The username to use when connecting to the remote server.|
|Password|string|All (Optional)|somepassword|The password to use when connecting to the remote server. Can be omitted if using Anonymous for FTP or Private key authentication for SFTP.|
|Domain|string|SMB (Optional)|WORKGROUP|The domain to use for the user account specified under Username.|
|Certificate|string|PulsenCombine (Mandatory)|-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----|The certificate to use for authentication to PulsenCombine/Web.|
|PrivateKey|string|SFTP (Optional), PulsenCombine (Mandatory)|-----BEGIN *** PRIVATE KEY-----\n...\n-----END *** PRIVATE KEY-----|The private key to use for authentication.|
|PrivateKeyPassword|string|SFTP (Optional)|somepassword|The password to use for the private key.|
|Fingerprint|string|SFTP (Optional)|RSA:.....|The fingerprint used to verify the remote host (currently unavailable)|

### Json Format
The JSON file contains all the fields above in lowercase with the addition of "connectiontype", for example:
```json

# FTP
{
    "connectiontype": "FTP",
    "address": "someserver.com",
    "username": "someuser",
    "password": "somepassword"
}

# SFTP (Username/Password)
{
    "connectiontype": "SFTP",
    "address": "someserver.com",
    "username": "someuser",
    "password": "somepassword"
}

# SFTP (Private Keyfile)
{
    "connectiontype": "SFTP",
    "address": "someserver.com",
    "username": "someuser",
    "privatekey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
}

# SMB (Domain-joined server/computer)
{
    "connectiontype": "SMB",
    "address": "SOMESERVER.internal.domain.com",
    "domain": "AD-DOMAIN",
    "username": "someaduser",
    "password": "somepassword"
}

# SMB (Not Domain-joined)
{
    "connectiontype": "SMB",
    "address": "SOMESERVER.internal.domain.com",
    "domain": "WORKGROUP",  # Or the server name (SERVERNAME)
    "username": "someaduser",
    "password": "somepassword"
}
```
![](resources/images/serverconfig.png)

### Hashicorp Vault
To retrieve secrets from Hashicorp Vault, you need to set some environment variables for the Frends process, either in the container if running on Linux/Docker/Kubernetes or in the environment variables for the Windows service.

| Variable | Example | Description |
|---|---|---|
| VAULT_ADDR | http://vault.example.com:8200 | The address of the Vault server |
| VAULT_TOKEN | s.1234567890abcdef | The token to use for authentication |
| VAULT_STORE | kv2 | The path to the secret storage engine in Vault |

## Filters
When filters are available for listings or batch transfers, the following types are available

|Property|Example Value|Description|
|---|---|---|
|None|None|No filter, return all items|
|Exact|SomeFile.txt|Return only items that match the exact filename|
|Contains|abc_file|Return only items that contain the given string|
|Wildcard|*.txt|Return only items that match the given wildcard, multiple wildcards can be applied|
|Regex|^.*file.*something$|Return only items that match the given regex|

![](resources/images/filters.png)

## Substitutions
For the tasks that have substitutions available (Backup file, Batch Transfer), the following placeholders can be used
|Placeholder|Example Source|Example Pattern|Example Replacement|Description|
|---|---|---|---|---|
|{source_filename}|SomeFile.txt|{source_filename}.bak|SomeFile.bak|The source filename without extension|
|{source_extension}|SomeFile.txt|AA{source_filename}.{source_extension}|AASomeFile.txt|The source filename extension|
|{date}|SomeFile.txt|{date}_{source_filename}.{source_extension}.bak|2022-01-01_SomeFile.txt.bak|The current date in YYYY-MM-DD format|
|{time}|SomeFile.txt|{date}_{time}_transfer.txt|2022-01-01_120120_transfer.txt|The current time in HHMMSS format|
|{guid}|SomeFile.txt|{guid}_{source_filename}.{source_extension}|3d8759f5-aef8-4f5d-885a-e36803ac3d91_SomeFile.txt|The GUID for the batch item|
|{incremental}|SomeFile.txt|{source_filename}_{incremental}.{source_extension}|SomeFile.txt_1.txt|The incremental number for the batch item (requires configuration storage, see under batch transfer below)|


## Direct File Operations

### Connection Tab
The connection tab on the below options sets the server to perform the operation on. If you have a Json object in the environment storage, you can choose the "Json" option and enter the path to the environment variable containing the Json object. You can also enter a Json config directly, or choose one of the server types and enter the information manually.

### List Files
List all files in a directory/path on the server, or apply patterns and find matching files.

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|Path|string|/some/path|The path to the directory on the server to list|
|Filter|enum|Regex|The type of filter to apply to the listing(see Filters above)
|Pattern|string|^SomeFile.txt$|The pattern to apply to the listing (see Filters above)|

![](resources/images/listfiles.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Count|int|3|The number of files found|
|Files|array of strings|["SomeFile.txt", "SomeOtherFile.txt", "SomeOtherFile.txt"]|The list of files found|

### Read File
Read the file contents into a string.

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|Path|string|/some/path/to/|The path to the folder containing the files (or directly to the file, in that case leave the File option empty)|
|File|string|SomeFile.txt|The name of the file to read|
|Encoding|enum|UTF-8|The encoding to use when reading the file|

![](resources/images/readfile.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Content|string|Some contents|The contents of the file|
|Path|string|/some/path/to/SomeFile.txt|The path to the file|
|Encoding|string|UTF-8|The encoding used when reading the file|

### Write File
Write a string to a file

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|Path|string|/some/path/to/|The path to the folder to write the file to (or directly to the file, in that case leave the File option empty)|
|File|string|SomeFile.txt|The name of the file to write|
|Content|string|Some content|The content to write to the file|
|Overwrite|bool|true|Whether to overwrite the file if it already exists|
|Encoding|enum|UTF-8|The encoding to use when writing the file|

![](resources/images/writefile.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Success|bool|true|Whether the file was successfully written|
|Path|string|/some/path/to/SomeFile.txt|The path to the file|
|Encoding|string|UTF-8|The encoding used when writing the file|

### Delete File
Delete a file from the server

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|Path|string|/some/path/to/|The path to the folder containing the files (or directly to the file, in that case leave the File option empty)|
|File|string|SomeFile.txt|The name of the file to delete|

![](resources/images/deletefile.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Success|bool|true|Whether the file was successfully deleted|
|Path|string|/some/path/to/SomeFile.txt|The path to the file|
### Copy File
Copy a file from one server to another

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|(Source) Path|string|/some/path/to/|The path to the folder containing the files (or directly to the file, in that case leave the File option empty)|
|(Source) File|string|SomeFile.txt|The name of the file to copy|
|(Source) Encoding|enum|UTF-8|The encoding to use when reading the file|
|(Destination) Path|string|/some/path/to/|The path to the folder to copy the file to|
|(Destination) File|string|SomeFile.txt|The name of the file to copy to|
|(Destination) Overwrite|bool|true|Whether to overwrite the file if it already exists|
|(Destination) Encoding|enum|UTF-8|The encoding to use when writing the file|

![](resources/images/copyfile_src.png)
![](resources/images/copyfile_dst.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Success|bool|true|Whether the file was successfully copied|

### Create Directory
Create a directory on the server

#### Input
|Property|Type|Example Value|Description|
|---|---|---|---|
|Path|string|/some/path/to/|The path to the folder to create|
|Recursive|bool|true|Whether to create any missing parent directories|

![](resources/images/createdir.png)

#### Output
|Property|Type|Example Value|Description|
|---|---|---|---|
|Success|bool|true|Whether the directory was successfully created|

## Batch Transfers
### General configuration
Enabled true/false - Whether to enable batch transfers.

![](resources/images/batchcfg.png)

### Configuration Storage
For batch transfers, there is the option to store the persistent information for a task on a remote server. This is currently only used for storing incremental values, but will be extended to cover storage of logs and statistics.
|Property|Type|Example Value|Description|
|---|---|---|---|
|Use config server|bool|true|Whether to store configuration on a remote server|
|Config server|string(json)|ServerConfigurationJson|The json string containing the server configuration|
|Config path|string|somefolder/config|The path on the remote server to store the configuration|

### Backup Storage
The option to store the copied files on a remote server for retention, useful if deleting the source file on every run.
|Property|Type|Example Value|Description|
|---|---|---|---|
|Backup files|bool|true|Whether to store the backup files on a remote server|
|Backup to config server|bool|true|Whether to use the same server for configuration and backup storage|
|Backup server|string(json)|ServerConfigurationJson|The json string containing the server configuration|
|Backup path|string|somefolder/backup|The path on the remote server to store the backup files|
|Backup filename|string|{source_filename}_{incremental}.{source_extension}|The filename to use for the backup files|

### Batch Item
A batch item is a set of configuration used to move one specific type of file. Each item has a unique identifier and a unique series of numbers (identifiers) if storage is enabled.



|Property|Type|Example Value|Description|
|---|---|---|---|
|Object Guid|string|3d8759f5-aef8-4f5d-885a-e36803ac3d91|The GUID for the batch item, can also be any other unique identifier string|
|Source Server|string(json)|ServerConfigurationJson|The json string containing the server configuration|
|Source Path|string|somefolder/source|The path on the remote server to store the source files|
|Source Filter Type|enum|Regex|The type of filter to apply to the source pattern(see Filters above) to find files|
|Source Filter Pattern|string|*.txt|The pattern to use to find files|
|Source Encoding|enum|UTF-8|The encoding to use for reading the source files|
|Destination server|string(json)|ServerConfigurationJson|The json string containing the server configuration|
|Destination Path|string|somefolder/destination|The path on the remote server to store the destination files|
|Destination Filename|string|{source_filename}_{incremental}.{source_extension}|The filename to use for the destination files. Substitution is available (see above)|
|Destination Encoding|enum|UTF-8|The encoding to use for writing the destination files|
|Overwrite|bool|true|Whether to overwrite existing files|
|Delete source|bool|true|Whether to delete the source file after copying|

![](resources/images/batchitem.png)

The configuration can also be stored in a json env variable or string and loaded with the Batch Transfer (JSON) task.

### Batch Results
The result of the batch items

|Property|Type|Example Value|Description|
|---|---|---|---|
|Count|int|10|The number of items in the batch successfully transferred|
|Results|List<BatchResult>|[{...},{...}]|The list of results for each item in the batch|

### Batch Result
The result of a single batch item

|Property|Type|Example Value|Description|
|---|---|---|---|
|Object Guid|string|3d8759f5-aef8-4f5d-885a-e36803ac3d91|The GUID for the batch item, can also be any other unique identifier string|
|SourceFile|string|user@server:/some/path/to/SomeFile.txt|The path to the source file|
|DestinationFile|string|user@server:/some/path/to/SomeFile.txt|The path to the destination file|
|Success|bool|true|Whether the file was successfully transferred|
|Error|string|The error message if the file was not successfully transferred|
|Timestamp|datetime|2018-01-01T00:00:00Z|The timestamp when the file was transferred|


## Development
If you have an idea, an enhancement or want to add another remote module feel free to submit a pull request.

### Adding modules for remote types
There are 4 things that need to be edited to add another type of remote:

#### Create a file
Create a new file named Frends.HIT.RemoteFS.[RemoteType].cs with the following structure.
Keep in mind that for the functions where input specifies "Path" and "File", the following can be assumed:

- The entire path can be in the "Path" parameter, with "File" left empty
- The path can be split between the "Path" and "File" parameters
- Ensure that slashes are added before/after the path/file parameters to join the path correctly
- Ensure that the path doesn't start with a / if the parameter doesn't specify it, and vice versa

Use the Path.Join function to create a full path to the file.

```cs
using System.IO;

namespace Frends.HIT.RemoteFS;

public class [RemoteType]
{
    /// <summary>
    /// List files in a directory
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static List<string> ListFiles(ListParams input, ServerConfiguration connection)
    {
        // Insert code to list files in a directory
        // Should return an unfiltered list of filenames (not full paths)
    }
    
    /// <summary>
    /// Read a file
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static string ReadFile(ReadParams input, ServerConfiguration connection)
    {
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        string actualPath = Path.Join(input.Path, input.File);

        // Insert code to get the content from a file with the encoding in encType
        // Should return a string with the file contents
    }

    /// <summary>
    /// Write a file
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static void WriteFile(WriteParams input, ServerConfiguration connection)
    {
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        string actualPath = Path.Join(input.Path, input.File);

        // Insert code to write to a file with the encoding in encType
        // This code should make a check that input.Overwrite is true before overwriting any file
        // No return on success, exception on failure
    }
    
    /// <summary>
    /// Create a directory
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        // Insert code to create a directory at the given path
        // Should honor the input.Recursive option on whether to create all dirs in a path
        // Should check if a file exists at the path, and throw an exception if it does
    }
    
    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        string actualPath = Path.Join(input.Path, input.File);

        // Insert code to delete a file (not dir)
    }
}


```
