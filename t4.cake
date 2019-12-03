Task("T4Gen")
    .Does(() =>
    {
        foreach(var f in GetFiles("**/*.tt"))
        {
            StartProcess("dotnet", $"tool run t4 -- {f}");
        }
    });