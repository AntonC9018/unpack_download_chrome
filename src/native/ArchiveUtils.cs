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

public struct ExtractToDirectoryWithSameNameResult
{
    public required ExtractStatus Status;
    public required string? OutputDirectory;
}

public enum ExtractStatus
{
    Failure,
    Success,
    NotArchive,
    DirectoryExists,
}

public static class ArchiveUtils
{
    public struct ExtractToDirectoryWithSameNameParams
    {
        public required string FilePath;
        public required ArchiveExecutablesContext Context;
    }

    public static ExtractToDirectoryWithSameNameResult ExtractToDirectoryWithSameName(ExtractToDirectoryWithSameNameParams p)
    {
        ArchiveType? GetArchiveType()
        {
            bool Ends(string end)
            {
                if (p.FilePath.EndsWith(end, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }
            if (Ends(".zip"))
            {
                return ArchiveType.Zip;
            }
            if (Ends(".rar"))
            {
                return ArchiveType.Rar;
            }
            if (Ends(".7z"))
            {
                return ArchiveType._7z;
            }
            if (Ends(".tar.gz"))
            {
                return ArchiveType.TarGz;
            }
            if (Ends(".tar"))
            {
                return ArchiveType.Tar;
            }
            return null;
        }

        if (GetArchiveType() is not { } archiveType)
        {
            return new()
            {
                Status = ExtractStatus.NotArchive,
                OutputDirectory = null,
            };
        }

        string FilePathWithoutExtension()
        {
            var filePathSpan = p.FilePath.AsSpan();
            var lastDirSeparatorPath = filePathSpan.LastIndexOfAny(['\\', '/']);
            int lastPartStart = lastDirSeparatorPath + 1;
            var lastPart = filePathSpan[lastPartStart ..];
            var dotIndex = lastPart.IndexOf(".");

            // Checked the extension previously.
            Debug.Assert(dotIndex != -1);

            return p.FilePath[.. (lastPartStart + dotIndex)];
        }

        var outputDirectory = FilePathWithoutExtension();
        if (Directory.Exists(outputDirectory))
        {
            return new()
            {
                Status = ExtractStatus.DirectoryExists,
                OutputDirectory = outputDirectory,
            };
        }

        bool success = ExtractArchive(new()
        {
            Context = p.Context,
            Type = archiveType,
            InputFilePath = p.FilePath,
            OutputDirectoryPath = outputDirectory,
        });

        ExtractStatus Status()
        {
            if (success)
            {
                return ExtractStatus.Success;
            }
            return ExtractStatus.Failure;
        }
        return new()
        {
            Status = Status(),
            OutputDirectory = outputDirectory,
        };
    }

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

    private static bool ExecuteWithArgs(
        string program,
        string outputPath,
        string[] args)
    {
        var startInfo = new ProcessStartInfo(
            program,
            arguments: args);
        startInfo.WorkingDirectory = outputPath;

        // The output is going to be ignored
        startInfo.RedirectStandardOutput = true;

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
            program: p.ExecutablePath,
            outputPath: p.OutputDirectoryPath,
            [
                "x",
                p.InputFilePath,
                // No output
                "-inul",
            ]);
    }

    public static bool Extract7Z(ExtractWithExecutableParams p)
    {
        return ExecuteWithArgs(
            program: p.ExecutablePath,
            outputPath: p.OutputDirectoryPath,
            [
                "x",
                p.InputFilePath,
                // There's no flag for no output
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