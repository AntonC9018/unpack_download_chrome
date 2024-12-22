using System.Diagnostics;
using System.IO.Compression;
using System.Text;

using var stdin = Console.OpenStandardInput();
using var logFile = File.OpenWrite(@"C:\Users\Anton\Desktop\ext\log.txt");
using var logger = new StreamWriter(logFile, Encoding.UTF8, leaveOpen: true);
using var stdout = Console.OpenStandardOutput();
while (!AppDomain.CurrentDomain.IsFinalizingForUnload())
{
    Loop();
}

void Loop()
{
    var message = ReadHelper.Read(stdin);

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
        {
            using var fileStream = File.OpenRead(message.FilePath);
            ZipFile.ExtractToDirectory(message.FilePath, filePathWithoutExtension);
        }
        File.Delete(message.FilePath);
    }
    catch (Exception e)
    {
        logger.WriteLine(e.Message);
    }
}