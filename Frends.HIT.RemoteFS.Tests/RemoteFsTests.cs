using System.Text;
using Frends.HIT.RemoteFS.Tests.Fixtures;

namespace Frends.HIT.RemoteFS.Tests;

/// <summary>
/// The tests every connection type has to pass. Each test works in a directory of
/// its own, so the tests don't depend on each other or on the order they run in.
/// </summary>
public abstract class RemoteFsTests<TFixture> where TFixture : RemoteFsFixture
{
    private const string Content = "Hello, World! Hallå, världen!";

    private readonly TFixture _fixture;

    protected RemoteFsTests(TFixture fixture)
    {
        _fixture = fixture;
    }

    private ServerParams Server => _fixture.Server;

    [Fact]
    public async Task WriteFile_ThenReadFile_ReturnsContent()
    {
        string dir = await NewDir();

        var written = await Main.WriteFile(Write(dir, "file.txt", Content), Server);
        var read = await Main.ReadFile(Read(dir, "file.txt"), Server);

        Assert.True(written.Success);
        Assert.Equal(Content, read.Content);
        Assert.Equal(Encoding.UTF8.GetBytes(Content), read.ByteContent);
    }

    [Theory]
    [InlineData(FileEncodings.UTF_8)]
    [InlineData(FileEncodings.UTF_32)]
    [InlineData(FileEncodings.ISO_8859_1)]
    public async Task WriteFile_ThenReadFile_RoundTripsEncoding(FileEncodings encoding)
    {
        string dir = await NewDir();

        await Main.WriteFile(Write(dir, "file.txt", Content, encoding), Server);
        var read = await Main.ReadFile(Read(dir, "file.txt", encoding), Server);

        Assert.Equal(Content, read.Content);
        Assert.Equal(Helpers.EncodingFromEnum(encoding).GetBytes(Content), read.ByteContent);
    }

    [Fact]
    public async Task WriteFile_Raw_RoundTripsBytes()
    {
        string dir = await NewDir();
        byte[] bytes = Enumerable.Range(0, 100_000).Select(i => (byte)(i % 256)).ToArray();

        await Main.WriteFile(new WriteParams
        {
            Path = dir,
            File = "file.bin",
            ByteContent = bytes,
            Encoding = FileEncodings.RAW,
        }, Server);
        var read = await Main.ReadFile(Read(dir, "file.bin", FileEncodings.RAW), Server);

        Assert.Equal(bytes, read.ByteContent);
        Assert.Equal("", read.Content);
    }

    [Fact]
    public async Task WriteFile_ExistingFileWithoutOverwrite_ThrowsAndKeepsContent()
    {
        string dir = await NewDir();
        await Main.WriteFile(Write(dir, "file.txt", Content), Server);

        await Assert.ThrowsAnyAsync<Exception>(
            () => Main.WriteFile(Write(dir, "file.txt", "Replaced"), Server)
        );

        var read = await Main.ReadFile(Read(dir, "file.txt"), Server);
        Assert.Equal(Content, read.Content);
    }

    [Fact]
    public async Task WriteFile_ExistingFileWithOverwrite_ReplacesContent()
    {
        string dir = await NewDir();
        await Main.WriteFile(Write(dir, "file.txt", Content + Content), Server);

        await Main.WriteFile(Write(dir, "file.txt", "Replaced", overwrite: true), Server);

        var read = await Main.ReadFile(Read(dir, "file.txt"), Server);
        Assert.Equal("Replaced", read.Content);
    }

    [Fact]
    public async Task ReadFile_MissingFile_Throws()
    {
        string dir = await NewDir();

        await Assert.ThrowsAnyAsync<Exception>(
            () => Main.ReadFile(Read(dir, "missing.txt"), Server)
        );
    }

