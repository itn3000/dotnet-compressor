var Configuration = Argument("Configuration", "Debug");
var Target = Argument("Target", "Default");
var Runtime = Argument("Runtime", "");

Task("Default")
    .IsDependentOn("Pack")
    ;

Task("Restore")
    .Does(() =>
    {
        DotNetCoreRestore("dotnet-compressor.slnproj");
    });
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var setting = new DotNetCoreBuildSettings()
        {
            Configuration = Configuration,
        };
        DotNetCoreBuild("dotnet-compressor.slnproj", setting);
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var setting = new DotNetCorePackSettings()
        {
            Configuration = Configuration,
            NoBuild = true,
        };
        if(!string.IsNullOrEmpty(Runtime))
        {
            setting.Runtime = Runtime;
        }
        DotNetCorePack("src/dotnet-compressor/dotnet-compressor.csproj", setting);
    });
Task("Native")
    .IsDependentOn("Build")
    .Does(() =>
    {
        if(string.IsNullOrEmpty(Runtime))
        {
            throw new Exception("you must input Runtime argument");
        }
        var props = new Dictionary<string, string[]>();
        props["WithCoreRT"] = new string[] { "true" };
        var msBuildSetting = new DotNetCoreMSBuildSettings()
            .WithProperty("WithCoreRT", "true")
            ;
        var setting = new DotNetCorePublishSettings()
        {
            Configuration = Configuration,
            MSBuildSettings = msBuildSetting,
            Runtime = Runtime,
        };
        DotNetCorePublish("src/dotnet-compressor/dotnet-compressor.csproj", setting);
    });

RunTarget(Target);