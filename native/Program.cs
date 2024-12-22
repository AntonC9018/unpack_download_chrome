using System.Diagnostics;
using System.IO.Compression;

using var stdin = Console.OpenStandardInput();
using var logFile = File.OpenWrite(@"C:\Users\Anton\Desktop\ext\log.txt");
while (true)
{
    Loop();
}

void Loop()
{
    Debugger.Launch();
    var message = ReadHelper.Read(stdin);
    using var fileStream = File.OpenRead(message.FilePath);

    // To support other formats:
    // https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
    if (!message.FilePath.EndsWith(".zip"))
    {
        return;
    }

    string FilePathWithoutExtension()
    {
        var dotIndex = message.FilePath.AsSpan().LastIndexOf(".");

        // Checked the extension previously.
        Debug.Assert(dotIndex != -1);

        return message.FilePath[.. dotIndex];
    }

    var filePathWithoutExtension = FilePathWithoutExtension();
    if (Directory.Exists(filePathWithoutExtension))
    {
        return;
    }

    try
    {
        ZipFile.ExtractToDirectory(message.FilePath, filePathWithoutExtension);
    }
    catch (Exception e)
    {
        WriteHelper.WriteError(logFile, e);
        logFile.Write([(byte) '\n']);
    }
}