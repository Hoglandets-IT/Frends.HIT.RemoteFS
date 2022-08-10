using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Renci.SshNet;

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
    public static ListResult ListFiles([PropertyTab] ListParams input, [PropertyTab] ServerParams connection)
    {
        var serverConfiguration = connection.GetServerConfiguration();
        var listMethod = serverConfiguration.GetActionClass("ListFiles");
        
        if (listMethod == null)
        {
            throw new Exception("No ListFiles action found in server configuration");
        }
        
        var files = (List<string>)listMethod.Invoke(null, new object[] { input, serverConfiguration });
        
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
        [PropertyTab] WriteParams destinationInput,
        [PropertyTab] ServerParams destinationConnection
    )
    {
        var file = ReadFile(sourceInput, sourceConnection);
        WriteFile(destinationInput, destinationConnection);

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

        foreach (BatchParams param in input)
        {
            int incremental = 0;
            if (config.UseConfigServer)
            {
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
                string backupPath = config.BackupPath; 
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
            
            
            
            
        }
        
    
        // Loop through BatchParams[]
        // Check if Use backup is enabled, if so, get or create backup folder for integration
        // Get file contents from source
        // Write file contents to destination
        // If backup is enabled, write file to backup server
        // If incremental, increase number in config and save config file

        return new BatchResults(
            count: 1,
            new List<BatchResult>(
                new BatchResult[] {
                    new BatchResult(
                        "true",
                        "File transferred successfully",
                        "File transferred successfully",
                        new DateTime()
                    )
                }
            )    
            );
    }
    
    
}