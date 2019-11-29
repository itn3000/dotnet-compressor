// var SourceUrl = Argument("SourceUrl", "");
var ApiKey = Argument("ApiKey", "");
var UserName = Argument("UserName", "");
var Password = Argument("Password", "");

Task("NuGet.GetBinary")
    .Does(() =>
    {
        var exePath = Directory("tools").Path.CombineWithFilePath("nuget.exe");
        if(FileExists(exePath))
        {
            return;
        }
        if(!DirectoryExists(exePath.GetDirectory()))
        {
            CreateDirectory(exePath.GetDirectory());
        }
        var tmpPath = DownloadFile("https://dist.nuget.org/win-x86-commandline/v5.3.1/nuget.exe");
        MoveFile(tmpPath, exePath);
    });

Task("NuGet.Push.NuGetOrg")
    .IsDependentOn("NuGet.GetBinary")
    .Does(() =>
    {
        var pushSettings = new NuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = ApiKey
        };
        var files = GetFiles(Directory("dist").Path.Combine(Configuration).Combine("nupkg").Combine("*.nupkg").ToString());
        NuGetPush(files, pushSettings);
    });

Task("NuGet.Push.GitHub")
    .IsDependentOn("NuGet.GetBinary")
    .Does(() =>
    {
        var SourceUrl = "https://nuget.pkg.github.com/itn3000/index.json";
        var SourceName = "GitHub";
        if(NuGetHasSource(SourceUrl))
        {
            Information("overwrite source");
            NuGetRemoveSource(SourceName, SourceUrl);
        }
        var settings = new NuGetSourcesSettings()
        {
            IsSensitiveSource = true,
        };
        if(!string.IsNullOrEmpty(UserName))
        {
            settings.UserName = UserName;
        }
        if(!string.IsNullOrEmpty(Password))
        {
            settings.Password = Password;
        }
        Information($"src is {SourceName}, {SourceUrl}");
        NuGetAddSource(SourceName, SourceUrl, settings);
        Information($"source add done");
        var pushSettings = new DotNetCoreNuGetPushSettings()
        {
            Source = SourceName
        };
        var pushSettings = new NuGetPushSettings()
        {
            Source = SourceName,
        };
        if(!string.IsNullOrEmpty(ApiKey))
        {
            pushSettings.ApiKey = ApiKey;
        }
        var files = GetFiles(Directory("dist").Path.Combine(Configuration).Combine("nupkg").Combine("*.nupkg").ToString());
        foreach(var f in files)
        {
            Information($"{f}");
        }
        NuGetPush(files, pushSettings);

        files = GetFiles(Directory("dist").Path.Combine(Configuration).Combine("nupkg").Combine("*.snupkg").ToString());
        foreach(var f in files)
        {
            Information($"{f}");
        }
        NuGetPush(files, pushSettings);
    });
Task("NuGet")
    .IsDependentOn("NuGet.Push.NuGetOrg")
    ;