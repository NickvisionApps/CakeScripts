Task("GeneratePot")
    .Does(() =>
    {
        var dirs = $"-s .{sep}{projectName}.Shared";
        foreach (var dir in new []{ $"{projectName}.GNOME", $"{projectName}.WinUI" })
        {
            if (DirectoryExists(dir))
            {
                dirs = $"{dirs} -s .{sep}{dir}";
            }
        }
        StartProcess($"GetText.Extractor{(IsRunningOnWindows() ? ".exe" : "")}", new ProcessSettings {
            Arguments = $"-o {dirs} -as \"_\" -ad \"_p\" -ap \"_n\" -adp \"_pn\" -t .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot"
        });
        if (IsRunningOnWindows())
        {
            StartProcess("powershell", new ProcessSettings {
                Arguments = $"-Command \"xgettext --from-code=UTF-8 --add-comments --keyword=_ --keyword=C_:1c,2 -o .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot -j .{sep}{projectName}.GNOME{sep}Blueprints{sep}*.blp\""
            });
        }
        else
        {
            StartProcess("sh", new ProcessSettings {
                Arguments = $"-c \"xgettext --from-code=UTF-8 --add-comments --keyword=_ --keyword=C_:1c,2 -o .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot -j .{sep}{projectName}.GNOME{sep}Blueprints{sep}*.blp\""
            });
        }
        StartProcess("xgettext", new ProcessSettings {
            Arguments = $"-o .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot -j .{sep}{projectName}.Shared{sep}{appId}.desktop.in"
        });
        StartProcess("xgettext", new ProcessSettings {
            Arguments = $"-o .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot -j --its .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}metainfo.its .{sep}{projectName}.Shared{sep}{appId}.metainfo.xml.in"
        });
    });

Task("UpdatePo")
    .Does(() =>
    {
        foreach (var lang in FileReadLines($".{sep}{projectName}.Shared{sep}Resources{sep}po{sep}LINGUAS"))
        {
            StartProcess("msgmerge", new ProcessSettings {
                Arguments = $"-U .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{lang}.po .{sep}{projectName}.Shared{sep}Resources{sep}po{sep}{shortName}.pot"
            });
        }
    });