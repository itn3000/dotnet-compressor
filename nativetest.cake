FilePath GetNativeExePath(string configuration, string rid, string targetFramework)
{
    string exeExtension = "";
    if(Environment.OSVersion.Platform == PlatformID.Win32NT)
    {
        exeExtension = ".exe";
    }
    return Directory("dist").Path
        .Combine("native")
        .Combine(configuration)
        .Combine(rid)
        .CombineWithFilePath(File("dcomp") + exeExtension)
        ;
}

void TestNativeTar(string configuration, string rid, string targetFramework)
{
    var exePath = GetNativeExePath(configuration, rid, targetFramework);
    try
    {
        var settings = new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .Append("tar")
                .Append("c")
                .Append("-b").Append("testdir")
                .Append("-i").Append("test.txt")
                .Append("-o").Append("test.tar")
                
        };
        var exitCode = StartProcess(exePath, settings);
        if(exitCode != 0)
        {
            throw new Exception($"exit code not 0: {exitCode}");
        }
        if(!FileExists("test.tar"))
        {
            throw new Exception("creating test tar failed");
        }
        settings = new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .Append("tar")
                .Append("d")
                .Append("-o").Append("testdir")
                .Append("-i").Append("test.tar")
        };
        exitCode = StartProcess(exePath, settings);
        if(exitCode != 0)
        {
            throw new Exception($"exit code not 0: {exitCode}");
        }
    }
    finally
    {
        if(FileExists("test.tar"))
        {
            DeleteFile("test.tar");
        }
    }
}

void TestNativeZip(string configuration, string rid, string targetFramework)
{
    var exePath = GetNativeExePath(configuration, rid, targetFramework);
    var settings = new ProcessSettings()
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("zip")
            .Append("c")
            .Append("-b").Append("testdir")
            .Append("-i").Append("test.txt")
            .Append("-p").Append("pass")
            .Append("-o").Append("test.zip")
            .Append("--replace-from").Append("abc")
            .Append("--replace-to").Append("def")
    };
    try
    {
        var exitCode = StartProcess(exePath, settings);
        if(exitCode != 0)
        {
            throw new Exception($"native zip compression failed({exitCode})");
        }
        settings = new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .Append("zip")
                .Append("d")
                .Append("-i").Append("test.zip")
                .Append("-o").Append(".")
                .Append("-p").Append("pass")
                .Append("--replace-from").Append("abc")
                .Append("--replace-to").Append("def")
        };
        exitCode = StartProcess(exePath, settings);
        if(exitCode != 0)
        {
            throw new Exception($"native zip compression failed({exitCode})");
        }
    }
    finally
    {
        if(FileExists(File("test.zip")))
        {
            DeleteFile("test.zip");
        }
    }
}

class NativeTestContext
{
    public string Configuration;
    public string Runtime;
    public string VersionSuffix;
    public string TargetFramework;
}

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
    .Does<NativeTestContext>((ctx) =>
    {
        TestNativeZip(ctx.Configuration, ctx.Runtime, ctx.TargetFramework);
    });
Task("Native.Test.Tar")
    .IsDependeeOf("Native.Test")
    .Does<NativeTestContext>((ctx) => TestNativeTar(ctx.Configuration, ctx.Runtime, ctx.TargetFramework))
    ;
Task("Native.Build")
    .IsDependentOn("Build")
    .Does<NativeTestContext>((ctx) =>
    {
        if(string.IsNullOrEmpty(ctx.Runtime))
        {
            throw new Exception("you must input Runtime argument");
        }
        var props = new Dictionary<string, string[]>();
        props["WithCoreRT"] = new string[] { "true" };
        var msBuildSetting = new DotNetCoreMSBuildSettings()
            .WithProperty("WithCoreRT", "true")
            .WithProperty("RuntimeIdentifier", ctx.Runtime)
            .WithProperty("TargetFramework", ctx.TargetFramework)
            ;
        if(!string.IsNullOrEmpty(ctx.VersionSuffix))
        {
            msBuildSetting = msBuildSetting.WithProperty("VersionSuffix", ctx.VersionSuffix);
        }
        var distbindir = Directory("dist").Path.Combine("native").Combine(ctx.Configuration).Combine(ctx.Runtime);
        var setting = new DotNetCorePublishSettings()
        {
            Configuration = ctx.Configuration,
            MSBuildSettings = msBuildSetting,
            OutputDirectory = distbindir,
        };
        DotNetPublish("src/dotnet-compressor/dotnet-compressor.csproj", setting);
    });
