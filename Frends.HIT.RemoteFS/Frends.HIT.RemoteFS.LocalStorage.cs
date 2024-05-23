using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Frends.HIT.RemoteFS;

public class LocalStorage
{
    public static async Task<dynamic> DoMethod(string action, object[] parameters)
    {
        switch (action)
        {
            case "ListFiles":
                return await ListFiles((ListParams)parameters[0], (ServerConfiguration)parameters[1]);

            case "ReadFile":
                return await ReadFile((ReadParams)parameters[0], (ServerConfiguration)parameters[1]);

            case "WriteFile":
                return await WriteFile((WriteParams)parameters[0], (ServerConfiguration)parameters[1]);

            case "DeleteFile":
                return await DeleteFile((DeleteParams)parameters[0], (ServerConfiguration)parameters[1]);

            case "CreateDir":
                return await CreateDir((CreateDirParams)parameters[0], (ServerConfiguration)parameters[1]);
        }

        return true;
    }
    
    /// <summary>
    /// List files in a directory on the local filesystem
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        try
        {
            var folder = new DirectoryInfo(input.Path);

            if (input.ListType == ObjectTypes.Files) return folder.GetFiles().Select(x => x.Name).ToList();
            if (input.ListType == ObjectTypes.Directories) return folder.GetDirectories().Select(x => x.Name).ToList();
            if (input.ListType == ObjectTypes.Both) {
                var files = folder.GetFiles().Select(x => x.Name).ToList();
                var dirs = folder.GetDirectories().Select(x => x.Name).ToList();
                files.AddRange(dirs);
                return files;
            }

            throw new Exception("Invalid ListType");
            
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
    public static async Task<byte[]> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);

        try
        {
            var file = File.ReadAllBytes(path);
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
    public static async Task<bool> WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);
        
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
            
            File.WriteAllBytes(path, input.ByteContent);
        }
        catch (Exception e)
        {
            throw new Exception("Error writing file at " + path, e);
        }

        return true;
    }
    
    /// <summary>
    /// Create a directory on the local filesystem
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        if (File.Exists(input.Path))
        {
            throw new Exception("File already exists at " + input.Path);
        }
        
        if (!Directory.Exists(input.Path))
        {
            Directory.CreateDirectory(input.Path);
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
        string path = Helpers.JoinPath(Helpers.OSDirSeparator, input.Path, input.File);

        if (!Directory.Exists(path))
        {
            throw new Exception("File does not exist at " + path + ", is a directory");
        }

        return true;
    }
}