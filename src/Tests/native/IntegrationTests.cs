using System.Text;

public class IntegrationTests
{
    [_7zFact]
    public void _7zFullTest()
    {
        DoFullTest(ArchiveType._7z);
    }

    [RarFact]
    public void RarFullTest()
    {
        DoFullTest(ArchiveType.Rar);
    }

    [Fact]
    public void ZipFullTest()
    {
        DoFullTest(ArchiveType.Zip);
    }

    private void DoFullTest(ArchiveType type)
    {
        using var tempDir = TempDirectory.Create();

        var testArchive = TestData.GetTestArchiveInfo(type);
        var archiveInTempDir = Path.Join(tempDir.FullPath, testArchive.Name);
        File.Copy(testArchive.RelativePath, archiveInTempDir);

        using var input = new MemoryStream();
        WriteHelper.WriteObject(input, new Message
        {
            FilePath = archiveInTempDir,
        });
        input.Position = 0;

        using var context = TestContext.Create(input);

        context.Thread.Join();
        // TODO: The output is disposed here, yikes.

        var outputDirectory = Path.Join(tempDir.FullPath, testArchive.NameWithoutExtension);
        TestData.VerifyUnpackedContents(outputDirectory);

        Assert.False(File.Exists(archiveInTempDir), "Initial file deleted properly");
    }
}

public sealed class TestContext : IDisposable
{
    public MemoryStream Output { get; }
    private Application Application { get; }
    public Thread Thread { get; }

    public TestContext(
        MemoryStream output,
        Application application,
        Thread thread)
    {
        Output = output;
        Application = application;
        Thread = thread;
    }

    public void Dispose()
    {
        Application.Dispose();
    }

    public static TestContext Create(MemoryStream input)
    {
        var output = new MemoryStream();
        var log = Console.OpenStandardError();
        var log1 = new StreamWriter(log, Encoding.UTF8, leaveOpen: false);
        var app = new Application(
            @in: input,
            @out: output,
            log: log1,
            context: ArchiveExecutablesContext.Create(),
            actionAfterUnpack: null);
        Thread? thread = null;
        try
        {
            thread = new Thread(() =>
            {
                // ReSharper disable once AccessToDisposedClosure
                app.ReadAndProcessMessage();
            });
            thread.Start();
        }
        catch
        {
            if (thread == null)
            {
                app.Dispose();
            }
            throw;
        }
        return new(
            output: output,
            application: app,
            thread: thread);
    }
}
