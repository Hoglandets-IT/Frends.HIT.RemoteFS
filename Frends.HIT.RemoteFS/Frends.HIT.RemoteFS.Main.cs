using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Renci.SshNet;
using System.Threading.Tasks;

namespace Frends.HIT.RemoteFS;

// ListFiles
// ReadFile
// WriteFile
// MoveFile
// BulkMoveFile


/// <summary>
/// 
/// </summary>
[DisplayName("RemoteFS")]
public class Main
{
    [DisplayName("List Files")]
    public static async Task<ListResult> ListFiles([PropertyTab] ListParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var listMethod = serverConfiguration.GetActionClass("ListFiles");
        
        if (listMethod == null)
        {
            throw new Exception("No ListFiles action found in server configuration");
        }
        
        // var files = (List<string>)listMethod.Invoke(null, new object[] { input, serverConfiguration });
        List<string> files = await SMB.ListFiles(input, serverConfiguration);
        
        files = Helpers.GetMatchingFiles(files, input.Pattern, input.Filter);

        return new ListResult(
            files.Count(),
            files
        );
    }

    [DisplayName("Read File")]
    public static ReadResult ReadFile([PropertyTab] ReadParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var readMethod = serverConfiguration.GetActionClass("ReadFile");
        
        if (readMethod == null)
        {
            throw new Exception("No ReadFile action found in current class");
        }
        
        var result = (string)readMethod.Invoke(null, new object[] { input, serverConfiguration });
        
        return new ReadResult(
            result,
            string.Join("/", new string[] { input.Path, input.File }),
            input.Encoding
        );
    }
    

    [DisplayName("Write File")]
    public static WriteResult WriteFile([PropertyTab] WriteParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var writeMethod = serverConfiguration.GetActionClass("WriteFile");
        
        if (writeMethod == null)
        {
            throw new Exception("No ListFiles action found in current class");
        }
        
        writeMethod.Invoke(null, new object[] { input, serverConfiguration });
        
        return new WriteResult(true, string.Join("/", input.Path, input.File), input.Encoding);
    }

    [DisplayName("Create Directory")]
    public static CreateDirResult CreateDir([PropertyTab] CreateDirParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var createDirMethod = serverConfiguration.GetActionClass("CreateDir");
        
        if (createDirMethod == null)
        {
            throw new Exception("No CreateDir action found in current class");
        }
        
        createDirMethod.Invoke(null, new object[] { input, serverConfiguration });
        
        return new CreateDirResult(true);
    }

    [DisplayName("Delete File")]
    public static DeleteResult DeleteFile([PropertyTab] DeleteParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var deleteMethod = serverConfiguration.GetActionClass("DeleteFile");
        
        if (deleteMethod == null)
        {
            throw new Exception("No ListFiles action found in current class");
        }
        
        deleteMethod.Invoke(null, new object[] { input, serverConfiguration });
        
        return new DeleteResult(true, string.Join("/", input.Path, input.File));
    }

    [DisplayName("Copy File")]
    public static CopyResult CopyFile(
        [PropertyTab] ReadParams sourceInput,
        [PropertyTab] ServerParams sourceConnection,
        [PropertyTab] CopyDestParams destinationInput,
        [PropertyTab] ServerParams destinationConnection
    )
    {
        var file = ReadFile(sourceInput, sourceConnection);
        WriteFile(destinationInput.GetWriteParams(file.Content), destinationConnection);

        return new CopyResult(true);
    }

    [DisplayName("Batch Transfer")]
    public static BatchResults BatchTransfer(
        [PropertyTab] BatchConfigParams config,
        [PropertyTab] BatchParams[] input
    )
    {
        var configServer = config.GetConfigurationServerParams();
        var backupServer = config.GetBackupServerParams();
        string backupPath = "";
        List<BatchResult> results = new List<BatchResult>();
        
        foreach (BatchParams param in input)
        {
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
                    
                    CreateDir(
                        new CreateDirParams().Create(
                            path: configPath,
                            recursive: true
                        ),
                        configServer
                    );
                }
                catch (Exception e)
                {
                        throw new Exception($"Could not create config directory {configPath}: ", e);
                }
                
                
                var isFile = ListFiles(
                    new ListParams().Create(
                      path: config.ConfigPath,
                      filter: FilterTypes.Exact,
                      pattern: $"{param.ObjectGuid}.json"
                    ),
                    configServer
                );

                if (isFile.Count != 0)
                {
                    incremental = Int32.Parse(
                        ReadFile(
                            new ReadParams().Create(
                                path: config.ConfigPath,
                                file: $"{param.ObjectGuid}.json",
                                FileEncodings.UTF_8
                            ),
                            configServer
                        ).Content);
                }
            }

            if (config.BackupFiles)
            {
                backupPath = config.BackupPath; 
                if (!config.BackupPath.EndsWith('/'))
                {
                    backupPath = backupPath + '/';
                }

                backupPath += param.ObjectGuid;

                CreateDir(
                    new CreateDirParams().Create(
                        path: backupPath,
                        recursive: true
                    ),
                    backupServer
                );
            }

            var fileList = ListFiles(
                new ListParams().Create(
                    path: param.SourcePath,
                    filter: param.SourceFilterType,
                    pattern: param.SourceFilterPattern
                ),
                sourceServer
            );

            foreach (var file in fileList.Files)
            {
                string errorStr = "";
                string fileContents = "";

                try
                {
                    fileContents = ReadFile(
                        new ReadParams().Create(
                            path: param.SourcePath,
                            file: file,
                            encoding: param.SourceEncoding
                        ),
                        sourceServer
                    ).Content;
                    
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
                        WriteFile(
                            new WriteParams().Create(
                                path: param.DestinationPath,
                                file: newFilename,
                                overwrite: param.Overwrite,
                                content: fileContents,
                                encoding: param.DestinationEncoding
                            ),
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
                    // If backup is enabled, also write file to backup server/folder
                    if (config.BackupFiles)
                    {
                        string backupFilename = Helpers.FilenameSubstitutions(
                            input: config.BackupFilename,
                            file,
                            param.ObjectGuid,
                            incremental
                        );

                        try
                        {
                            WriteFile(
                                new WriteParams().Create(
                                    path: backupPath,
                                    file: backupFilename,
                                    overwrite: param.Overwrite,
                                    content: fileContents,
                                    encoding: param.DestinationEncoding
                                ),
                                backupServer
                            );
                        }
                        catch (Exception e)
                        {
                            errorStr = $"Error writing backup file to {backupServer.Address}:{backupPath}/{backupFilename}: {e.Message}";
                        }
                        
                        
                    }
                    
                    // Delete source file if configured
                    if (param.DeleteSource)
                    {
                        try
                        {
                            DeleteFile(
                                new DeleteParams().Create(
                                    path: param.SourcePath,
                                    file: file
                                ),
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
                    WriteFile(
                        new WriteParams().Create(
                            path: config.ConfigPath,
                            file: $"{param.ObjectGuid}.json",
                            overwrite: true,
                            content: incremental.ToString(),
                            encoding: FileEncodings.UTF_8
                        ),
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
