using System.Text;
using Newtonsoft.Json;

namespace Frends.HIT.RemoteFS;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


/// <summary>
/// Connection to Pulsen Combine server
/// </summary>
public class Speedadmin
{

    private static HttpClient GetHttpClient(ServerConfiguration connection)
    {
        if (string.IsNullOrEmpty(connection.SecretKey)) {
            throw new Exception("Secret key is required for Speedadmin API");
        }

        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        HttpClient client = new HttpClient(handler);

        client.DefaultRequestHeaders.Add("Authorization", connection.SecretKey);

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
                return await CreateDir((CreateDirParams)parameters[0], (ServerConfiguration)parameters[1]);
        }

        return true;
    }

    private class BookkepingsListResult {
        public int BookkeepingId { get; set; }
        public string BookkeepingText { get; set; }
        public string BookkeepingStatus { get; set; }
        public string StatusText { get; set; } 
    }

    private class BookkeepingsListResponse {
        public int TotalResults { get; set; }
        public int RequestResults { get; set; }

        public List<BookkepingsListResult> Results { get; set; }
    }

    /// <summary>
    /// List files on the Speedadmin API
    /// </summary>
    /// <param name="input">What files to list (e.g. financialfiles/generalledger)</param>
    /// <param name="connection">The connection details for the server</param>

    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        List<string> fileList = new List<string>();

        using (var client = GetHttpClient(connection))
        {
            using( var resp = await client.PostAsync($"https://{connection.Address}/v1/bookkeepings/", new StringContent(
                JsonConvert.SerializeObject(new {
                    Take=500,
                    Skip=0
                }), Encoding.UTF8, "application/json"
            ))) {
                if (resp.IsSuccessStatusCode) {
                    var content = await resp.Content.ReadAsStringAsync();
                    var bookkeepings = JsonConvert.DeserializeObject<BookkeepingsListResponse>(content);

                    if (bookkeepings.TotalResults == 0) {
                        return fileList;
                    }

                    foreach (var b in bookkeepings.Results) {
                        fileList.Add(b.BookkeepingText + "||" + b.BookkeepingId);
                    }
                } else {
                    throw new Exception($"Failed to list files: {resp.ReasonPhrase}");
                }
            }
        }

        return fileList;
    }

    /// <summary>
    /// Read a file from the Speedadmin API
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<byte[]> ReadFile(ReadParams input, ServerConfiguration connection)
    {
        var splitFilename = input.File.Split("||");
        if (splitFilename.Length != 2) {
            throw new Exception($"Invalid file name {input.File}: Want format filename||fileID");
        }

        var fileId = input.File.Split("||")[1];
        if (!int.TryParse(fileId, out int fileIdInt)) {
            throw new Exception($"Failed to parse file id {fileId}: not a number");
        }

        using (var client = GetHttpClient(connection))
        {
            using (var response = await client.GetAsync($"https://{connection.Address}/v1/bookkeepings/{fileId}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    if (content.Length == 0) {
                        throw new Exception($"Failed to read file {input.File} - no content: {response.ReasonPhrase}");
                    }

                    // var markPending = await client.PostAsJsonAsync($"https://{connection.Address}/v1/bookkeepings/{fileId}/setstatus", new {
                    //     BookkeepingId=fileIdInt,
                    //     Status="IntegratedExportPending",
                    //     Text="Read by integration platform"
                    // });

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
        var splitFilename = input.File.Split("||");
        if (splitFilename.Length != 2) {
            throw new Exception($"Invalid file name {input.File}: Want format filename||fileID");
        }

        var fileId = input.File.Split("||")[1];
        if (!int.TryParse(fileId, out int fileIdInt)) {
            throw new Exception($"Failed to parse file id {fileId}: not a number");
        }

        using (var client = GetHttpClient(connection))
        {
            using( var resp = await client.PostAsync("https://{connection.Address}/v1/bookkeepings/setstatus", new StringContent(
                JsonConvert.SerializeObject(new {
                    BookkeepingId=fileIdInt,
                    Status="IntegratedExportDone",
                    Text="Read by integration platform"
                }), Encoding.UTF8, "application/json"
            ))) {
                if (!resp.IsSuccessStatusCode) {
                    throw new Exception($"Failed to mark file {input.File} as pending: {resp.ReasonPhrase}");
                }

                return true;
            }

            throw new Exception("Error while making request");
        }
    }
}
