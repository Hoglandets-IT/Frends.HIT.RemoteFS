using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Frends.HIT.RemoteFS.Tests.Fixtures;

public sealed class SmbFixture : RemoteFsFixture
{
    private const int SmbPort = 445;
    private const string Share = "Data";

    protected override IContainer Container { get; } = new ContainerBuilder()
        .WithImage("dockurr/samba:latest")
        .WithEnvironment("NAME", Share)
        .WithEnvironment("USER", Username)
        .WithEnvironment("PASS", Password)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(SmbPort))
        .Build();

    protected override async Task ConfigureAsync()
    {
        // The SMB client can only connect to port 445, which is rarely free to bind on the
        // host, so the container is addressed directly. This needs a routable container
        // network, which Docker Desktop doesn't have.
        string address = Container.IpAddress;
        Assert.SkipUnless(
            await IsReachable(address, SmbPort),
            $"The SMB container at {address}:{SmbPort} is not reachable from the host"
        );

        Server = new ServerParams
        {
            ConfigurationSource = ConfigurationType.SMB,
            Address = $"{address}/{Share}",
            Username = Username,
            Password = Password,
        };
        BasePath = "/";
    }

    private static async Task<bool> IsReachable(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(host, port, timeout.Token);
            return true;
        }
        catch (Exception e) when (e is SocketException or OperationCanceledException)
        {
            return false;
        }
    }
}
