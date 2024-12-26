public sealed class ArchiveTests
{
    private struct TestParams
    {
        public required Func<string> FindExecutable;
        public required Func<ArchiveUtils.ExtractWithExecutableParams, bool> Extract;
        public required string DataFileName;
    }

    private void Test(TestParams p)
    {
        var path = p.FindExecutable();
        using var tempDir = TempDirectory.Create();

        var fullPathToData = Path.GetFullPath(p.DataFileName);
        bool success = p.Extract(new()
        {
            ExecutablePath = path,
            InputFilePath = fullPathToData,
            OutputDirectoryPath = tempDir.FullPath,
        });
        Assert.True(success);

        TestData.VerifyUnpackedContents(tempDir.FullPath);
    }

    [_7zFact]
    public void _7z()
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.Find7ZExecutable!,
            Extract = ArchiveUtils.Extract7Z,
            DataFileName = TestData.GetTestArchivePath(ArchiveType._7z),
        });
    }

    [RarFact]
    public void Rar()
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.FindRarExecutable!,
            Extract = ArchiveUtils.ExtractRar,
            DataFileName = TestData.GetTestArchiveName(ArchiveType.Rar),
        });
    }
}