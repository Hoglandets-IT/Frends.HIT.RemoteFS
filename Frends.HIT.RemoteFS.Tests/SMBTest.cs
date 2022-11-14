namespace Frends.HIT.RemoteFS.Tests;
using System.Diagnostics;
using Frends.HIT.RemoteFS;

class SMBTest
{
    ServerParams SMBParams = new ServerParams(){
        ConfigurationSource = ConfigurationType.SMB,
        Address = "127.0.0.1",
        Username = "hello-world",
        Password = "h3110w0r1d"
    };
  
    public async Task<bool> TestConvertToConnection()
    {
        ServerConfiguration test = SMBParams.GetServerConfiguration();
        Debug.Assert(test.ConnectionType == ConnectionTypes.SMB);
        Debug.Assert(test.Address == SMBParams.Address);
        Debug.Assert(test.Domain == SMBParams.Domain);
        Debug.Assert(test.Username == SMBParams.Username);
        Debug.Assert(test.Password == SMBParams.Password);
        return true;
    }

    public async Task<bool> TestList()
    {
        // Match myfile.txt and myotherfile.txt
        var lsall = new ListParams(){
            Path = "INTEGRATION",
            Filter = FilterTypes.None,
            Pattern = ""
        };
        ListResult resall = await Main.ListFiles(lsall, SMBParams);
        List<string> allresult = new List<string>{"myfile.txt", "myotherfile.txt"};

        // Match myfile.txt
        var lsexact = new ListParams(){
            Path = "INTEGRATION",
            Filter = FilterTypes.Exact,
            Pattern = "myfile.txt"
        };
        ListResult resexact = await Main.ListFiles(lsexact, SMBParams);
        List<string> exactresult = new List<string>{"myfile.txt"};

        // Match myfile.txt
        var lscontains = new ListParams(){
            Path = "INTEGRATION",
            Filter = FilterTypes.Contains,
            Pattern = "yfil"
        };
        ListResult rescontains = await Main.ListFiles(lscontains, SMBParams);
        List<string> containsresult = new List<string>{"myfile.txt"};
        
        // Match myfile.txt and myotherfile.txt
        var lswildcard = new ListParams(){
            Path = "INTEGRATION",
            Filter = FilterTypes.Wildcard,
            Pattern = "*.txt"
        };
        ListResult reswildcard = await Main.ListFiles(lswildcard, SMBParams);
        List<string> wildcardresult = new List<string>{"myfile.txt", "myotherfile.txt"};

        // Match myfile.txt and myotherfile.txt
        var lsregex = new ListParams(){
            Path = "INTEGRATION",
            Filter = FilterTypes.Regex,
            Pattern = "(my)+.*\\.txt"
        };
        ListResult resregex = await Main.ListFiles(lsregex, SMBParams);
        List<string> regexresult = new List<string>{"myfile.txt", "myotherfile.txt"};
        
        Debug.Assert(resall.Files.Count == 2);
        Debug.Assert(resexact.Files.Count == 1);
        Debug.Assert(rescontains.Files.Count == 1);
        Debug.Assert(reswildcard.Files.Count == 2);
        Debug.Assert(resregex.Files.Count == 2);

        Debug.Assert(resall.Files.SequenceEqual(allresult));
        Debug.Assert(resexact.Files.SequenceEqual(exactresult));
        Debug.Assert(rescontains.Files.SequenceEqual(containsresult));
        Debug.Assert(reswildcard.Files.SequenceEqual(wildcardresult));
        Debug.Assert(resregex.Files.SequenceEqual(regexresult));

        return true;
    }

    public async Task<bool> TestRead()
    {
        var read = new ReadParams(){
            Path = "INTEGRATION/myfile.txt"
        };
        ReadResult res = await Main.ReadFile(read, SMBParams);
        Debug.Assert(res.Content == "Hello World!");

        var read_byt = new ReadParams(){
            Path = "INTEGRATION",
            File = "myfile.txt",
            Encoding = FileEncodings.RAW
        };
        ReadResult res_byt = await Main.ReadFile(read_byt, SMBParams);
        Debug.Assert(res_byt.ByteContent.ToString() == "Hello World!");
        return true;
    }
}