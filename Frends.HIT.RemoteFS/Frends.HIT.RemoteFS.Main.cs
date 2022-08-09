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
        List<string> files = new List<string>();
        var listMethod = Helpers.GetSubclassMethod($"Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}", "ListFiles");
        if (listMethod == null)
        {
            throw new Exception($"Could not find method Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}.ListFiles");
        }
        files = (List<string>)listMethod.Invoke(null, new object[] { input, connection.GetServerConfiguration() });
        files = Helpers.GetMatchingFiles(files, input.Pattern, input.Filter);

        return new ListResult(
            files.Count(),
            files
        );
    }

    [DisplayName("Read File")]
    public static ReadResult ReadFile([PropertyTab] ReadParams input, [PropertyTab] ServerParams connection)
    {
        var readMethod = Helpers.GetSubclassMethod($"Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}", "ReadFile");
        if (readMethod == null)
        {
            throw new Exception($"Could not find method Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}.ReadFile");
        }
        
        var result = (string)readMethod.Invoke(null, new object[] { input, connection.GetServerConfiguration() });
        
        return new ReadResult(
            result,
            string.Join("/", new string[] { input.Path, input.File }),
            input.Encoding
        );
    }
    

    [DisplayName("Write File")]
    public static WriteResult WriteFile([PropertyTab] WriteParams input, [PropertyTab] ServerParams connection)
    {
        var writeMethod = Helpers.GetSubclassMethod($"Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}", "WriteFile");
        if (writeMethod == null)
        {
            throw new Exception($"Could not find method Frends.HIT.RemoteFS.{connection.ConfigurationSource.ToString()}.WriteFile");
        }
        
        writeMethod.Invoke(null, new object[] { input, connection.GetServerConfiguration() });
        
        return new WriteResult(true, string.Join("/", input.Path, input.File), input.Encoding);
    }

    
    // [DisplayName("Delete File")]
    // public static DeleteResult DeleteFile([PropertyTab] DeleteParams input, [PropertyTab] ServerParams connection)
    // {
    //     return new DeleteResult();
    // }
    //
    // [DisplayName("Copy Files")]
    // public static CopyResult CopyFile([PropertyTab] CopyParams input, [PropertyTab] ServerParams connection)
    // {
    //     return new CopyResult();
    // }
    //
    // [DisplayName("Move Files")]
    // public static MoveResult MoveFile([PropertyTab] MoveParams input, [PropertyTab] ServerParams connection)
    // {
    //     return new MoveResult();
    // }
}