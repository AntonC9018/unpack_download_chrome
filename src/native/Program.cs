using System.Text;

using var stdin = Console.OpenStandardInput();
using var logFile = CreateOrOpenLogFile();
using var logger = new StreamWriter(logFile, Encoding.UTF8, leaveOpen: true);
using var stdout = Console.OpenStandardOutput();
var context = ArchiveExecutablesContext.Create();
while (!AppDomain.CurrentDomain.IsFinalizingForUnload())
{
    Loop();
}

void Loop()
{
    var message = ReadHelper.Read(stdin);

    // TODO: Implement options?
    if (message.RefreshTools)
    {
        context.Refresh();
        WriteHelper.WriteObject(stdout, new StatusResponse
        {
            Found7Z = context._7ZPath != null,
            FoundWinRar = context.RarPath != null,
        });
        return;
    }

    if (message.FilePath is not { } filePath)
    {
        return;
    }

    try
    {
        var result = ArchiveUtils.ExtractToDirectoryWithSameName(new()
        {
            Context = context,
            FilePath = filePath,
        });
        if (result.Status == ExtractStatus.Success)
        {
            File.Delete(filePath);
            OpenDirectoryInExplorer(result.OutputDirectory!);
        }
    }
    catch (Exception e)
    {
        logger.WriteLine(e.Message);
    }
}

void OpenDirectoryInExplorer(string directory)
{
    ExplorerHelper.OpenFolderAndSelectFile(directory);
}

Stream CreateOrOpenLogFile()
{
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var myAppDir = Path.Combine(appData, "unpack_zip");
    Directory.CreateDirectory(myAppDir);
    var logFilePath = Path.Combine(myAppDir, "log.txt");
    var ret = File.OpenWrite(logFilePath);
    return ret;
}
