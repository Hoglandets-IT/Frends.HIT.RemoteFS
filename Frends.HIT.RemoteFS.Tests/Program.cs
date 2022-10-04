using Frends.HIT.RemoteFS;

// using EzSmb;
// using EzSmb.Paths;

string testServer = "";
string testDomain = "";
string testUser = "";
string testPassword = "";
string testShare = "";



var par = new ListParams();
par.Path = testShare;
par.Filter = FilterTypes.None;
par.Pattern = "";

var conn = new ServerConfiguration(
    connectiontype: ConnectionTypes.SMB, 
    address: testServer,
    domain: testDomain,
    username: testUser,
    password: testPassword,
    privatekey: "",
    privatekeypassword: "",
    fingerprint: ""
);

var files = await SMB.ListFiles(par, conn);
foreach (var fle in files) {
    Console.WriteLine(fle);
}