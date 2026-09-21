using Amazon.S3;
using DotNet.Testcontainers.Containers;
using Testcontainers.Minio;

namespace Frends.HIT.RemoteFS.Tests.Fixtures;

public sealed class S3Fixture : RemoteFsFixture
{
    private const string Bucket = "remotefs-tests";
    private const string Region = "us-east-1";

    // MinIO no longer publishes images to Docker Hub
    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("quay.io/minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithUsername(Username)
        .WithPassword(Password)
        .Build();

    protected override IContainer Container => _minio;

    public override bool SupportsDirectories => false;

    protected override async Task ConfigureAsync()
    {
        string address = $"http://127.0.0.1:{_minio.GetMappedPublicPort(MinioBuilder.MinioPort)}";

        using var client = new AmazonS3Client(Username, Password, new AmazonS3Config
        {
            ServiceURL = address,
            AuthenticationRegion = Region,
            ForcePathStyle = true,
        });
        await client.PutBucketAsync(Bucket);

        Server = new ServerParams
        {
            ConfigurationSource = ConfigurationType.S3,
            Address = address,
            SecretId = Username,
            SecretKey = Password,
            S3Region = Region,
            S3Bucket = Bucket,
        };
        BasePath = "/";
    }
}
