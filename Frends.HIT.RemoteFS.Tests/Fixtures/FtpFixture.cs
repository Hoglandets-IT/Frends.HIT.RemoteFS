using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Frends.HIT.RemoteFS.Tests.Fixtures;

public sealed class FtpFixture : RemoteFsFixture
{
    private const int ControlPort = 21;
    private const int PassivePortCount = 10;

    protected override IContainer Container { get; }

    public FtpFixture()
    {
        // The server announces its passive ports to the client, so they have to be
        // the same on the host as in the container.
        int minPort = FindFreePortRange(PassivePortCount);
        int maxPort = minPort + PassivePortCount - 1;

        var builder = new ContainerBuilder()
            .WithImage("delfer/alpine-ftp-server:latest")
            .WithEnvironment("USERS", $"{Username}|{Password}")
            .WithEnvironment("ADDRESS", "127.0.0.1")
            .WithEnvironment("MIN_PORT", minPort.ToString())
            .WithEnvironment("MAX_PORT", maxPort.ToString())
            .WithPortBinding(ControlPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ControlPort));

        for (int port = minPort; port <= maxPort; port++)
        {
            builder = builder.WithPortBinding(port, port);
        }

        Container = builder.Build();
    }

    protected override Task ConfigureAsync()
    {
        Server = new ServerParams
        {
            ConfigurationSource = ConfigurationType.FTP,
            Address = $"127.0.0.1:{Container.GetMappedPublicPort(ControlPort)}",
            Username = Username,
            Password = Password,
        };
        // Users aren't chrooted, and can only write to their home directory
        BasePath = $"/ftp/{Username}";

        return Task.CompletedTask;
    }

    private static int FindFreePortRange(int count)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            // Stay below the ephemeral ports, which Docker hands out to the other containers
            int start = Random.Shared.Next(20000, 32000 - count);
            var listeners = new List<TcpListener>();
            try
            {
                for (int port = start; port < start + count; port++)
                {
                    var listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    listeners.Add(listener);
                }
                return start;
            }
            catch (SocketException)
            {
                // Something in the range is taken, try another one
            }
            finally
            {
                listeners.ForEach(l => l.Stop());
            }
        }

        throw new InvalidOperationException($"Could not find {count} consecutive free ports for FTP passive mode");
    }
}
