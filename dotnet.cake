#addin nuget:?package=Cake.FileHelpers&version=6.1.3
var configuration = Argument("configuration", "Debug");
var selfContained = HasArgument("self-contained") || HasArgument("sc");
var prefix = Argument("prefix", IsRunningOnLinux() ? "/usr" : "\\app"); // prefix doesn't have to start with a path separator

Task("Clean")
    .Does(() =>
    {
        CleanDirectory($"{projectName}.{projectSuffix}{sep}bin{sep}{configuration}");
    });

Task("Build")
    .Does(() =>
    {
        DotNetBuild($"{projectName}.{projectSuffix}{sep}{projectName}.{projectSuffix}.csproj", new DotNetBuildSettings
        {
            Configuration = configuration
        });
    });

Task("Run")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetRun($"{projectName}.{projectSuffix}{sep}{projectName}.{projectSuffix}.csproj", new DotNetRunSettings
        {
            Configuration = configuration,
            NoBuild = true
        });
    });

Task("Publish")
    .Does(() =>
    {
        var runtime = Argument("runtime", "");
        if (string.IsNullOrEmpty(runtime))
        {
            runtime = IsRunningOnLinux() ? "linux-" : "win-";
            runtime += System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLower();
        }
        var outDir = EnvironmentVariable("NICK_BUILDDIR", "_nickbuild");
        CleanDirectory(outDir);
        if (!prefix.StartsWith(sep))
        {
            prefix = $"{sep}{prefix}";
        }
        var libDir = string.IsNullOrEmpty(prefix) ? "lib" : $"{prefix}{sep}lib";
        var publishDir = $"{outDir}{libDir}{sep}{appId}";
        var exitCode = 0;
        Information($"Publishing {projectName}.{projectSuffix} ({runtime})...");
        DotNetPublish($"{projectName}.{projectSuffix}{sep}{projectName}.{projectSuffix}.csproj", new DotNetPublishSettings
        {
            Configuration = "Release",
            SelfContained = selfContained,
            OutputDirectory = publishDir,
            Sources = Argument("sources", "").Split(" "),
            Runtime = runtime,
            HandleExitCode = code => {
                exitCode = code;
                return false;
            }
        });
        if (exitCode != 0)
        {
            throw new CakeException(exitCode);
        }
        // Post-publish
        if (IsRunningOnLinux())
        {
            PostPublishLinux(outDir, prefix, libDir);
        }
        if (projectSuffix == "GNOME")
        {
            PostPublishGNOME(outDir, prefix, libDir);
        }
    });

