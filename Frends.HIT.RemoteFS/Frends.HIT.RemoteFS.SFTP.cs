using System.Text;
using Renci.SshNet;

namespace Frends.HIT.RemoteFS;

public class SFTP
{
    /// <summary>
    /// List files in a directory on an SFTP server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    /// <returns></returns>
    public static List<string> ListFiles(ListParams input, ServerConfiguration connection)
    {
        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            client.Connect();
            var listing = client.ListDirectory(input.Path);
            client.Disconnect();
            return new List<string>(listing.Select(x => x.Name));
        }
    }
    
    public static string ReadFile(ReadParams input, ServerConfiguration connection)
    {
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);

        string path = "";

        if (string.IsNullOrEmpty(input.File))
        {
            path = input.Path;
        }
        else
        {
            if (input.Path.EndsWith("/"))
            {
                path = input.Path + input.File;
            }
            else
            {
                path = string.Join("/", input.Path, input.File);
            }
        }

        try
        {
            using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
            {
                client.Connect();
                var file = client.ReadAllText(path, encType);
                client.Disconnect();

                return file.ToString();
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error reading path {path} from server", e);
        }
    }
    
    public static void WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = "";

        if (string.IsNullOrEmpty(input.File))
        {
            path = input.Path;
        }
        else
        {
            if (input.Path.EndsWith("/"))
            {
                path = input.Path + input.File;
            }
            else
            {
                path = string.Join("/", input.Path, input.File);
            }
        }

        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);

        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            // Connect to the server
            client.Connect();
            
            // Check if the file exists
            if (client.Exists(path))
            {
                if (!input.Overwrite)
                {
                    throw new Exception($"File at path {path} already exists");
                }
                client.Delete(path);
            }
            
            // Write to the file
            client.WriteAllText(path, input.Content, encType);
        }
    }
}