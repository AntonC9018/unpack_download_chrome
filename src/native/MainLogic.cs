using System.Text;

public sealed class Application : IDisposable
{
    private readonly Stream _in;
    private readonly Stream _out;
    private readonly StreamWriter _log;
    private readonly ArchiveExecutablesContext _context;
    private readonly Action<string> _actionAfterUnpack;

    public Application(
        Stream @in,
        Stream @out,
        StreamWriter log,
        ArchiveExecutablesContext context,
        Action<string>? actionAfterUnpack)
    {
        _in = @in;
        _out = @out;
        _context = context;
        _log = log;
        _actionAfterUnpack = actionAfterUnpack ?? (_ => {});
    }

    public void Dispose()
    {
        _in.Dispose();
        _out.Dispose();
        _log.Dispose();
    }

    public static Application CreateDefault()
    {
        StreamWriter? logger = null;
        Stream? stdin = null;
        Stream? stdout = null;
        try
        {
            logger = CreateOrOpenDefaultLogFile();
            stdin = Console.OpenStandardInput();
            stdout = Console.OpenStandardOutput();
        }
        catch (Exception)
        {
            logger?.Dispose();
            stdin?.Dispose();
            stdout?.Dispose();
            throw;
        }
        var context = ArchiveExecutablesContext.Create();
        return new Application(
            @in: stdin,
            @out: stdout,
            log: logger,
            context: context,
            actionAfterUnpack: OpenDirectoryInExplorer);

        static void OpenDirectoryInExplorer(string directory)
        {
            ExplorerHelper.OpenFolderAndSelectFile(directory);
        }
    }

    public bool MainLoop()
    {
        try
        {
            while (!AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                ReadAndProcessMessage();
            }
            return true;
        }
        catch (Exception e)
        {
            _log.WriteLine(e.Message);
            if (e.StackTrace is { } s)
            {
                _log.WriteLine(s);
            }
            return false;
        }
    }

    public void ReadAndProcessMessage()
    {
        var message = ReadHelper.Read(_in);
        if (message is null)
        {
            return;
        }

        // TODO: Implement options?
        if (message.RefreshTools)
        {
            _context.Refresh();
            WriteHelper.WriteObject(_out, new StatusResponse
            {
                Found7Z = _context._7ZPath != null,
                FoundWinRar = _context.RarPath != null,
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
                Context = _context,
                FilePath = filePath,
            });
            if (result.Status == ExtractStatus.Success)
            {
                File.Delete(filePath);
                _actionAfterUnpack(result.OutputDirectory!);
            }
        }
        catch (Exception e)
        {
            _log.WriteLine(e.Message);
        }
    }

    public static StreamWriter CreateOrOpenDefaultLogFile()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var myAppDir = Path.Combine(appData, AppConstants.AppName);
        Directory.CreateDirectory(myAppDir);
        var logFilePath = Path.Combine(myAppDir, "log.txt");
        var ret = File.OpenWrite(logFilePath);
        return new StreamWriter(ret, Encoding.UTF8, leaveOpen: false);
    }
}