public sealed class ArchiveTests
{
    private struct TestParams
    {
        public required Func<string> FindExecutable;
        public required Func<ArchiveUtils.ExtractWithExecutableParams, bool> Extract;
        public required TestArchiveInfo ArchiveInfo;
    }

    private void Test(TestParams p)
    {
        var path = p.FindExecutable();
        using var tempDir = TempDirectory.Create();

        var fullPathToData = Path.GetFullPath(p.ArchiveInfo.RelativePath);
        bool success = p.Extract(new()
        {
            ExecutablePath = path,
            InputFilePath = fullPathToData,
            OutputDirectoryPath = tempDir.FullPath,
        });
        Assert.True(success);

        p.ArchiveInfo.Verify(tempDir.FullPath);
    }

    public static IEnumerable<object[]> ArchiveDefinitions => TestData.ArchiveDefinitionsAsParams;

    [_7zTheory]
    [MemberData(nameof(ArchiveDefinitions))]
    public void _7z(ArchiveDefinition def)
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.Find7ZExecutable!,
            Extract = ArchiveUtils.Extract7Z,
            ArchiveInfo = TestData.GetTestArchiveInfo(ArchiveType._7z, def),
        });
    }

    [RarTheory]
    [MemberData(nameof(ArchiveDefinitions))]
    public void Rar(ArchiveDefinition def)
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.FindRarExecutable!,
            Extract = ArchiveUtils.ExtractRar,
            ArchiveInfo = TestData.GetTestArchiveInfo(ArchiveType.Rar, def),
        });
    }
}