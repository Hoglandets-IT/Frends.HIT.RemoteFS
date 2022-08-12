using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Frends.HIT.RemoteFS;

public class LocalStorage
{
    /// <summary>
    /// List files in a directory on the local filesystem
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static List<string> ListFiles(ListParams input, ServerConfiguration connection)
    {
        try
        {
            var folder = new DirectoryInfo(input.Path);
            return folder.GetFiles().Select(x => x.Name).ToList();
        }
        catch (Exception e)
        {
            throw new Exception("Error reading file at " + input.Path, e);
        }
    }
    
    /// <summary>
    /// Read a file from the local filesystem
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static string ReadFile(ReadParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);

        try
        {

            string file = File.ReadAllText(path, encType);
            return file;
        }
        catch (Exception e)
        {
            throw new Exception("Error reading file at " + input.Path, e);
        }
        
       
    }

    /// <summary>
    /// Write a file to the local filesystem
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static void WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
        try
        {
            // Check if file exists
            if (File.Exists(path))
            {
                if (!input.Overwrite)
                {
                    throw new Exception("File already exists at " + path + " and overwrite is set to false");
                }

                File.Delete(path);
            }
            
            File.WriteAllText(path, input.Content, encType);
        }
        catch (Exception e)
        {
            throw new Exception("Error writing file at " + path, e);
        }
    }
    
    /// <summary>
    /// Create a directory on the local filesystem
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        if (File.Exists(input.Path))
        {
            throw new Exception("File already exists at " + input.Path);
        }
        
        if (!Directory.Exists(input.Path))
        {
            Directory.CreateDirectory(input.Path);
        }
    }
    
    /// <summary>
    /// Delete a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);

        if (!Directory.Exists(path))
        {
            throw new Exception("File does not exist at " + path + ", is a directory");
        }
    }
}