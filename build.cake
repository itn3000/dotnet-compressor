var Configuration = Argument("Configuration", "Debug");
var Target = Argument("Target", "Default");
var Runtime = Argument("Runtime", "");
var VersionSuffix = Argument("VersionSuffix", "");
var IsRelease = HasArgument("IsRelease");
const string DefaultTargetFramework = "net8.0";

#load "nativetest.cake"
#load "nuget.cake"
#load "t4.cake"

Setup(ctx =>
{
    return new NativeTestContext()
    {
        Configuration = ctx.Argument("Configuration", "Debug"),
        Runtime = ctx.Argument("Runtime", ""),
        VersionSuffix = ctx.Argument("VersionSuffix", ""),
        TargetFramework = ctx.Argument("TargetFramework", DefaultTargetFramework),
    };
});

Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    ;

Task("Restore")
    .Does(() =>
    {
        DotNetRestore("dotnet-compressor.slnproj");
    });
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var setting = new DotNetBuildSettings()
        {
            Configuration = Configuration,
            VersionSuffix = VersionSuffix,
        };
        DotNetBuild("dotnet-compressor.slnproj", setting);
    });
Task("Test")
    .IsDependentOn("SlnGen")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetTest("dotnet-compressor.sln");
    });
Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        if(!IsRelease && string.IsNullOrEmpty(VersionSuffix))
        {
            VersionSuffix = "alpha-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        var setting = new DotNetPackSettings()
        {
            Configuration = Configuration,
            NoBuild = true,
            OutputDirectory = $"dist/{Configuration}/nupkg",
            VersionSuffix = VersionSuffix,
        };
        if(!string.IsNullOrEmpty(Runtime))
        {
            setting.Runtime = Runtime;
        }
        DotNetPack("src/dotnet-compressor/dotnet-compressor.csproj", setting);
    });
Task("Publish")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var setting = new DotNetPublishSettings()
        {
            Configuration = Configuration,
            Runtime = Runtime,
            OutputDirectory = Directory("dist").Path.Combine(Configuration).Combine(Runtime)
        };
        DotNetPublish(File("src/dotnet-compressor/dotnet-compressor.csproj"), setting);
    });
Task("SlnGen")
    .Does(() =>
    {
        var msbuildSetting = new MSBuildSettings()
        {
            Verbosity = Verbosity.Normal
        };
        msbuildSetting = msbuildSetting.WithTarget("SlnGen");
        MSBuild("dotnet-compressor.slngenproj", msbuildSetting);
    });

RunTarget(Target);