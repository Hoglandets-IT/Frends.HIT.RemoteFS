using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Renci.SshNet;
using System.Threading.Tasks;

namespace Frends.HIT.RemoteFS;

/// <summary>
/// Main class for RemoteFS
/// </summary>
[DisplayName("RemoteFS")]
public class Main
{
    [DisplayName("Run Executable")]
    public static string RunExec(string command, string args)
    {
        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
        Console.WriteLine(strExeFilePath);
        Console.WriteLine(strWorkPath);
        using (var pp = new Process())
        {
            string stt = "";
            pp.StartInfo.FileName = command;
            pp.StartInfo.Arguments = args;
            pp.StartInfo.WorkingDirectory = strWorkPath;
            pp.StartInfo.CreateNoWindow = true;
            pp.StartInfo.UseShellExecute = false;
            pp.StartInfo.RedirectStandardOutput = true;
            pp.StartInfo.RedirectStandardError = true;

            pp.OutputDataReceived += (sender, data) => Console.WriteLine("ot" + data.Data);
            pp.ErrorDataReceived += (sender, data) => Console.WriteLine("er" + data.Data);

            pp.Start();
            pp.WaitForExit(1000*10);
            stt = pp.StandardOutput.ReadToEnd().TrimEnd('\n');
            return stt;
        }
    }

    [DisplayName("List Files")]
    public static async Task<ListResult> ListFiles([PropertyTab] ListParams input, [PropertyTab] ServerParams connection)
    {
        List<string> files = new List<string>();
        var serverConfiguration = connection.GetServerConfiguration();
        
        switch (serverConfiguration.ConnectionType)
        {
            case ConnectionTypes.SMB:
                files = await SMB.ListFiles(input, serverConfiguration);
                break;
            case ConnectionTypes.SFTP:
                files = await SFTP.ListFiles(input, serverConfiguration);
                break;
            
            case ConnectionTypes.LocalStorage:
                files = await LocalStorage.ListFiles(input, serverConfiguration);
                break;
            
            case ConnectionTypes.FTP:
                files = await FTP.ListFiles(input, serverConfiguration);
                break;
        }
        
        files = Helpers.GetMatchingFiles(files, input.Pattern, input.Filter);
        
        return new ListResult(
            files.Count(),
            files
        );
    }

    [DisplayName("Read File")]
    public static async Task<ReadResult> ReadFile([PropertyTab] ReadParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        byte[] content = null;
        
        switch (serverConfiguration.ConnectionType)
        {
            case ConnectionTypes.SMB:
                content = await SMB.ReadFile(input, serverConfiguration);
                break;
            case ConnectionTypes.SFTP:
                content = await SFTP.ReadFile(input, serverConfiguration);
                break; 
            case ConnectionTypes.LocalStorage:
                content = await LocalStorage.ReadFile(input, serverConfiguration);
                break;
            case ConnectionTypes.FTP:
                content = await FTP.ReadFile(input, serverConfiguration);
                break;
        }
        
        string encoded = "";
        if (input.Encoding != FileEncodings.RAW) {
            Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
            encoded = encType.GetString(content);
        }

        return new ReadResult(
            content: encoded,
            bytecontent: content,
            path: string.Join("/", new string[] { input.Path, input.File }),
            encoding: input.Encoding
        );
    }

    [DisplayName("Write File")]
    public static async Task<WriteResult> WriteFile([PropertyTab] WriteParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        
        if (input.Encoding != FileEncodings.RAW) 
        {
            Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
            input.ByteContent = encType.GetBytes(input.Content);
        }

        switch (serverConfiguration.ConnectionType)
        {
            case ConnectionTypes.SMB:
                await SMB.WriteFile(input, serverConfiguration);
                break;
            case ConnectionTypes.SFTP:
                await SFTP.WriteFile(input, serverConfiguration);
                break;
            case ConnectionTypes.LocalStorage:
                await LocalStorage.WriteFile(input, serverConfiguration);
                break;
            case ConnectionTypes.FTP:
                await FTP.WriteFile(input, serverConfiguration);
                break;
        }
        
        return new WriteResult(true, string.Join("/", input.Path, input.File), input.Encoding);
    }

    [DisplayName("Create Directory")]
    public static async Task<CreateDirResult> CreateDir([PropertyTab] CreateDirParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        
        switch (serverConfiguration.ConnectionType)
        {
            case ConnectionTypes.SMB:
                await SMB.CreateDir(input, serverConfiguration);
                break;
            case ConnectionTypes.SFTP:
                await SFTP.CreateDir(input, serverConfiguration);
                break;
            
            case ConnectionTypes.LocalStorage:
                await LocalStorage.CreateDir(input, serverConfiguration);
                break;
            
            case ConnectionTypes.FTP:
                await FTP.CreateDir(input, serverConfiguration);
                break;
        }
        
        return new CreateDirResult(true);
    }

