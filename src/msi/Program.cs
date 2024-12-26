using System.Diagnostics;
using WixSharp;
using WixSharp.CommonTasks;
using WixToolset.Dtf.WindowsInstaller;
using File = WixSharp.File;

public sealed class Program
{
    public static void Main()
    {
        var project = new ManagedProject(
            name: AppConstants.AppName,
            new Dir($@"%ProgramFiles%\{AppConstants.AppName}",
                FilesAsObjects()));

        project.Platform = Platform.x64;
        project.ManagedUI = ManagedUI.Default;
        project.GUID = new Guid("18681f28-d57e-473d-b38e-be5fdc216ad3");

        {
            var afterInstall = new ManagedAction(Msi_AfterInstall)
            {
                Impersonate = true,
                Condition = Condition.NOT_Installed,
                Return = Return.check,
                Step = Step.InstallFinalize,
                When = When.After,
            };
            var afterUninstall = new ManagedAction(Msi_AfterUninstall)
            {
                Impersonate = true,
                Condition = Condition.BeingUninstalled,
                Return = Return.check,
                Step = Step.InstallFinalize,
                When = When.After,
            };
            project.AddActions(afterInstall, afterUninstall);
        }

        Compiler.BuildMsi(project);
    }

    [CustomAction]
    public static ActionResult Msi_AfterInstall(Session s)
    {
        if (!s.IsInstalling())
        {
            s.Log("Install action not executed, because we're not installing?");
            return ActionResult.NotExecuted;
        }
        var installDirectory = s.Property("INSTALLDIR");
        NativeMessagingRegistryHelper.Init(installDirectory);
        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult Msi_AfterUninstall(Session s)
    {
        if (!s.IsUninstalling())
        {
            s.Log("Uninstall action not executed, because we're not uninstalling?");
            return ActionResult.NotExecuted;
        }
        NativeMessagingRegistryHelper.Deinit();
        return ActionResult.Success;
    }

    private static WixEntity[] FilesAsObjects()
    {
        var cwd = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(cwd, "*", SearchOption.AllDirectories).Select(x =>
        {
            var len = cwd.Length + 1;
            var relativePath = x.Substring(len);
            return relativePath;
        });
        var filesByDirectory = files.GroupBy(x =>
        {
            var lastSepIndex = x.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSepIndex == -1)
            {
                return "";
            }
            return x.Substring(0, lastSepIndex);
        });
        var ret = filesByDirectory.Select(x =>
            {
                var filesInDir = x.Select(f => (WixEntity) new File(f));
                if (x.Key == "")
                {
                    return filesInDir;
                }
                return [new Dir(x.Key, filesInDir.ToArray())];
            })
            .SelectMany(x => x)
            .ToArray();
        return ret;
    }
}
