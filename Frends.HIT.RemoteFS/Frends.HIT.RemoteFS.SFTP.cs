using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using SharpCifs.Util.Sharpen;

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

    public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            client.Connect();
            if (input.Recursive)
            {
                List<string> tPath = new List<string>();

                foreach (string part in input.Path.Split('/'))
                {
                    tPath.Add(part);
                    try
                    {
                        SftpFileAttributes attrs = client.GetAttributes(string.Join('/', tPath));
                        if (!attrs.IsDirectory)
                        {
                            throw new Exception("There is a file in the way of creating these directories");
                        }
                    }
                    catch (SftpPathNotFoundException)
                    {
                        client.CreateDirectory(string.Join('/', tPath));
                    }
                }
            }
            else
            {
                client.CreateDirectory(input.Path);
            }
            client.Disconnect();
        }
    }

    public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
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
        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            client.Connect();
            client.Delete(path);
            client.Disconnect();
        }
    }
}