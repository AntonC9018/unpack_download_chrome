public sealed class RegistryChromeVersionTest
{
    [Fact]
    public void Test()
    {
        Assert.True(InstallationHelper.CheckIsInstalledChromeArch(InstallationHelper.Arch._64bit));
        // Assert.False(InstallationHelper.CheckIsInstalledChromeArch(InstallationHelper.Arch._32bit));
    }
}