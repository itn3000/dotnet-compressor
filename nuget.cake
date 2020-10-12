// var SourceUrl = Argument("SourceUrl", "");
var ApiKey = Argument("ApiKey", "");
var UserName = Argument("UserName", "");
var Password = Argument("Password", "");
class NuGetContext
{
    public string ApiKey;
    public string UserName;
    public string Password;
    public string Configuration;
}
Setup(ctx => new NuGetContext()
{
    ApiKey = ctx.Argument("ApiKey", ""),
    UserName = ctx.Argument("UserName", ""),
    Password = ctx.Argument("Password", ""),
    Configuration = ctx.Argument("Configuration", "Debug")
});
Task("NuGet.GetBinary")
    .Does<NuGetContext>((ctx) =>
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
        var tmpPath = DownloadFile("https://dist.nuget.org/win-x86-commandline/v5.7.0/nuget.exe");
        MoveFile(tmpPath, exePath);
    });

Task("NuGet.Push.NuGetOrg")
    .IsDependentOn("NuGet.GetBinary")
    .Does<NuGetContext>((ctx) =>
    {
        var pushSettings = new NuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = ApiKey
        };
        var files = GetFiles(Directory("dist").Path.Combine(ctx.Configuration).Combine("nupkg").Combine("*.nupkg").ToString());
        NuGetPush(files, pushSettings);
    });

Task("NuGet.Push.GitHub")
    .IsDependentOn("NuGet.GetBinary")
    .Does<NuGetContext>((ctx) =>
    {
        var SourceUrl = "https://nuget.pkg.github.com/itn3000/index.json";
        var SourceName = "github";
        // if(NuGetHasSource(SourceUrl))
        // {
        //     Information("overwrite source");
        //     NuGetRemoveSource(SourceName, SourceUrl);
        // }
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
        StartProcess(File("./tools/nuget.exe"), "help");
        var nugetconfig = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <packageSources>
        <clear />
        <add key=""nugetv3"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
        <add key=""github"" value=""{0}"" />
    </packageSources>
    <packageSourceCredentials>
        <github>
            <add key=""username"" value=""{1}"" />
            <add key=""cleartextpassword"" value=""{2}"" />
            <add key=""validauthenticationtypes"" value=""basic"" />
        </github>
    </packageSourceCredentials>
</configuration>
", SourceUrl, UserName, Password);
        CreateDirectory(Directory("tmp"));
        System.IO.File.WriteAllText(Directory("tmp").Path.CombineWithFilePath("NuGet.config").ToString(), nugetconfig);
        try
        {
            // Information($"src is {SourceName}, {SourceUrl}");
            // NuGetAddSource(SourceName, SourceUrl, settings);
            // Information($"source add done");
            var pushSettings = new NuGetPushSettings()
            {
                Source = SourceName,
            };
            if(!string.IsNullOrEmpty(ApiKey))
            {
                pushSettings.ApiKey = ApiKey;
            }
            pushSettings.ToolPath = File("./tools/nuget.exe");
            pushSettings.ConfigFile = File("tmp/NuGet.config");
            var files = GetFiles(Directory("dist").Path.Combine(ctx.Configuration).Combine("nupkg").Combine("*.nupkg").ToString());
            foreach(var f in files)
            {
                Information($"{f}");
                StartProcess(File("./tools/nuget.exe"), $"{f} -Source {SourceName} -ConfigFile ./tmp/NuGet.config -ForceEnglishOutput -Verbosity detailed");
            }
            
            // NuGetPush(files, pushSettings);

            files = GetFiles(Directory("dist").Path.Combine(ctx.Configuration).Combine("nupkg").Combine("*.snupkg").ToString());
            foreach(var f in files)
            {
                Information($"{f}");
                StartProcess(File("./tools/nuget.exe"), $"{f} -Source {SourceName} -ConfigFile ./tmp/NuGet.config -ForceEnglishOutput -Verbosity detailed");
            }
            // NuGetPush(files, pushSettings);
        }
        finally
        {
            CleanDirectory(Directory("tmp"));
        }
    });
Task("NuGet")
    .IsDependentOn("NuGet.Push.NuGetOrg")
    ;