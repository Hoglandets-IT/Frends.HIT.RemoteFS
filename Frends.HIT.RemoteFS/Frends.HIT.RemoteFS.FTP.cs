using System.Text;
using FluentFTP;

namespace Frends.HIT.RemoteFS;

public class FTP
{
    /// <summary>
    /// List files in a directory on a FTP server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    /// <returns></returns>
    public static List<string> ListFiles(ListParams input, ServerConfiguration connection)
    {
        var result = new List<string>();

        Int32 port = 21;
        string[] split = connection.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }

        using (FtpClient client = new FtpClient(host, port, connection.Username, connection.Password))
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

    public static string ReadFile(ReadParams input, ServerConfiguration connection)
    {
        Int32 port = 21;
        string[] split = connection.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }

        string path = "";

        if (string.IsNullOrEmpty(input.File.ToString()) || string.IsNullOrWhiteSpace(input.File.ToString()))
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
            
        using (FtpClient client = new FtpClient(host, port, connection.Username, connection.Password))
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
                throw new Exception($"Error reading path {path} from server {host}:{port}", e);
            }
        }
    }
    
    public static void WriteFile(WriteParams input, ServerConfiguration connection)
    {
        Int32 port = 21;
        string[] split = connection.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }

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

        using (FtpClient client = new FtpClient(host, port, connection.Username, connection.Password))
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
                throw new Exception($"Error writing to path {path} to server {host}:{port} (File exists or directory does not exist)");
            }

            memStream.Dispose();
            client.Disconnect();
        }
    }

    public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        Int32 port = 21;
        string[] split = connection.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }
        
        using (FtpClient client = new FtpClient(host, port, connection.Username, connection.Password))
        {
            client.AutoConnect();
            client.CreateDirectory(input.Path, input.Recursive);
            client.Disconnect();
        }
    }

    public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        Int32 port = 21;
        string[] split = connection.Address.Split(':');
            
        string host = split[0];
        if (split.Length > 1) {
            port = Int32.Parse(split[1]);
        }

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
        
        using (FtpClient client = new FtpClient(host, port, connection.Username, connection.Password))
        {
            client.AutoConnect();
            client.DeleteFile(path);
            client.Disconnect();
        }
        
    }
}