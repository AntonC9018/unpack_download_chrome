using System.Diagnostics;

public readonly struct ArchiveDefinition
{
    public required string FileNameWithoutExtension { get; init; }
    public required Action<string> VerifyUnpackedContents { get; init; }
}

public readonly struct TestArchiveInfo
{
    public required string Name { get; init; }
    public required string NameWithoutExtension { get; init; }
    public required string RelativePath { get; init; }
    public required Action<string> Verify { get; init; }
}

public enum ArchiveIndex
{
    Simple,
    WithSpaces,
    Russian,
    RussianWithSpace,
    _Count,
}

public static class TestData
{
    private static readonly ArchiveDefinition[] _archiveDefinitions = CreateNames();
    private static ArchiveDefinition[] CreateNames()
    {
        var values = new ArchiveDefinition[(int) ArchiveIndex._Count];
        void Set(ArchiveIndex i, ArchiveDefinition name)
        {
            values[(int) i] = name;
        }

        Set(ArchiveIndex.Simple, Simple);
        Set(ArchiveIndex.WithSpaces, WithSpaces);
        Set(ArchiveIndex.Russian, Russian);
        Set(ArchiveIndex.RussianWithSpace, RussianWithSpace);

        Debug.Assert(values.All(x => x.VerifyUnpackedContents != null));
        return values;
    }

    public static IReadOnlyList<ArchiveDefinition> ArchiveDefinitions => _archiveDefinitions;
    public static IReadOnlyList<object[]> ArchiveDefinitionsAsParams =>
        ArchiveDefinitions.Select(x => new object[] { x }).ToArray();
    public static ArchiveDefinition GetArchiveDefinition(ArchiveIndex i) => _archiveDefinitions[(int) i];

    public static ArchiveDefinition Simple => new()
    {
        VerifyUnpackedContents = GhoulVerifyUnpackedContents,
        FileNameWithoutExtension = "ghoul",
    };
    public static ArchiveDefinition WithSpaces => new()
    {
        VerifyUnpackedContents = TestTxtVerifyUnpackedContents,
        FileNameWithoutExtension = "space space",
    };
    public static ArchiveDefinition Russian => new()
    {
        VerifyUnpackedContents = TestTxtVerifyUnpackedContents,
        FileNameWithoutExtension = "привет",
    };
    public static ArchiveDefinition RussianWithSpace => new()
    {
        VerifyUnpackedContents = TestTxtVerifyUnpackedContents,
        FileNameWithoutExtension = "привет пробел",
    };

    public const string DataDirectoryName = "data";

    public static TestArchiveInfo GetTestArchiveInfo(ArchiveType type, ArchiveDefinition archive)
    {
        var nameWithoutExtension = archive.FileNameWithoutExtension;
        var name = nameWithoutExtension + GetExtension(type);
        var path = PathRelativeToData(name);
        return new TestArchiveInfo
        {
            Name = name,
            NameWithoutExtension = nameWithoutExtension,
            RelativePath = path,
            Verify = archive.VerifyUnpackedContents,
        };
    }

    private static string PathRelativeToData(string s)
    {
        return DataDirectoryName + "/" + s;
    }

    public static string GetExtension(ArchiveType type)
    {
        return type switch
        {
            ArchiveType._7z => ".7z",
            ArchiveType.Rar => ".rar",
            ArchiveType.Zip => ".zip",
            _ => throw new NotImplementedException("An archive for this type does not exist in the data set"),
        };
    }

    private static void GhoulVerifyUnpackedContents(string outputDirectory)
    {
        Assert.True(Directory.Exists(outputDirectory));
        var filePath = Path.Combine(outputDirectory, "ghoul.jpeg");
        Assert.True(File.Exists(filePath));
    }

    private static void TestTxtVerifyUnpackedContents(string outputDirectory)
    {
        Assert.True(Directory.Exists(outputDirectory));
        var filePath = Path.Combine(outputDirectory, "test.txt");
        Assert.True(File.Exists(filePath));
    }
}

public readonly struct TempDirectory : IDisposable
{
    public readonly string FullPath;

    public TempDirectory(string fullPath)
    {
        FullPath = fullPath;
    }

    public static TempDirectory Create()
    {
        var path = Directory.CreateTempSubdirectory();
        return new TempDirectory(path.FullName);
    }

    public void Dispose()
    {
        Directory.Delete(FullPath, recursive: true);
    }
}