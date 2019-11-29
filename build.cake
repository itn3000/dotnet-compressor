#load "nativetest.cake"
#load "nuget.cake"

var Configuration = Argument("Configuration", "Debug");
var Target = Argument("Target", "Default");
var Runtime = Argument("Runtime", "");
var VersionSuffix = Argument("VersionSuffix", "");
var IsRelease = HasArgument("IsRelease");


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
            VersionSuffix = VersionSuffix,
        };
        DotNetCoreBuild("dotnet-compressor.slnproj", setting);
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        if(!IsRelease && string.IsNullOrEmpty(VersionSuffix))
        {
            VersionSuffix = "alpha-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        var setting = new DotNetCorePackSettings()
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
        DotNetCorePack("src/dotnet-compressor/dotnet-compressor.csproj", setting);
    });
Task("Native")
    .IsDependentOn("Native.Build")
    .IsDependentOn("Native.Test")
    ;
Task("Native.Test")
    .IsDependentOn("Native.Build")
    .ReportError(e =>
    {
        Error($"test failed: {e}");
    });
Task("Native.Test.Zip")
    .IsDependeeOf("Native.Test")
    .Does(() =>
    {
        TestNativeZip(Configuration, Runtime);
    });
Task("Native.Test.Tar")
    .IsDependeeOf("Native.Test")
    .Does(() => TestNativeTar(Configuration, Runtime))
    ;
Task("Native.Build")
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
            .WithProperty("RuntimeIdentifier", Runtime)
            ;
        if(!string.IsNullOrEmpty(VersionSuffix))
        {
            msBuildSetting = msBuildSetting.WithProperty("VersionSuffix", VersionSuffix);
        }
        var setting = new DotNetCorePublishSettings()
        {
            Configuration = Configuration,
            MSBuildSettings = msBuildSetting,
        };
        DotNetCorePublish("src/dotnet-compressor/dotnet-compressor.csproj", setting);
        var distbindir = Directory("dist").Path.Combine(Configuration).Combine("bin");
        if(!DirectoryExists(distbindir))
        {
            CreateDirectory(distbindir);
        }
        foreach(var f in GetFiles(Directory("src").Path
            .Combine("dotnet-compressor")
            .Combine("bin")
            .Combine(Configuration)
            .Combine("netcoreapp2.1")
            .Combine(Runtime)
            .Combine("native").CombineWithFilePath("*").ToString()))
        {
            var destfile = distbindir.CombineWithFilePath($"{f.GetFilenameWithoutExtension()}-{Runtime}{f.GetExtension()}");
            CopyFile(f, destfile);
        }
    });

Task("NuGet")
    .IsDependentOn("Pack")
    .Does(() =>
    {
    });

RunTarget(Target);