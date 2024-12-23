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

    ArchiveType? GetArchiveType()
    {
        if (filePath.EndsWith(".zip"))
        {
            return ArchiveType.Zip;
        }
        if (filePath.EndsWith(".rar"))
        {
            return ArchiveType.Rar;
        }
        if (filePath.EndsWith(".7z"))
        {
            return ArchiveType._7z;
        }
        if (filePath.EndsWith(".tar.gz"))
        {
            return ArchiveType.TarGz;
        }
        if (filePath.EndsWith(".tar"))
        {
            return ArchiveType.Tar;
        }
        return null;
    }

    if (GetArchiveType() is not { } archiveType)
    {
        return;
    }

    string FilePathWithoutExtension()
    {
        var filePathSpan = filePath.AsSpan();
        var lastDirSeparatorPath = filePathSpan.LastIndexOfAny(['\\', '/']);
        int lastPartStart = lastDirSeparatorPath + 1;
        var lastPart = filePathSpan[lastPartStart ..];
        var dotIndex = lastPart.IndexOf(".");

        // Checked the extension previously.
        Debug.Assert(dotIndex != -1);

        return filePath[.. (lastPartStart + dotIndex)];
    }

    var filePathWithoutExtension = FilePathWithoutExtension();
    if (Directory.Exists(filePathWithoutExtension))
    {
        return;
    }

    try
    {
        ArchiveUtils.ExtractArchive(new()
        {
            Context = context,
            Type = archiveType,
            InputFilePath = filePath,
            OutputDirectoryPath = filePathWithoutExtension,
        });
        File.Delete(filePath);
    }
    catch (Exception e)
    {
        logger.WriteLine(e.Message);
    }
}