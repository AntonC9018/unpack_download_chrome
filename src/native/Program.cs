using System.Diagnostics;
using System.Text;

using var stdin = Console.OpenStandardInput();
using var logFile = File.OpenWrite(@"C:\Users\Anton\Desktop\ext\log.txt");
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
    var processInfo = new ProcessStartInfo(
        fileName: "explorer.exe",
        arguments: [directory]);
    processInfo.RedirectStandardOutput = true;
    Process.Start(processInfo);
}
