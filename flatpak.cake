
Task("FlatpakBuild")
    .Does(() =>
    {
        if (!IsRunningOnLinux())
        {
            throw new CakeException("This task can only be executed on Linux.");
        }
        StartProcess("flatpak", new ProcessSettings
        {
            Arguments = $"run org.flatpak.Builder --force-clean --disable-rofiles-fuse _build flatpak/{appId}.json"
        });
    });

Task("FlatpakRun")
    .Does(() =>
    {
        if (!IsRunningOnLinux())
        {
            throw new CakeException("This task can only be executed on Linux.");
        }
        var uid = EnvironmentVariable("UID", "1000");
        StartProcess("flatpak", new ProcessSettings
        {
            Arguments = $"build --with-appdir --talk-name=org.freedesktop.portal.* --talk-name=org.a11y.Bus --bind-mount=/run/user/{uid}/doc=/run/user/{uid}/doc/by-app/{appId} _build {appId}"
        });
    });