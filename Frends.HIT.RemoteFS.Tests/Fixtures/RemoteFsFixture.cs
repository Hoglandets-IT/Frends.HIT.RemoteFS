using DotNet.Testcontainers.Containers;

namespace Frends.HIT.RemoteFS.Tests.Fixtures;

/// <summary>
/// Starts a disposable server in a container and exposes the connection details for it.
/// One instance is shared by all tests in a test class.
/// </summary>
public abstract class RemoteFsFixture : IAsyncLifetime
{
    public const string Username = "testuser";
    public const string Password = "testpassword";

    /// <summary>Connection parameters for the running container</summary>
    public ServerParams Server { get; protected set; } = new();

    /// <summary>A writable path on the server under which the tests create their files</summary>
    public string BasePath { get; protected set; } = "/";

    /// <summary>Whether directories can be created. On S3 they only exist implicitly, as part of the file names</summary>
    public virtual bool SupportsDirectories => true;

    protected abstract IContainer Container { get; }

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();
        await ConfigureAsync();
    }

    /// <summary>Called once the container is running, to fill in Server and BasePath</summary>
    protected abstract Task ConfigureAsync();

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
