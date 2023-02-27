using EzSmb;
using EzSmb.Params;
using EzSmb.Params.Enums;

using System;
using System.Text;
using System.Threading.Tasks;
namespace Frends.HIT.RemoteFSDebug;
public class SMB
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
    /// List files in a directory on a SMB server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        List<string> res = new List<string>();
        var server = await Node.GetNode(Helpers.JoinPath("/", connection.Address, input.Path),
            Helpers.GetSMBConnectionParams(connection),
            true
        );

        if (server == null)
        {
            throw new Exception(
                $"Could not establish connection, or path does not exist. Please double-check your connection parameters. ({Helpers.JoinPath(connection.Address, input.Path, "FilterBy"+input.Filter.ToString(), input.Pattern)}"
                );
        }
        
        var listing = await server.GetList();
        if (listing == null)
        {
            return res;
        }
        foreach (Node nod in listing)
        {
            if (nod.Type == NodeType.File)
            {
                res.Add(nod.Name);
            }
            
            // if (input.ListType == ObjectTypes.Both)
            // {
            //     res.Add(nod.Name);    
            // }
            // else if (input.ListType == ObjectTypes.Directories && nod.Type == NodeType.Folder)
            // {
                // res.Add(nod.Name);
            // }
            // else if (input.ListType == ObjectTypes.Files && nod.Type == NodeType.File)
            // {
                // res.Add(nod.Name);
            // }
        }

        return res;
    }
   
    
    /// <summary>
    /// Read a file from an SMB Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    
    public static async Task<byte[]> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        var file = await Node.GetNode(Helpers.JoinPath("/", connection.Address, input.Path, input.File),
            Helpers.GetSMBConnectionParams(connection)
        );

        if (file == null)
        {
            throw new Exception(
                $"Could not establish connection, or file does not exist. Please double-check your connection parameters. ({Helpers.JoinPath(connection.Address, input.Path, input.File)})"
                );
        }

        using (var stream = await file.Read())
        {
            return stream.ToArray();
        }
    }

    /// <summary>
    /// Write a file to a SMB server
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> WriteFile(WriteParams input, ServerConfiguration connection)
    {
        var pathParts = Helpers.JoinPath("/", separateLastPart: true, connection.Address, input.Path, input.File);

        var folder = await Node.GetNode(pathParts[0], Helpers.GetSMBConnectionParams(connection));

        if (folder == null)
        {
            throw new Exception(
                $"Could not establish connection, or path does not exist. Please double-check your connection parameters. ({pathParts[0]})"
                );
        }

        var flExists = await folder.GetNode(pathParts[1]);
        if (flExists != null && !input.Overwrite)
        {
            throw new Exception($"File already exists, and overwrite is not set to true. ({pathParts[1]})");
        }
        
        using (var stream = new MemoryStream(input.ByteContent))
        {
            await folder.Write(stream, pathParts[1]);

            flExists = await folder.GetNode(pathParts[1]);
            if (flExists == null)
            {
                throw new Exception($"Could not write file. ({pathParts[1]})");
            }

            var written = await flExists.Read();
            if (!stream.ToArray().SequenceEqual(written.ToArray()))
            {
                throw new Exception($"Could not write file. ({pathParts[1]}): The written information is not the same as the input.");
            }
        }

        return true;
    }
    
    /// <summary>
    /// Create a directory on a SMB server
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        var pathParts = Helpers.JoinPath("/", separateLastPart: true, connection.Address, input.Path);
        var pSet = Helpers.GetSMBConnectionParams(connection);
        
        var parent = await Node.GetNode(pathParts[0], pSet);
        if (parent == null)
        {
            if (input.Recursive == false)
            {
                throw new Exception(
                    $"Could not establish connection, or parent path does not exist. Recursion is not enabled. Please double-check your connection parameters. ({pathParts[0]})"
                );
            }

            var splitPath = pathParts[0].Split('/');
            string recPath = splitPath[0] + '/' + splitPath[1];
            parent = await Node.GetNode(recPath, pSet);
            if (parent == null)
            {
                throw new Exception(
                    $"Cannot create path due to highest level (Server/Share) not existing. Please double-check your connection parameters. ({pathParts[0]})"
                );
            }
            for (int i = 2; i < splitPath.Length; i++)
            {
                recPath += '/' + splitPath[i];
                var recCurrent = await Node.GetNode(recPath, pSet);
                if (recCurrent == null)
                {
                    await parent.CreateFolder(splitPath[i]);
                    parent = await Node.GetNode(recPath, pSet);
                }
            }
        }
        
        await parent.CreateFolder(pathParts[1]);
        return true;
    }
    
    /// <summary>
    /// Delete a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        var folder = await Node.GetNode(Helpers.JoinPath("/", connection.Address, input.Path, input.File), 
            Helpers.GetSMBConnectionParams(connection));

        if (folder == null)
        {
            throw new Exception(
                $"Could not establish connection, or path does not exist. Please double-check your connection parameters. ({Helpers.JoinPath(connection.Address, input.Path, input.File)})"
                );
        }

        await folder.Delete();
        return true;
    }
    
}