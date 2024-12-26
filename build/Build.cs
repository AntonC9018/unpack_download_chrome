using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Build1);

    [Parameter("Whether to package the .net runtime. For local tests do 'false' since that makes it compile faster.")]
    readonly bool Standalone;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    readonly AbsolutePath SrcDirectory = RootDirectory / "src";
    readonly AbsolutePath TempDirectory = RootDirectory / "temp";
    AbsolutePath ExtensionDirectory => SrcDirectory / "extension";
    AbsolutePath OutputDirectory => RootDirectory / "artifacts" / this.Configuration;

    AbsolutePath TempPluginDirectory => TempDirectory / "plugin";

    Target Clean => o => o
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean();
        });

    Target Restore => o => o
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore();
            // NpmTasks.NpmInstall(x =>
            // {
            //     x = x.SetProcessWorkingDirectory(ExtensionDirectory);
            //     return x;
            // });
        });

    AbsolutePath GetProject(string name)
    {
        return SrcDirectory / name / (name + ".csproj");
    }

    AbsolutePath BuildProj(string projectDirName)
    {
        var output = TempDirectory / projectDirName;
        DotNetTasks.DotNetPublish(x =>
        {
            var pp = GetProject(projectDirName);
            x = x.SetProject(pp);
            x = x.SetConfiguration(Configuration);
            x = x.SetOutput(output);
            x = x.SetNoRestore(true);
            if (Standalone)
            {
                x = x.SetSelfContained(true);
                x = x.AddProcessAdditionalArguments("-r", "win-x64");
            }
            return x;
        });
        return output;
    }

    Target BuildDeps => o => o
        .DependsOn(Restore)
        .Executes(() =>
        {
            TempDirectory.CreateOrCleanDirectory();
            OutputDirectory.CreateOrCleanDirectory();

            var nativeOutput = BuildProj("native");

            var tempPluginDir = TempPluginDirectory;
            tempPluginDir.CreateOrCleanDirectory();

            ExtensionDirectory.Copy(
                tempPluginDir,
                excludeDirectory: x =>
                {
                    if (x.Name is "node_modules")
                    {
                        return true;
                    }
                    if (x.Name is "package.json")
                    {
                        return true;
                    }
                    if (x.Name is "package.lock.json")
                    {
                        return true;
                    }
                    return false;
                },
                policy: ExistsPolicy.DirectoryMerge | ExistsPolicy.FileFail);

            var binDir = tempPluginDir / "bin";
            binDir.CreateOrCleanDirectory();
            nativeOutput.Move(binDir, policy: ExistsPolicy.FileFail | ExistsPolicy.DirectoryMerge);
        });

    Target Build1 => o => o
        .DependsOn(BuildDeps)
        .Executes(() =>
        {
            var msiPath = GetProject("msi");
            DotNetTasks.DotNetRun(x =>
            {
                x = x.SetNoRestore(true);
                x = x.SetProjectFile(msiPath);
                x = x.SetProcessWorkingDirectory(TempPluginDirectory);
                return x;
            });

            var msiFileName = AppConstants.AppName + ".msi";
            var msiFilePath = TempPluginDirectory / msiFileName;
            msiFilePath.CopyToDirectory(OutputDirectory, policy: ExistsPolicy.FileOverwrite);
        });

}
