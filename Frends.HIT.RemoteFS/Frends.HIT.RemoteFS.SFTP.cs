﻿using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace Frends.HIT.RemoteFS;

public class SFTP
{
    public static string GetConnectionString(ServerConfiguration config)
    {
        return String.Join("|", new List<string>(){
            config.ConnectionType.ToString(),
            config.Address,
            config.Domain,
            config.Username,
            config.Password,
            config.PrivateKey,
            config.PrivateKeyPassword,
            config.Fingerprint
        });
    }

    public static async Task<dynamic> DoMethod(string action, object[] parameters)
    {
        switch (action)
        {
            case "ListFiles":
                return await ListFiles((ListParams)parameters[0], (ServerConfiguration)parameters[1]);
            case "ReadFile":
                return await ReadFile((ReadParams)parameters[0], (ServerConfiguration)parameters[1]);
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
    /// List files in a directory on an SFTP server
    /// </summary>
    /// <param name="input">The path to the directory to list, and regex to filter files</param>
    /// <param name="connection">The connection details for the server</param>
    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            client.Connect();
            var listing = new List<string>(client.ListDirectory(input.Path).Select(x => x.Name));
            client.Disconnect();
            return listing;
        }
    }
    
    /// <summary>
    /// Read a file from an SFTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<byte[]> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);

        try
        {
            using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
            {
                client.Connect();
                var result = client.ReadAllBytes(path);
                client.Disconnect();
                return result;
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error reading path {path} from server", e);
        }
    }
    
    /// <summary>
    /// Write a file to a SFTP server
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);

        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {   
            client.Connect();
            // Check if the file exists
            if (client.Exists(path))
            {
                if (!input.Overwrite)
                {
                    client.Disconnect();
                    throw new Exception($"File at path {path} already exists");
                }
                client.Delete(path);
            }
            
            // Write to the file
            client.WriteAllBytes(path, input.ByteContent);
            client.Disconnect();
        }

        return true;
    }

    /// <summary>
    /// Create a directory on a SFTP server
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
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
                            client.Disconnect();
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

        return true;
    }
    
    /// <summary>
    /// Delete a file from an SFTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        string path = Helpers.JoinPath("/", input.Path, input.File);

        using (var client = new SftpClient(Helpers.GetSFTPConnectionInfo(connection)))
        {
            client.Connect();
            client.Delete(path);
            client.Disconnect();
        }

        return true;
    }
}