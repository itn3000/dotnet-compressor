Task("T4Gen")
    .Does(() =>
    {
        StartProcess("dotnet", $"tool restore");
        foreach(var f in GetFiles("**/*.tt"))
        {
            StartProcess("dotnet", $"tool run t4 -- {f}");
        }
    });