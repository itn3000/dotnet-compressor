FilePath GetNativeExePath(string configuration, string rid)
{
    string exeExtension = "";
    if(Environment.OSVersion.Platform == PlatformID.Win32NT)
    {
        exeExtension = ".exe";
    }
    return Directory("src").Path
        .Combine("dotnet-compressor")
        .Combine("bin").Combine(configuration).Combine("netcoreapp3.1").Combine(rid).Combine("native")
        .CombineWithFilePath(File("dcomp") + exeExtension)
        ;
}

void TestNativeTar(string configuration, string rid)
{
    var exePath = GetNativeExePath(configuration, rid);
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

void TestNativeZip(string configuration, string rid)
{
    var exePath = GetNativeExePath(configuration, rid);
    var settings = new ProcessSettings()
    {
        Arguments = new ProcessArgumentBuilder()
            .Append("zip")
            .Append("c")
            .Append("-b").Append("testdir")
            .Append("-i").Append("test.txt")
            .Append("-p").Append("pass")
            .Append("-o").Append("test.zip")
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
        TestNativeZip(ctx.Configuration, ctx.Runtime);
    });
Task("Native.Test.Tar")
    .IsDependeeOf("Native.Test")
    .Does<NativeTestContext>((ctx) => TestNativeTar(ctx.Configuration, ctx.Runtime))
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
            ;
        if(!string.IsNullOrEmpty(ctx.VersionSuffix))
        {
            msBuildSetting = msBuildSetting.WithProperty("VersionSuffix", ctx.VersionSuffix);
        }
        var setting = new DotNetCorePublishSettings()
        {
            Configuration = ctx.Configuration,
            MSBuildSettings = msBuildSetting,
        };
        DotNetCorePublish("src/dotnet-compressor/dotnet-compressor.csproj", setting);
        var distbindir = Directory("dist").Path.Combine(ctx.Configuration).Combine("bin");
        if(!DirectoryExists(distbindir))
        {
            CreateDirectory(distbindir);
        }
        foreach(var f in GetFiles(Directory("src").Path
            .Combine("dotnet-compressor")
            .Combine("bin")
            .Combine(ctx.Configuration)
            .Combine("netcoreapp3.1")
            .Combine(ctx.Runtime)
            .Combine("native").CombineWithFilePath("*").ToString()))
        {
            var destfile = distbindir.CombineWithFilePath($"{f.GetFilenameWithoutExtension()}-{ctx.Runtime}{f.GetExtension()}");
            CopyFile(f, destfile);
        }
    });
