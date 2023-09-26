Task("DocsGeneratePot")
    .Does(() =>
    {
        EnsureLinux();
        StartProcess("sh", new ProcessSettings {
            Arguments = $"-c \"itstool -o po{sep}{shortName}.pot yelp{sep}C{sep}*.page\"",
            WorkingDirectory = $"{projectName}.Shared{sep}Docs"
        });
    });

Task("DocsUpdatePo")
    .Does(() =>
    {
        EnsureLinux();
        foreach (var lang in FileReadLines($".{sep}{projectName}.Shared{sep}Docs{sep}po{sep}LINGUAS"))
        {
            Information($"Updating PO for {lang}...");
            StartProcess("msgmerge", new ProcessSettings {
                Arguments = $"-U .{sep}{projectName}.Shared{sep}Docs{sep}po{sep}{lang}.po .{sep}{projectName}.Shared{sep}Docs{sep}po{sep}{shortName}.pot"
            });
        }
    });

Task("DocsUpdateYelp")
    .Does(() =>
    {
        EnsureLinux();
        foreach (var lang in FileReadLines($".{sep}{projectName}.Shared{sep}Docs{sep}po{sep}LINGUAS"))
        {
            Information($"Updating Yelp docs for {lang}...");
            StartProcess("msgfmt", new ProcessSettings {
                Arguments = $"{lang}.po -o {lang}.mo",
                WorkingDirectory = $"{projectName}.Shared{sep}Docs{sep}po"
            });
            CreateDirectory($".{sep}{projectName}.Shared{sep}Docs{sep}yelp{sep}{lang}");
            StartProcess("sh", new ProcessSettings {
                Arguments = $"-c \"itstool -m po{sep}{lang}.mo -o yelp{sep}{lang}{sep} yelp{sep}C{sep}*.page\"",
                WorkingDirectory = $"{projectName}.Shared{sep}Docs"
            });
            DeleteFile($"{projectName}.Shared{sep}Docs{sep}po{sep}{lang}.mo");
            CreateDirectory($".{sep}{projectName}.Shared{sep}Docs{sep}yelp{sep}{lang}{sep}figures");
            CopyDirectory($".{sep}{projectName}.Shared{sep}Docs{sep}yelp{sep}C{sep}figures", $".{sep}{projectName}.Shared{sep}Docs{sep}yelp{sep}{lang}{sep}figures");
        }
    });

Task("DocsUpdateHtml")
    .Does(() =>
    {
        EnsureLinux();
        //C for english
        CreateDirectory($"{projectName}.Shared{sep}Docs{sep}html{sep}C");
        CreateDirectory($"{projectName}.Shared{sep}Docs{sep}html{sep}C{sep}figures");
        CopyDirectory($"{projectName}.Shared{sep}Docs{sep}yelp{sep}C{sep}figures", $"{projectName}.Shared{sep}Docs{sep}html{sep}C{sep}figures");
        Information("Generating html for C...");
        StartProcess("yelp-build", new ProcessSettings {
            Arguments = $"html -o html{sep}C{sep} yelp{sep}C{sep}",
            WorkingDirectory = $"{projectName}.Shared{sep}Docs"
        });
        //Other langs
        foreach (var lang in FileReadLines($".{sep}{projectName}.Shared{sep}Docs{sep}po{sep}LINGUAS"))
        {
            Information($"Generating html for {lang}...");
            CreateDirectory($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}");
            StartProcess("yelp-build", new ProcessSettings {
                Arguments = $"html -o html{sep}{lang}{sep} yelp{sep}{lang}{sep}",
                WorkingDirectory = $"{projectName}.Shared{sep}Docs"
            });
            DeleteDirectory($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}figures", new DeleteDirectorySettings {
                Force = true,
                Recursive = true
            });
            DeleteFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.css");
            DeleteFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.js");
            ReplaceRegexInFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.html", "href=\".*.css\"", "href=\"../C/C.css\"");
            ReplaceRegexInFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.html", "src=\"highlight.pack.js\"", "src=\"../C/highlight.pack.js\"");
            ReplaceRegexInFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.html", "src=\"yelp.js\"", "src=\"../C/yelp.js\"");
            ReplaceRegexInFiles($"{projectName}.Shared{sep}Docs{sep}html{sep}{lang}{sep}*.html", $"src=\"figures/{shortName}.png\"", $"src=\"../C/figures/{shortName}.png\"");
        }
    });

Task("DocsUpdateAll")
    .IsDependentOn("DocsGeneratePot")
    .IsDependentOn("DocsUpdatePo")
    .IsDependentOn("DocsUpdateYelp")
    .IsDependentOn("DocsUpdateHtml");