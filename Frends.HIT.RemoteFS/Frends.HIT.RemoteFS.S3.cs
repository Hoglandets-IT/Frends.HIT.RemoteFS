using Amazon.S3;
using Amazon.S3.Model;
using FluentFTP.Helpers;
using Genbox.SimpleS3.Core.Extensions;

namespace Frends.HIT.RemoteFS;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


/// <summary>
/// Connection to Pulsen Combine server
/// </summary>
public class S3
{

    private static AmazonS3Client GetS3Client(ServerConfiguration connection)
    {
        AmazonS3Client client = new AmazonS3Client(connection.SecretId, connection.SecretKey, new AmazonS3Config {
            ServiceURL = $"https://{connection.Address}",
            AuthenticationRegion = connection.S3Region,
            Timeout = System.TimeSpan.FromSeconds(10),
        });

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

    /// <summary>
    /// List files on the Pulsen Combine server
    /// </summary>
    /// <param name="input">What files to list (e.g. financialfiles/generalledger)</param>
    /// <param name="connection">The connection details for the server</param>

    public static async Task<List<string>> ListFiles(ListParams input, ServerConfiguration connection)
    {
        List<string> fileList = new List<string>();

        using (var client = GetS3Client(connection))
        {
            var noPrefixPath = Strings.RemovePrefix(input.Path, "/");

            var response = await client.ListObjectsV2Async(new ListObjectsV2Request{
                BucketName = connection.S3Bucket,
                Prefix = noPrefixPath,
            });

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"Failed to list files: {response.HttpStatusCode}");
            }

            foreach (var item in response.S3Objects) {
                var pureKey = Strings.RemovePrefix(item.Key, noPrefixPath);
                if (pureKey == "") {
                    continue;
                }
                if (pureKey.Contains('/')) {
                    if (pureKey.EndsWith("/") && (input.ListType == ObjectTypes.Directories || input.ListType == ObjectTypes.Both)) {
                        fileList.Add(Strings.RemovePostfix(pureKey, "/"));
                    }
                    continue;
                }

                fileList.Add(pureKey);
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
        string path = Strings.RemovePrefix(Helpers.JoinPath("/", input.Path, input.File), "/");

        using (var client = GetS3Client(connection)) {
            var response = await client.GetObjectAsync(new GetObjectRequest{
                BucketName = connection.S3Bucket,
                Key = path,
            });

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"Failed to read file {input.File} - does not exist: {response.HttpStatusCode}");
            }

            return await response.ResponseStream.AsDataAsync();
        }
    }

    /// <summary>
    /// Write a file to a FTP server
    /// </summary>
    /// <param name="input">The params to identify the file and contents</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> WriteFile(WriteParams input, ServerConfiguration connection)
    {
        string path = Strings.RemovePrefix(Helpers.JoinPath("/", input.Path, input.File), "/");

        using (var client = GetS3Client(connection)) {
            if (!input.Overwrite) {
                var exists = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest{
                    BucketName = connection.S3Bucket,
                    Key = path,
                });

                if (exists.HttpStatusCode == System.Net.HttpStatusCode.OK && !input.Overwrite) {
                    throw new Exception($"Failed to write file {input.File} - already exists");
                }
            }

            var response = await client.PutObjectAsync(new PutObjectRequest{
                BucketName = connection.S3Bucket,
                Key = path,
                InputStream = new MemoryStream(input.ByteContent),
            });

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"Failed to write file {input.File}: {response.HttpStatusCode}");
            }

            return true;
        }
        
    }

    /// <summary>
    /// Create a directory on a FTP server
    /// </summary>
    /// <param name="input">The params to identify the directory</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        throw new Exception("Not implemented for this server type: S3. Directories are created automatically when writing files.");
    }

    /// <summary>
    /// Delete a file from an FTP Server
    /// </summary>
    /// <param name="input">The params to identify the file</param>
    /// <param name="connection">The connection settings</param>
    public static async Task<bool> DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        // TODO: Implement this
        return false;
    }
}