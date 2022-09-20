using System.Text;
using System.IO;
using FluentFTP;

namespace Frends.HIT.RemoteFS;

public class FTP
{
    public static async Task<dynamic> DoMethod(string action, object[] parameters)
    {
        switch (action)
        {
            case "ListFiles":
                return await ListFiles((ListParams)parameters[0], (ServerConfiguration)parameters[1]);
                break;
            case "ReadFile":
                return await ReadFile((ReadParams)parameters[0], (ServerConfiguration)parameters[1]);
                break;
            case "WriteFile":
                await WriteFile((WriteParams)parameters[0], (ServerConfiguration)parameters[1]);
                break;
            case "DeleteFile":
                await DeleteFile((DeleteParams)parameters[0], (ServerConfiguration)parameters[1]);
                break;
            case "CreateDir":
                await CreateDir((CreateDirParams)parameters[0], (ServerConfiguration)parameters[1]);
                break;
        }

        return true;
    }
    
    /// <summary>
    /// List files in a directory on a FTP server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        var result = new List<string>();

        using (FtpClient client = Helpers.GetFTPConnection(connection))
        {
            client.AutoConnect();
            var listing = client.GetListing(input.Path, FtpListOption.Modify);
            client.Disconnect();
            
            foreach (FtpListItem item in listing)
            {
                if (item.Type == FtpFileSystemObjectType.File)
                {
                    result.Add(item.Name);
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// Read a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<string> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
        using (FtpClient client = Helpers.GetFTPConnection(connection))
        {
            try
            {
                client.AutoConnect();

                var memStream = new MemoryStream();
                var file = client.Download(memStream, path);

                client.Disconnect();

                return encType.GetString(memStream.ToArray());
            }
            catch (Exception e)
            {
                throw new Exception($"Error reading path {path} from server {connection.Address}", e);
            }
        }
    }
    
    /// <summary>
    /// Write a file to a FTP server
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);

        using (FtpClient client = Helpers.GetFTPConnection(connection))
        {
            client.AutoConnect();
           
            FtpRemoteExists overwrite = FtpRemoteExists.Overwrite;
            if (input.Overwrite)
            {
                overwrite = FtpRemoteExists.Overwrite;
            }
            
            var memStream = new MemoryStream(encType.GetBytes(input.Content));
            var file = client.Upload(memStream, path, overwrite, false);
            if (file == FtpStatus.Failed || file == FtpStatus.Skipped)
            {
                throw new Exception($"Error writing to path {path} on server {connection.Address} (File exists or directory does not exist)");
            }

            memStream.Dispose();
            client.Disconnect();
        }

        return true;
    }

    /// <summary>
    /// Create a directory on a FTP server
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        using (FtpClient client = Helpers.GetFTPConnection(connection))
        {
            client.AutoConnect();
            client.CreateDirectory(input.Path, input.Recursive ?? false);
            client.Disconnect();
        }

        return true;
    }

    /// <summary>
    /// Delete a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);

        using (FtpClient client = Helpers.GetFTPConnection(connection))
        {
            client.AutoConnect();
            client.DeleteFile(path);
            client.Disconnect();
        }

        return true;
    }
}