using System.Diagnostics;
using System.Net.Http;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using Frends.HIT.RemoteFS;

namespace Frends.HIT.RemoteFS.Tests {
  
  class DirTreeItem {
    public string Name { get; set; }
    public bool IsDir { get; set; }
    public string Content { get; set; } = "";
    public List<DirTreeItem> Children { get; set; } = new List<DirTreeItem>();

    public DirTreeItem(string name, string content) {
      Name = name;
      Content = content;
      IsDir = false;
    }

    public DirTreeItem(string name, List<DirTreeItem> children) {
      Name = name;
      Children = children;
      IsDir = true;
    }

    public void Create(string basePath) {
      if (IsDir) {
        if (!Directory.Exists(Path.Join(basePath, Name))) {
            Directory.CreateDirectory(Path.Join(basePath, Name));
            Console.WriteLine("Creating dir: " + Path.Join(basePath, Name));
        }
        
        foreach (var child in Children) {
          child.Create(Path.Join(basePath, Name));
        }

      } else if (!File.Exists(Path.Join(basePath, Name))) {
        File.WriteAllText(Path.Join(basePath, Name), Content);
        Console.WriteLine("Creating file: " + Path.Join(basePath, Name));
      }
    }
  }



  class TestContainerInstances {
    private string _sourceFolder = "src";
    private string _destFolder = "dst";
    private string _baseFolder = "";

    private Dictionary<string,DotNet.Testcontainers.Containers.IContainer?> _containers = new Dictionary<string,DotNet.Testcontainers.Containers.IContainer?>();
    private DotNet.Testcontainers.Containers.IContainer? _ftpContainer;

    private DirTreeItem _dirTree { 
      get {
        return new DirTreeItem(
            "",
            new List<DirTreeItem>(){
              new ("src", new List<DirTreeItem>(){
                new ("parentDir", new List<DirTreeItem>(){
                  new ("nested-file-one.txt", "Hello World!"),
                  new ("nested-file-two.txt", "Hello World!"),
                }),
                  new ("file-one.txt", "Hello World!"),
                  new ("file-two.txt", "Hello World!"),
                }
              ),
              new ("dst", new List<DirTreeItem>(){
                new ("ftp", new List<DirTreeItem>(){}),
                new ("sftp", new List<DirTreeItem>(){}),
                new ("smb", new List<DirTreeItem>(){}),
              })
            }
        );
      }
    }

  public string GetTemporaryDirectory()
  {
      string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

      if(File.Exists(tempDirectory)) {
          return GetTemporaryDirectory();
      } else {
          Directory.CreateDirectory(tempDirectory);
          return tempDirectory;
      }
  }

    public void PrepareTestFiles() {
      // Create temp dir
      _baseFolder = GetTemporaryDirectory() + "/";

      _sourceFolder = _baseFolder + "/" + _sourceFolder;
      _destFolder = _baseFolder + "/" + _destFolder;

      _dirTree.Create(_baseFolder);
    }

    public async Task<int> StartContainer(
      string id, string image, 
      int defaultPort, Dictionary<string, string> folders, 
      List<int> symmetricPorts, Dictionary<string, string> envs, List<string> args = null) {
        var container = new DotNet.Testcontainers.Builders.ContainerBuilder()
          .WithImage(image)
          .WithPrivileged(true)
          .WithPortBinding(defaultPort, true);
        
        foreach (var p in symmetricPorts) {
            container = container.WithPortBinding(p, p);
        }

        foreach (var e in envs) {
            container = container.WithEnvironment(e.Key, e.Value);
        }

        foreach (var f in folders) {
          container = container.WithBindMount(_baseFolder + f.Key, f.Value, DotNet.Testcontainers.Configurations.AccessMode.ReadWrite);
        }

        if (args != null) {
          container = container.WithCommand(args.ToArray());
        }

        _containers[id] = container.Build();

        await _containers[id].StartAsync().ConfigureAwait(false);
        return _containers[id].GetMappedPublicPort(defaultPort);
      }

      public string GetContainerIP(string id) {
        return _containers[id].IpAddress;
        // var ip = await _containers[id]
      }

      public async Task EndContainer(string id) {
        await _containers[id].DisposeAsync();
      }

    public async Task<bool> RemoveFolders(string id, Dictionary<string,string> folders) {
      // Get current user id
      var uid = System.Environment.GetEnvironmentVariable("UID");

      foreach (var folder in folders) {
          await _containers[id].ExecAsync(new List<string>(){
            "chown", "-R", uid + ":" + uid, folder.Value
          });
      }

      return true;
    }

    public async Task<int> StartFtpServer(string username, string password, string folder) {
      return await StartContainer(
        "ftp",
        "garethflowers/ftp-server",
        21,
        new Dictionary<string, string>(){
          {Path.Join(_baseFolder, folder), Path.Join("/home", username)}
        },
        new List<int>(){
          40000, 40001, 40002, 40003, 40004, 40005, 40006, 40007, 40008, 40009, 40010
        },
        new Dictionary<string, string>(){
          {"FTP_USER", username},
          {"FTP_PASS", password}
        }
      ).ConfigureAwait(false);
    }

