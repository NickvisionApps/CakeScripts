Task("Install")
    .Does(() =>
    {
        EnsureLinux();
        var buildDir = EnvironmentVariable("NICK_BUILDDIR", "_nickbuild");
        var destDir = Argument("destdir", "/");
        CopyDirectory(buildDir, destDir);
    });

Task("FlatpakSourcesGen")
    .Does(() =>
    {
        EnsureLinux();
        var userMode = HasArgument("user") || HasArgument("u");
        StartProcess("flatpak-dotnet-generator", new ProcessSettings {
            Arguments = $"{projectName}.{projectSuffix}{sep}{projectName}.{projectSuffix}.csproj -o {projectName}.{projectSuffix}{sep}nuget-sources.json -a Cake.Tool Cake.FileHelpers{(userMode ? " -u" : "")}"
        });
    });