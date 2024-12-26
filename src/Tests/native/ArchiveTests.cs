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
        var tempDir = Directory.CreateTempSubdirectory().FullName;

        try
        {
            var fullPathToData = Path.GetFullPath(p.DataFileName);
            bool success = p.Extract(new()
            {
                ExecutablePath = path,
                InputFilePath = fullPathToData,
                OutputDirectoryPath = tempDir,
            });
            Assert.True(success);

            var filePath = Path.Combine(tempDir, "ghoul.jpeg");
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [_7zFact]
    public void _7z()
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.Find7ZExecutable!,
            Extract = ArchiveUtils.Extract7Z,
            DataFileName = "data/ghoul.7z",
        });
    }

    [RarFact]
    public void Rar()
    {
        Test(new()
        {
            FindExecutable = ArchiveUtils.FindRarExecutable!,
            Extract = ArchiveUtils.ExtractRar,
            DataFileName = "data/ghoul.rar",
        });
    }
}