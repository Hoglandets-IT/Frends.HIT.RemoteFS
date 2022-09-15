using EzSmb;
using EzSmb.Params;
using EzSmb.Params.Enums;

using System;
using System.Text;
using System.Threading.Tasks;
namespace Frends.HIT.RemoteFS;
public class SMB
{
    private static async Task<Node[]> GetNodes(string path, ParamSet paramSet)
    {
        EzSmb.Node server = await EzSmb.Node.GetNode(path, paramSet);
        Node[] nodes = await server.GetList();
        server.Dispose();
        return nodes;
    }

    /// <summary>
    /// List files in a directory on a SMB server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        var server = await Node.GetNode(Helpers.JoinPath("/", connection.Address, input.Path), new EzSmb.Params.ParamSet()
        {
            UserName = connection.Username,
            Password = connection.Password,
            DomainName = connection.Domain
        });

        var retLst = new List<string>();
        var listing = await server.GetList("*");
        foreach (Node node in listing)
        {
            if (node.Type == NodeType.File)
            {
                retLst.Add(node.Name);
            }
        }
        server.Dispose();
        return retLst;
    }
    
    // /// <summary>
    // /// Read a file from an SMB Server
    // /// </summary>
    // /// <param name="input">The params to identify the file</param>
    // /// <param name="connection">The connection settings</param>
    // public static string ReadFile(ReadParams input, ServerConfiguration connection)
    // {
    //     Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
    //     var file = new SmbFile(Helpers.GetSMBConnectionString(connection, input.Path, input.File));

    //     if (file.Exists())
    //     {
    //         var readStream = file.GetInputStream();
    //         var memStream = new MemoryStream();
            
    //         ((Stream)readStream).CopyTo(memStream);
    //         readStream.Dispose();
            
    //         return encType.GetString(memStream.ToArray());
    //     }
        
    //     throw new Exception($"File {input.Path}/{input.File} does not exist");
    // }

    // /// <summary>
    // /// Write a file to a SMB server
    // /// </summary>
    // /// <param name="input">The params to identify the file and contents</param>
    // /// <param name="connection">The connection settings</param>
    // public static void WriteFile(WriteParams input, ServerConfiguration connection)
    // {
    //     Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
    //     var file = new SmbFile(Helpers.GetSMBConnectionString(connection, input.Path, input.File));

    //     if (file.Exists())
    //     {
    //         if (!input.Overwrite)
    //         {
    //             throw new Exception($"File {input.Path}/{input.File} already exists and Overwrite is not enabled");
    //         }
    //         file.Delete();
    //     }
        
    //     file.CreateNewFile();
        
    //     var writeStream = file.GetOutputStream();
    //     writeStream.Write(encType.GetBytes(input.Content));
    //     writeStream.Dispose();
    // }
    
    // /// <summary>
    // /// Create a directory on a SMB server
    // /// </summary>
    // /// <param name="input">The params to identify the directory</param>
    // /// <param name="connection">The connection settings</param>
    // public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    // {
    //     var folder = new SmbFile(Helpers.GetSMBConnectionString(connection, input.Path, ""));

    //     if (folder.IsFile())
    //     {
    //         throw new Exception("The path cannot be created because there is a file with the same name present");
    //     }

    //     if (!folder.IsDirectory())
    //     {
    //         if (input.Recursive ?? false)
    //         {
    //             folder.Mkdirs();
    //         }
    //         else
    //         {
    //             folder.Mkdir();
    //         }    
    //     }
    // }
    
    // /// <summary>
    // /// Delete a file from an FTP Server
    // /// </summary>
    // /// <param name="input">The params to identify the file</param>
    // /// <param name="connection">The connection settings</param>
    // public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
    // {
    //     new SmbFile(Helpers.GetSMBConnectionString(connection, input.Path, input.File)).Delete();
    // }
}