void PostPublishLinux(string outDir, string prefix, string libDir)
{
    var binDir = string.IsNullOrEmpty(prefix) ? $"{outDir}/bin" : $"{outDir}{prefix}/bin";
    var shareDir = string.IsNullOrEmpty(prefix) ? $"{outDir}/share" : $"{outDir}{prefix}/share";
    // Add launch script
    CreateDirectory(binDir);
    CopyFileToDirectory($"./{projectName}.Shared/Linux/{appId}.in", binDir);
    ReplaceTextInFiles($"{binDir}/{appId}.in", "@EXEC@", selfContained ? $"{libDir}/{appId}/{projectName}.{projectSuffix}" : $"dotnet {libDir}/{appId}/{projectName}.{projectSuffix}.dll");
    MoveFile($"{binDir}/{appId}.in", $"{binDir}/{appId}");
    StartProcess("chmod", new ProcessSettings{
        Arguments = $"+x {binDir}/{appId}"
    });
    // Add icons
    var iconsScalableDir = $"{shareDir}/icons/hicolor/scalable/apps";
    CreateDirectory(iconsScalableDir);
    CopyFileToDirectory($"./{projectName}.Shared/Resources/{appId}.svg", iconsScalableDir);
    CopyFileToDirectory($"./{projectName}.Shared/Resources/{appId}-devel.svg", iconsScalableDir);
    var iconsSymbolicDir = $"{shareDir}/icons/hicolor/symbolic/apps";
    CreateDirectory(iconsSymbolicDir);
    CopyFileToDirectory($"./{projectName}.Shared/Resources/{appId}-symbolic.svg", iconsSymbolicDir);
    // Add desktop file
    var desktopDir = $"{shareDir}/applications";
    CreateDirectory(desktopDir);
    CopyFileToDirectory($"./{projectName}.Shared/Linux/{appId}.desktop.in", desktopDir);
    ReplaceTextInFiles($"{desktopDir}/{appId}.desktop.in", "@EXEC@", $"{prefix}/bin/{appId}");
    StartProcess("msgfmt", new ProcessSettings {
        Arguments = $"--desktop --template={desktopDir}/{appId}.desktop.in -o {desktopDir}/{appId}.desktop -d ./{projectName}.Shared/Resources/po/"
    });
    DeleteFile($"{desktopDir}/{appId}.desktop.in");
    // Add metainfo file
    var metainfoDir = $"{shareDir}/metainfo";
    CreateDirectory(metainfoDir);
    CopyFileToDirectory($"./{projectName}.Shared/Linux/{appId}.metainfo.xml.in", metainfoDir);
    StartProcess("msgfmt", new ProcessSettings {
        Arguments = $"--xml --template={metainfoDir}/{appId}.metainfo.xml.in -o {metainfoDir}/{appId}.metainfo.xml -d ./{projectName}.Shared/Resources/po/"
    });
    DeleteFile($"{metainfoDir}/{appId}.metainfo.xml.in");
    // Add extension file
    if (FileExists($"{projectName}.Shared/Linux/{appId}.extension.xml"))
    {
        var mimeDir = $"{shareDir}/mime/packages";
        CreateDirectory(mimeDir);
        CopyFileToDirectory($"{projectName}.Shared/Linux/{appId}.extension.xml", mimeDir);
    }
}

void PostPublishGNOME(string outDir, string prefix, string libDir)
{
    var shareDir = string.IsNullOrEmpty(prefix) ? $"{outDir}{sep}share" : $"{outDir}{prefix}{sep}share";
    // Add gresource
    CreateDirectory($"{shareDir}{sep}{appId}");
    MoveFileToDirectory($"{outDir}{libDir}{sep}{appId}{sep}{appId}.gresource", $"{shareDir}{sep}{appId}");
    // Add DBus service (if exists)
    if (FileExists($"{projectName}.GNOME{sep}{appId}.service.in"))
    {
        var servicesDir = $"{shareDir}{sep}dbus-1{sep}services";
        CreateDirectory(servicesDir);
        CopyFileToDirectory($"{projectName}.GNOME{sep}{appId}.service.in", servicesDir);
        ReplaceTextInFiles($"{servicesDir}{sep}{appId}.service.in", "@PREFIX@", $"{prefix}");
        MoveFile($"{servicesDir}{sep}{appId}.service.in", $"{servicesDir}{sep}{appId}.service");
        FileAppendLines($"{shareDir}{sep}applications{sep}{appId}.desktop" , new string[] { "DBusActivatable=true" });
    }
    // Add Yelp docs (if exist)
    if (DirectoryExists($"{projectName}.Shared{sep}Docs"))
    {
        var docsDir = $"{shareDir}{sep}help";
        // C for English
        CreateDirectory($"{docsDir}{sep}C{sep}{shortName}");
        CopyDirectory($"{projectName}.Shared{sep}Docs{sep}yelp{sep}C", $"{docsDir}{sep}C{sep}{shortName}");
        // Other languages
        foreach (var lang in FileReadLines($"{projectName}.Shared{sep}Docs{sep}po{sep}LINGUAS"))
        {
            CreateDirectory($"{docsDir}{sep}{lang}{sep}{shortName}");
            CopyDirectory($"{projectName}.Shared{sep}Docs{sep}yelp{sep}{lang}", $"{docsDir}{sep}{lang}{sep}{shortName}");
        }
    }
}