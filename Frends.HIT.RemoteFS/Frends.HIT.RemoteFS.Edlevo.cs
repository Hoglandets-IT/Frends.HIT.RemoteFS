using System.Text;
using System.IO;
using FluentFTP;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Frends.HIT.RemoteFS;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


/// <summary>
/// Connection to Pulsen Combine server
/// </summary>
public class Edlevo
{

    private static HttpClient GetHttpClient(ServerConfiguration connection)
    {
        var cert = X509Certificate2.CreateFromPem(connection.Certificate, connection.PrivateKey);
        if (!cert.HasPrivateKey)
        {
            throw new Exception("Private key not found in certificate");
        }

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        handler.ClientCertificates.Add(cert);

        HttpClient client = new HttpClient(handler);

        return client;
    }

    /// <summary>
    /// Execute the method
    /// </summary>
    /// <param name="action">Which method to execute</param>
    /// <param name="parameters">Which parameters to use</param>
    /// <returns></returns>
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
                throw new Exception("CreateDir is not supported for PulsenCombine");
        }

        return true;
    }

    /// <summary>
    /// List files on the Pulsen Combine server
    /// </summary>
    /// <param name="input">What files to list (e.g. financialfiles/generalledger)</param>
    /// <param name="connection">The connection details for the server</param>

    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        List<string> fileList = new List<string>();

        using (var client = GetHttpClient(connection))
        {
            using (var response = await client.GetAsync($"https://{connection.Address}/WE.Education.Integration.Host.Proxy/LES/System/V1/System/ListFileNames?LicenseKey={connection.LicenseKey}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    fileList = content.Split("\r\n").ToList();
                }
                else {
                    throw new Exception($"Failed to list files: {response.ReasonPhrase}");
                }
            }
        }

        return fileList;
    }

    /// <summary>
    /// Read a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<byte[]> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        using (var client = GetHttpClient(connection))
        {
            using (var response = await client.GetAsync($"https://{connection.Address}/WE.Education.Integration.Host.Proxy/LES/System/V1/System/GetFile?Filename={input.File}&LicenseKey={connection.LicenseKey}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    Console.WriteLine(content);
                    Console.WriteLine(content.Length);
                    return content;
                }
                else {
                    throw new Exception($"Failed to read file {input.File} - does not exist: {response.ReasonPhrase}");
                }
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
        throw new Exception("Not implemented for this server type");
    }

    /// <summary>
    /// Create a directory on a FTP server
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        throw new Exception("Not implemented for this server type");
    }

    /// <summary>
    /// Delete a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        using (var client = GetHttpClient(connection))
        {
            using (var response = await client.DeleteAsync($"https://{connection.Address}/WE.Education.Integration.Host.Proxy/LES/System/V1/System/DeleteFile?Filename={input.File}&LicenseKey={connection.LicenseKey}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else {
                    throw new Exception($"Failed to delete file {input.File}: {response.ReasonPhrase}");
                }
            }
        }
    }
}