    [DisplayName("Delete File")]
    public static async Task<DeleteResult> DeleteFile([PropertyTab] DeleteParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        bool succ = true;
        try
        {
            switch (serverConfiguration.ConnectionType)
            {
                case ConnectionTypes.SMB:
                    await SMB.DeleteFile(input, serverConfiguration);
                    break;
                case ConnectionTypes.SFTP:
                    await SFTP.DeleteFile(input, serverConfiguration);
                    break;

                case ConnectionTypes.LocalStorage:
                    await LocalStorage.DeleteFile(input, serverConfiguration);
                    break;

                case ConnectionTypes.FTP:
                    await FTP.DeleteFile(input, serverConfiguration);
                    break;
            }
        }
        catch (System.NullReferenceException)
        {
            succ = false;
            // throw new Exception(
            // "File not found, or no connection established to server. Check the path and connection parameters.");
        }

        return new DeleteResult(succ, string.Join("/", input.Path, input.File));
    }

    [DisplayName("Copy File")]
    public static async Task<CopyResult> CopyFile(
        [PropertyTab] ReadParams sourceInput,
        [PropertyTab] ServerParams sourceConnection,
        [PropertyTab] CopyDestParams destinationInput,
        [PropertyTab] ServerParams destinationConnection
    )
    {
        var file = await ReadFile(sourceInput, sourceConnection);
        await WriteFile(
            destinationInput.GetWriteParams(
                file.Content,
                file.ByteContent
            ),
            destinationConnection
        );

        return new CopyResult(true);
    }