    [Fact]
    public async Task ListFiles_ReturnsFileNames()
    {
        string dir = await NewDir("one.txt", "two.txt", "three.xml");

        var result = await Main.ListFiles(new ListParams { Path = dir }, Server);

        Assert.Equal(new[] { "one.txt", "three.xml", "two.txt" }, result.Files.Order());
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task ListFiles_EmptyDirectory_ReturnsNothing()
    {
        Assert.SkipUnless(_fixture.SupportsDirectories, "No empty directories on this connection type");
        string dir = await NewDir();

        var result = await Main.ListFiles(new ListParams { Path = dir }, Server);

        Assert.Empty(result.Files);
    }

    [Theory]
    [InlineData(FilterTypes.None, "", new[] { "data.csv", "my-file.txt", "my-other-file.txt" })]
    [InlineData(FilterTypes.Exact, "my-file.txt", new[] { "my-file.txt" })]
    [InlineData(FilterTypes.Contains, "other", new[] { "my-other-file.txt" })]
    [InlineData(FilterTypes.Wildcard, "*.txt", new[] { "my-file.txt", "my-other-file.txt" })]
    [InlineData(FilterTypes.Wildcard, "*.xml", new string[0])]
    [InlineData(FilterTypes.Regex, @"^(my)+.*\.txt$", new[] { "my-file.txt", "my-other-file.txt" })]
    public async Task ListFiles_WithFilter_ReturnsMatches(FilterTypes filter, string pattern, string[] expected)
    {
        string dir = await NewDir("my-file.txt", "my-other-file.txt", "data.csv");

        var result = await Main.ListFiles(new ListParams
        {
            Path = dir,
            Filter = filter,
            Pattern = pattern,
        }, Server);

        Assert.Equal(expected, result.Files.Order());
    }

    [Fact]
    public async Task ListFiles_ByType_SeparatesFilesAndDirectories()
    {
        string dir = await NewDir("file.txt");
        await CreateDir(Join(dir, "subdir"));
        await Main.WriteFile(Write(Join(dir, "subdir"), "nested.txt", Content), Server);

        var files = await Main.ListFiles(new ListParams { Path = dir, ListType = ObjectTypes.Files }, Server);
        var dirs = await Main.ListFiles(new ListParams { Path = dir, ListType = ObjectTypes.Directories }, Server);
        var both = await Main.ListFiles(new ListParams { Path = dir, ListType = ObjectTypes.Both }, Server);

        Assert.Equal(new[] { "file.txt" }, files.Files);
        Assert.Equal(new[] { "subdir" }, dirs.Files);
        Assert.Equal(new[] { "file.txt", "subdir" }, both.Files.Order());
    }

    [Fact]
    public async Task ListFilesRecursive_FindsFilesInSubdirectories()
    {
        string dir = await NewDir("root.txt", "root.xml");
        await CreateDir(Join(dir, "a/b"), recursive: true);
        await CreateDir(Join(dir, "excluded"));
        await Main.WriteFile(Write(Join(dir, "a"), "nested.txt", Content), Server);
        await Main.WriteFile(Write(Join(dir, "a/b"), "deep.txt", Content), Server);
        await Main.WriteFile(Write(Join(dir, "excluded"), "hidden.txt", Content), Server);

        var listParams = new ListParams { Path = dir, Filter = FilterTypes.Wildcard, Pattern = "*.txt" };

        var all = await Main.ListFilesRecursive(
            listParams, new RecursiveListParams { Depth = 2, Exclude = new[] { "excluded" } }, Server
        );
        var shallow = await Main.ListFilesRecursive(
            listParams, new RecursiveListParams { Depth = 1, Exclude = new[] { "excluded" } }, Server
        );

        Assert.Equal(new[] { "a/b/deep.txt", "a/nested.txt", "root.txt" }, all.Files.Order());
        Assert.Equal(new[] { "a/nested.txt", "root.txt" }, shallow.Files.Order());
    }

    [Fact]
    public async Task CreateDir_Recursive_CreatesMissingParents()
    {
        Assert.SkipUnless(_fixture.SupportsDirectories, "No directories on this connection type");
        string dir = await NewDir();

        var result = await Main.CreateDir(new CreateDirParams { Path = Join(dir, "x/y/z"), Recursive = true }, Server);
        await Main.WriteFile(Write(Join(dir, "x/y/z"), "file.txt", Content), Server);

        Assert.True(result.Success);
        var listing = await Main.ListFiles(new ListParams { Path = Join(dir, "x/y/z") }, Server);
        Assert.Equal(new[] { "file.txt" }, listing.Files);
    }

    [Fact]
    public async Task CreateDir_WithoutDirectorySupport_Throws()
    {
        Assert.SkipWhen(_fixture.SupportsDirectories, "Directories are supported on this connection type");

        await Assert.ThrowsAnyAsync<Exception>(
            () => Main.CreateDir(new CreateDirParams { Path = Join(_fixture.BasePath, "dir") }, Server)
        );
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        string dir = await NewDir("keep.txt", "delete.txt");

        var result = await Main.DeleteFile(new DeleteParams { Path = dir, File = "delete.txt" }, Server);

        Assert.True(result.Success);
        var listing = await Main.ListFiles(new ListParams { Path = dir }, Server);
        Assert.Equal(new[] { "keep.txt" }, listing.Files);
    }

    [Fact]
    public async Task CopyFile_KeepsSource()
    {
        string dir = await NewDir("source.txt");

        var result = await Main.CopyFile(
            Read(dir, "source.txt"), Server,
            new CopyDestParams { Path = dir, File = "copy.txt", Encoding = FileEncodings.UTF_8 }, Server
        );

        Assert.True(result.Success);
        var listing = await Main.ListFiles(new ListParams { Path = dir }, Server);
        Assert.Equal(new[] { "copy.txt", "source.txt" }, listing.Files.Order());
        Assert.Equal(Content, (await Main.ReadFile(Read(dir, "copy.txt"), Server)).Content);
    }

    [Fact]
    public async Task MoveFile_RemovesSource()
    {
        string dir = await NewDir("source.txt");

        var result = await Main.MoveFile(
            Read(dir, "source.txt"), Server,
            new CopyDestParams { Path = dir, File = "moved.txt", Encoding = FileEncodings.UTF_8 }, Server
        );

        Assert.True(result.Success);
        var listing = await Main.ListFiles(new ListParams { Path = dir }, Server);
        Assert.Equal(new[] { "moved.txt" }, listing.Files);
        Assert.Equal(Content, (await Main.ReadFile(Read(dir, "moved.txt"), Server)).Content);
    }

    /// <summary>
    /// Create a uniquely named directory below the base path, containing the given files
    /// </summary>
    private async Task<string> NewDir(params string[] files)
    {
        string dir = Join(_fixture.BasePath, Guid.NewGuid().ToString("N"));

        await CreateDir(dir);

        foreach (string file in files)
        {
            await Main.WriteFile(Write(dir, file, Content), Server);
        }

        return dir;
    }

    /// <summary>
    /// Create a directory, where that's needed for files to be written to it
    /// </summary>
    private async Task CreateDir(string path, bool recursive = false)
    {
        if (_fixture.SupportsDirectories)
        {
            await Main.CreateDir(new CreateDirParams { Path = path, Recursive = recursive }, Server);
        }
    }

    private static string Join(string path, string child) => path.TrimEnd('/') + "/" + child;

    private static WriteParams Write(
        string dir, string file, string content,
        FileEncodings encoding = FileEncodings.UTF_8, bool overwrite = false
    ) => new()
    {
        Path = dir,
        File = file,
        Content = content,
        Encoding = encoding,
        Overwrite = overwrite,
    };

    private static ReadParams Read(string dir, string file, FileEncodings encoding = FileEncodings.UTF_8) => new()
    {
        Path = dir,
        File = file,
        Encoding = encoding,
    };
}

public class FtpTests(FtpFixture fixture) : RemoteFsTests<FtpFixture>(fixture), IClassFixture<FtpFixture>;

public class SftpTests(SftpFixture fixture) : RemoteFsTests<SftpFixture>(fixture), IClassFixture<SftpFixture>;

public class SmbTests(SmbFixture fixture) : RemoteFsTests<SmbFixture>(fixture), IClassFixture<SmbFixture>;

public class S3Tests(S3Fixture fixture) : RemoteFsTests<S3Fixture>(fixture), IClassFixture<S3Fixture>;
