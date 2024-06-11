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
public class PulsenCombine
{
    private class FinancialFileInfo
    {
        public Int64 CreatedDate { get; set; }
        public string Description { get; set; }
        public string Href { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
    }

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
        client.DefaultRequestHeaders.Add("X-Api-Version", "1");
        return client;
    }

    private static async Task<Dictionary<string, FinancialFileInfo>> GetFilenameMap(HttpClient client, string host, string path)
    {
        var fileList = new Dictionary<string, FinancialFileInfo>();
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        using (var response = await client.GetAsync($"https://{host}/{path}"))
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var files = JsonConvert.DeserializeObject<Dictionary<string, List<FinancialFileInfo>>>(content);

                foreach (var file in files["DtoFinancialFile"])
                {
                    fileList.Add(file.Name, file);
                }
            }
        }

        client.DefaultRequestHeaders.Remove("Accept");

        return fileList;
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

        if (input.ListType == ObjectTypes.Directories) {
            if (input.Path.TrimEnd('/') != "financialfiles") {
                return fileList;           
            }

            fileList.Add("generalledger");
            fileList.Add("customerledger");
            fileList.Add("paymentssus");

            return fileList;
        }

        using (var client = GetHttpClient(connection))
        {
            Console.WriteLine("Getting file list ");
            var fileDict = await GetFilenameMap(client, connection.Address, input.Path);
            fileList = fileDict.Keys.ToList();
            Console.WriteLine("File list gotten ");
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
            Console.WriteLine("Getting file map ");
            var fileDict = await GetFilenameMap(client, connection.Address, input.Path);
            if (fileDict.ContainsKey(input.File))
            {
                Console.WriteLine("Match exists, getting ", fileDict[input.File].Href);

                using (var response = await client.GetStreamAsync(fileDict[input.File].Href))
                {
                    using (var memstr = new MemoryStream())
                    {
                        response.CopyTo(memstr);
                        return memstr.ToArray();
                    }
                }
            }
            else
            {
                throw new Exception($"File {input.File} not found in {input.Path}");
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
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// Delete a file from an FTP Server
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// /// 
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        using (var client = GetHttpClient(connection))
        {
            Console.WriteLine("Getting file map ");
            var fileDict = await GetFilenameMap(client, connection.Address, input.Path);
            if (fileDict.ContainsKey(input.File))
            {
                Console.WriteLine("Match exists, getting ", fileDict[input.File].Href);
                var response = await client.DeleteAsync(fileDict[input.File].Href);
                return response.IsSuccessStatusCode;
            }
            else
            {
                throw new Exception($"File {input.File} not found in {input.Path}");
            }
        }
    }
}