using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Frends.HIT.RemoteFS.Tests.Fixtures;

public sealed class SftpFixture : RemoteFsFixture
{
    private const int SshPort = 22;
    private const string UploadDir = "upload";

    protected override IContainer Container { get; } = new ContainerBuilder()
        .WithImage("atmoz/sftp:alpine")
        // user:password:uid:gid:directory - the home directory is a read-only chroot,
        // only the listed directory is writable
        .WithCommand($"{Username}:{Password}:::{UploadDir}")
        .WithPortBinding(SshPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server listening on"))
        .Build();

    protected override Task ConfigureAsync()
    {
        Server = new ServerParams
        {
            ConfigurationSource = ConfigurationType.SFTP,
            Address = $"127.0.0.1:{Container.GetMappedPublicPort(SshPort)}",
            Username = Username,
            Password = Password,
        };
        BasePath = "/" + UploadDir;

        return Task.CompletedTask;
    }
}