    public async Task<int> EndFtpServer(string username) {
      await RemoveFolders("ftp", new Dictionary<string,string>(){
        {Path.Join("/home", username), Path.Join("/home", username)}
      });

      await _containers["ftp"].StopAsync().ConfigureAwait(false);

      return 0;
    }

    // public async Task<int> StartSftpServer(string username, string password, string folder) {
    //   return await StartContainer(
    //     "sftp",
    //     "atmoz/sftp",
    //     22,

    //   );
    // }
  }


  static class Program {
    public static async Task TestFtpSimple(TestContainerInstances testCon) {
        var ftpPort = await testCon.StartFtpServer("username", "password", "src");
        Console.WriteLine("Started FTP Server on port: " + ftpPort);

        var lParams = new Frends.HIT.RemoteFS.ListParams(){
          Path = "/",
          Filter = Frends.HIT.RemoteFS.FilterTypes.None,
          Pattern = ""
        };

        var sParams = new Frends.HIT.RemoteFS.ServerParams(){
          ConfigurationSource = Frends.HIT.RemoteFS.ConfigurationType.FTP,
          Address = "localhost:" + ftpPort,
          Username = "username",
          Password = "password",
          Retries = 0,
          RetryTimeout = 5
        };

        var files = Frends.HIT.RemoteFS.Main.ListFiles(lParams, sParams).Result;
        Console.WriteLine(files.Files.Count + " Files found for ftp");

        Debug.Assert(files.Files.Count == 2);
        Debug.Assert(files.Files.All(f => new List<string>(){"file-one.txt", "file-two.txt"}.Contains(f)));

        lParams.Path = "/parentDir";
        files = Frends.HIT.RemoteFS.Main.ListFiles(lParams, sParams).Result;

        Console.WriteLine(files.Files.Count + " Files found for ftp/parentDir");
        Debug.Assert(files.Files.Count == 2);
        Debug.Assert(files.Files.All(f => new List<string>(){"nested-file-one.txt", "nested-file-two.txt"}.Contains(f)));

        await testCon.EndFtpServer("username");
        return;
    }

    

    // public static async Task TestSftpSimple(TestContainerInstances testCon) {
    //   var sftpPort = await testCon.StartSftpServer("username", "password", "src");


    // }



    public static async Task Main() {
        Console.WriteLine("Starting tests");
        var testContainerInstances = new TestContainerInstances();
        testContainerInstances.PrepareTestFiles();


      //   if (!Directory.Exists("/tmp/ftp-tests")) {
      //     Directory.CreateDirectory("/tmp/ftp-tests");
      //   }

      //   // Basic FTP Tests
      //   var cont = await testContainerInstances.StartContainer(
      //   "ftp",
      //   "garethflowers/ftp-server",
      //   21,
      //   new Dictionary<string, string>(){
      //     {"dst/ftp", "/home/basicusername"}
      //   },
      //   new List<int>(){
      //     40000, 40001, 40002, 40003,  40005, 40006, 40007, 40008, 40009, 40010
      //   },
      //   new Dictionary<string, string>(){
      //     {"FTP_USER", "basicusername"},
      //     {"FTP_PASS", "basicpassword"}
      //   }
      // ).ConfigureAwait(false);

      // var ftp = new ReadWriteTests(new ServerParams(){
      //     ConfigurationSource = ConfigurationType.FTP,
      //     Address = "localhost:" + cont.ToString(),
      //     Username = "basicusername",
      //     Password = "basicpassword",
      // });

      // await ftp.RunTests("/");

      // await testContainerInstances.EndContainer("ftp");

        // Basic FTP Tests
      // var cont = await testContainerInstances.StartContainer(
      //   "ftp",
      //   "atmoz/sftp",
      //   22,
      //   new Dictionary<string, string>(){},
      //   new List<int>(){},
      //   new Dictionary<string, string>(){},
      //   new List<string>(){
      //     "basicusername:basicpassword:::tstdir"
      //   }
      // ).ConfigureAwait(false);

      // var sftp = new ReadWriteTests(new ServerParams(){
      //     ConfigurationSource = ConfigurationType.SFTP,
      //     Address = "localhost:" + cont.ToString(),
      //     Username = "basicusername",
      //     Password = "basicpassword",
      // });

      // await sftp.RunTests("tstdir");

      // await testContainerInstances.EndContainer("ftp");

      var cont = await testContainerInstances.StartContainer(
        "samba",
        "dperson/samba",
        137,
        new (){},
        new (){
          139, 445
        },
        new (){},
        new List<string>(){
          "-p",
          "-u", "basicusername;basicpassword",
          "-s", "tempfo;/tmp;no;no;no;basicusername"
        }
      );

      var containerIp = testContainerInstances.GetContainerIP("samba");

      var smb = new ReadWriteTests(new ServerParams(){
          ConfigurationSource = ConfigurationType.SMB,
          Address = "localhost",
          Username = "basicusername",
          Password = "basicpassword",
      });

      await smb.RunTests("tempfo/");

      await testContainerInstances.EndContainer("samba");
    }
  }
}

