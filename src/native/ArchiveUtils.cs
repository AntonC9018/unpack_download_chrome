using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;

public sealed class ArchiveExecutablesContext
{
    public string? RarPath { get; private set; }
    public string? _7ZPath { get; private set; }

    public void Refresh()
    {
        var _7z = ArchiveUtils.Find7ZExecutable();
        var rar = ArchiveUtils.FindRarExecutable();
        _7ZPath = _7z;
        RarPath = rar;
    }

    public static ArchiveExecutablesContext Create()
    {
        var ret = new ArchiveExecutablesContext();
        ret.Refresh();
        return ret;
    }
}

public static class ArchiveUtils
{
    public struct ExtractParams
    {
        public required ArchiveExecutablesContext Context;
        public required string InputFilePath;
        public required ArchiveType Type;
        public required string OutputDirectoryPath;
    }

    public static bool ExtractArchive(ExtractParams p)
    {
        Stream OpenInput()
        {
            var fileStream = File.OpenRead(p.InputFilePath);
            return fileStream;
        }

        try
        {
            switch (p.Type)
            {
                case ArchiveType.Zip:
                {
                    using var fileStream = OpenInput();
                    ZipFile.ExtractToDirectory(p.InputFilePath, p.OutputDirectoryPath);
                    return true;
                }
                case ArchiveType.Rar:
                {
                    if (p.Context.RarPath is null)
                    {
                        return false;
                    }
                    bool ret = ExtractRar(new()
                    {
                        ExecutablePath = p.Context.RarPath,
                        InputFilePath = p.InputFilePath,
                        OutputDirectoryPath = p.OutputDirectoryPath,
                    });
                    return ret;
                }
                case ArchiveType._7z:
                {
                    if (p.Context._7ZPath is null)
                    {
                        return false;
                    }
                    bool ret = Extract7Z(new()
                    {
                        ExecutablePath = p.Context._7ZPath,
                        InputFilePath = p.InputFilePath,
                        OutputDirectoryPath = p.OutputDirectoryPath,
                    });
                    return ret;
                }
                case ArchiveType.Tar:
                {
                    using var file = OpenInput();
                    TarFile.ExtractToDirectory(file, p.OutputDirectoryPath, overwriteFiles: false);
                    return true;
                }
                case ArchiveType.TarGz:
                {
                    using var file = OpenInput();
                    using var gzipStream = new GZipStream(file, CompressionMode.Decompress);
                    TarFile.ExtractToDirectory(gzipStream, p.OutputDirectoryPath, overwriteFiles: false);
                    return true;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(p.Type));
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public struct ExtractWithExecutableParams
    {
        public required string InputFilePath;
        public required string OutputDirectoryPath;
        public required string ExecutablePath;
    }

    private static bool ExecuteWithArgs(string program, string[] args)
    {
        var startInfo = new ProcessStartInfo(
            program,
            arguments: args);
        var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return true;
        }
        return false;
    }

    public static bool ExtractRar(ExtractWithExecutableParams p)
    {
        return ExecuteWithArgs(
            p.ExecutablePath,
            [
                "x",
                p.InputFilePath,
                p.OutputDirectoryPath,
            ]);
    }

    public static bool Extract7Z(ExtractWithExecutableParams p)
    {
        return ExecuteWithArgs(
            p.ExecutablePath,
            [
                "x",
                p.InputFilePath,
                $"-o\"{p.OutputDirectoryPath}\"",
            ]);
    }

    public struct FindExecutableParams
    {
        public required string DefaultInstallationDirectory;
        public required string ExecutableName;
    }

    public static string? FindExecutable(FindExecutableParams p)
    {
        Debug.Assert(p.ExecutableName.EndsWith(".exe"));
        Debug.Assert(!p.ExecutableName.Contains(Path.DirectorySeparatorChar));

        {
            var fullPath = Path.Combine(p.DefaultInstallationDirectory, p.ExecutableName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        {
            if (FindExecutableInPath() is { } rar)
            {
                return rar;
            }
        }
        return null;

        string? FindExecutableInPath()
        {
            var startInfo = new ProcessStartInfo(fileName: "where", arguments: [p.ExecutableName]);
            startInfo.RedirectStandardOutput = true;
            var result = Process.Start(startInfo);
            Debug.Assert(result != null);
            using var standardOutput = result.StandardOutput;
            var line = standardOutput.ReadLine();
            result.WaitForExit();
            if (line == null)
            {
                Debug.Assert(result.ExitCode != 0);
                return null;
            }
            return line;
        }
    }

    public static string? FindRarExecutable()
    {
        var ret = FindExecutable(new()
        {
            ExecutableName = "rar.exe",
            DefaultInstallationDirectory = @"C:\Program Files\WinRAR",
        });
        return ret;
    }

    public static string? Find7ZExecutable()
    {
        var ret = FindExecutable(new()
        {
            ExecutableName = "7z.exe",
            DefaultInstallationDirectory = @"C:\Program Files\7-Zip",
        });
        return ret;
    }
}

public enum ArchiveType
{
    Zip,
    Rar,
    Tar,
    TarGz,
    _7z,
}