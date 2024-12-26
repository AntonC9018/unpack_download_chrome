public static class TestData
{
    public const string TestArchiveNameWithoutExtension = "ghoul";
    public const string DataDirectoryName = "data";

    public static (string Name, string NameWithoutExtension, string RelativePath)
        GetTestArchiveInfo(ArchiveType type)
    {
        var name = GetTestArchiveName(type);
        var nameWithoutExtension = TestArchiveNameWithoutExtension;
        var path = PathRelativeToData(name);
        return (
            Name: name,
            NameWithoutExtension: nameWithoutExtension,
            RelativePath: path);
    }

    public static string GetTestArchivePath(ArchiveType type)
    {
        var name = GetTestArchiveName(type);
        return PathRelativeToData(name);
    }

    private static string PathRelativeToData(string s)
    {
        return DataDirectoryName + "/" + s;
    }

    public static string GetTestArchiveName(ArchiveType type)
    {
        return TestArchiveNameWithoutExtension + type switch
        {
            ArchiveType._7z => ".7z",
            ArchiveType.Rar => ".rar",
            ArchiveType.Zip => ".zip",
            _ => throw new NotImplementedException("An archive for this type does not exist in the data set"),
        };
    }

    public static void VerifyUnpackedContents(string outputDirectory)
    {
        Assert.True(Directory.Exists(outputDirectory));
        var filePath = Path.Combine(outputDirectory, "ghoul.jpeg");
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