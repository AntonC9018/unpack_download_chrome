using System.IO.Compression;

using var stdin = Console.OpenStandardInput();
using var stdout = Console.OpenStandardOutput();
while (true)
{
    var message = ReadHelper.Read(stdin);
    using var fileStream = File.OpenRead(message.FilePath);
    var filePathWithoutExtension = Path.GetFileNameWithoutExtension(message.FilePath);

    // To support other formats:
    // https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples
    if (!message.FilePath.EndsWith(".zip"))
    {
        continue;
    }

    if (Directory.Exists(filePathWithoutExtension))
    {
        continue;
    }

    try
    {
        ZipFile.ExtractToDirectory(message.FilePath, filePathWithoutExtension);
    }
    catch (Exception e)
    {
        WriteHelper.WriteError(stdout, e);
    }
}