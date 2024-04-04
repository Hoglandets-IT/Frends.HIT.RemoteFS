using Frends.HIT.RemoteFS;
using Frends.HIT.RemoteFS.Tests;
using Renci.SshNet.Security;
using SMBLibrary.SMB2;

var serverParams = new ServerParams(){
    ConfigurationSource = ConfigurationType.PulsenCombine,
    Address = "integrations.pulsencombine-a.se",
    Certificate = "",
    PrivateKey = "",
};

var allf = new ListParams(){
    Path = "financialfiles/generalledger",
    Filter = FilterTypes.None,
    Pattern = ""
};

var filelist = await Main.ListFiles(allf, serverParams);
Console.WriteLine(String.Join(", ", filelist.Files));

var filezero = filelist.Files[0];

var rpar = new ReadParams(){
    Path = "financialfiles/generalledger",
    File = filezero
};

var filecontent = await Main.ReadFile(rpar, serverParams);
Console.WriteLine(System.Text.Encoding.UTF8.GetString(filecontent.ByteContent));



// var SMB = new SMBTest();
// await SMB.TestConvertToConnection();
// await SMB.TestList();
// // await SMB.TestRead();
// // await SMB.TestWrite();
// // await SMB.TestDelete();


// Console.WriteLine("Tests passed!");
