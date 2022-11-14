using Frends.HIT.RemoteFS.Tests;

var SMB = new SMBTest();
await SMB.TestConvertToConnection();
await SMB.TestList();
// await SMB.TestRead();
// await SMB.TestWrite();
// await SMB.TestDelete();


Console.WriteLine("Tests passed!");