    [DisplayName("Batch Transfer")]
    public static async Task<BatchResults> BatchTransfer(
        [PropertyTab] BatchConfigParams config,
        [PropertyTab] BatchParams[] input
    )
    {
        var configServer = config.GetConfigurationServerParams();
        var backupServer = config.GetBackupServerParams();
        string backupPath = "";
        string backupPathSubfolder = "";

        List<BatchResult> results = new List<BatchResult>();
        
        foreach (BatchParams param in input)
        {
            System.Threading.Thread.Sleep(500);
            var sourceServer = param.GetSourceServerParams();
            
            var destinationServer = param.GetDestinationServerParams();
            
            int incremental = 0;
            string configPath = config.ConfigPath;

            if (config.UseConfigServer)
            {
                try
                {
                    if (!configPath.EndsWith("/"))
                    {
                        configPath += "/";
                    }
                    
                    await CreateDir(
                        new CreateDirParams(){
                            Path = configPath,
                            Recursive = true
                        },
                        configServer
                    );
                }
                catch (Exception e)
                {
                        throw new Exception($"Could not create config directory {configPath}: ", e);
                }
                

                var isFile = await ListFiles(
                    new ListParams(){
                        Path = config.ConfigPath,
                        Filter = FilterTypes.Exact,
                        Pattern = $"{param.ObjectGuid}.json"
                    },
                    configServer
                );

                if (isFile.Count != 0)
                {
                    var inc = await ReadFile(
                        new ReadParams(){
                            Path = config.ConfigPath,
                            File = $"{param.ObjectGuid}.json",
                            Encoding = FileEncodings.UTF_8
                        },
                        configServer
                    );
                    incremental = Int32.Parse(
                        inc.Content
                    );
                }
            }

            if (config.BackupFiles)
            {
                if (backupServer != null)
                {
                    backupPath = config.BackupPath; 
                    if (!config.BackupPath.EndsWith('/'))
                    {
                        backupPath = backupPath + '/';
                    }

                    backupPath += param.ObjectGuid;

                    await CreateDir(
                        new CreateDirParams(){
                            Path = backupPath,
                            Recursive = true
                        },
                        backupServer
                    );
                }

                if (config.BackupToSubfolder)
                {
                    backupPathSubfolder = Helpers.JoinPath("/", param.SourcePath, config.SubfolderName);

                    await CreateDir(
                        new CreateDirParams(){
                            Path = backupPathSubfolder,
                            Recursive = true
                        },
                        sourceServer
                    );
                }
            }

            var fileList = await ListFiles(
                new ListParams(){
                    Path = param.SourcePath,
                    Filter = param.SourceFilterType,
                    Pattern = param.SourceFilterPattern
                },
                sourceServer
            );

            foreach (var file in fileList.Files)
            {
                string errorStr = "";
                string fileContents = "";
                byte[] fileContentsByte = null;

                try
                {
                    var inc = await ReadFile(
                        new ReadParams(){
                            Path = param.SourcePath,
                            File = file,
                            Encoding = param.SourceEncoding
                        },
                        sourceServer
                    );
                    fileContents = inc.Content;
                    fileContentsByte = inc.ByteContent;
                    
                }
                catch (Exception e)
                {
                    errorStr = $"Error reading file {sourceServer.Address}:{param.SourcePath}/{file}: {e.Message}";
                }

                string newFilename = "";
                
                if (errorStr == "")
                {
                    try
                    {
                        // Replace placeholders for new filename
                        newFilename = Helpers.FilenameSubstitutions(
                            input: param.DestinationFilename,
                            sourceFilename: file,
                            objectGuid: param.ObjectGuid,
                            incremental: incremental
                        );
                    }
                    catch (Exception e)
                    {
                        errorStr = $"Error substituting parameters {sourceServer.Address}:{param.SourcePath}/{file}: {e.Message}";
                    }
                }

                if (errorStr == "")
                {
                    try
                    {
                        // Write file to destination
                        await WriteFile(
                            new WriteParams(){
                                Path = param.DestinationPath,
                                File = newFilename,
                                Overwrite = param.Overwrite,
                                Content = fileContents,
                                ByteContent = fileContentsByte,
                                Encoding = param.DestinationEncoding
                            },
                            destinationServer
                        );
                    }
                    catch (Exception e)
                    {
                        errorStr = $"Error writing file {destinationServer.Address}:{param.DestinationPath}/{newFilename}: {e.Message}";
                    }
                }

                if (errorStr == "")
                {
                    // If backup to special dir is enabled, also write file to backup server/folder
                    if (config.BackupFiles)
                    { 
                        string backupFilename = Helpers.FilenameSubstitutions(
                            input: config.BackupFilename,
                            file,
                            param.ObjectGuid,
                            incremental
                        );

                        if (backupServer != null)
                        {
                            try
                            {
                                await WriteFile(
                                    new WriteParams(){
                                        Path = backupPath,
                                        File = backupFilename,
                                        Overwrite = param.Overwrite,
                                        Content = fileContents,
                                        ByteContent = fileContentsByte,
                                        Encoding = param.DestinationEncoding
                                    },
                                    backupServer
                                );
                            }
                            catch (Exception e)
                            {
                                errorStr = $"Error writing backup file to {backupServer.Address}:{backupPath}/{backupFilename}: {e.Message}";
                            }
                        }

                        
                        else if (config.BackupToSubfolder && backupPathSubfolder != "")
                        {
                            try
                            {
                                await WriteFile(
                                    new WriteParams(){
                                        Path = backupPathSubfolder,
                                        File = backupFilename,
                                        Overwrite = param.Overwrite,
                                        Content = fileContents,
                                        ByteContent = fileContentsByte,
                                        Encoding = param.DestinationEncoding
                                    },
                                    sourceServer
                                );
                            }
                            catch (Exception e)
                            {
                                errorStr = $"Error writing backup file to {sourceServer.Address}:{Helpers.JoinPath("/", param.SourcePath, backupPathSubfolder)}/{backupFilename}: {e.Message}";
                            }
                        }
                    }
                    
                    // Delete source file if configured
                    if (param.DeleteSource)
                    {
                        try
                        {
                            await DeleteFile(
                                new DeleteParams(){
                                    Path = param.SourcePath,
                                    File = file
                                },
                                sourceServer
                            );
                        }
                        catch (Exception e)
                        {
                            errorStr = $"Error deleting source file {sourceServer.Address}:{param.SourcePath}/{file}: {e.Message}";
                        }
                    }
                }
                
                results.Add(new BatchResult(
                    objectGuid: param.ObjectGuid,
                    sourceFile: Helpers.JoinPath("/", $"{sourceServer.Username}{sourceServer.Address}", param.SourcePath, file),
                    destinationFile: Helpers.JoinPath("/", $"{destinationServer.Username}{destinationServer.Address}", param.DestinationPath, newFilename),
                    success: errorStr == "",
                    message: errorStr,
                    timestamp: DateTime.Now
                ));
            }

            incremental++;
            
            if (config.UseConfigServer)
            {

                try
                {
                    await WriteFile(
                        new WriteParams(){
                            Path = config.ConfigPath,
                            File = $"{param.ObjectGuid}.json",
                            Overwrite = true,
                            Content = incremental.ToString(),
                            ByteContent = new byte[]{},
                            Encoding = FileEncodings.UTF_8
                        },
                        configServer
                    );
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not write config file {config.ConfigPath}/{param.ObjectGuid}.json: ", e);
                }

            }
        }

        return new BatchResults(
            count: results.Count,
            results: results
        );
    }
    
    
}
