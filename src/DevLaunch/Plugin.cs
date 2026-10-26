using System;
using BepInEx;
using BepInEx.Logging;

namespace DevLaunch;

[BepInPlugin(MyPluginInfo.Id, MyPluginInfo.Name, MyPluginInfo.Version)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    internal static Launch Launch { get; private set; } = new();

    private void Awake()
    {
        Log = Logger;
        Badge.Bind(Config);
        Launch = Launch.From(Environment.GetCommandLineArgs());
        Log.LogInfo($"{MyPluginInfo.Name} {MyPluginInfo.Version} loaded");
        if (Launch.Online == null && !Launch.Host && Launch.JoinAddress == null && Launch.Tile == null)
        {
            return;
        }
        Splash.Stop();
        Menus.Hook();
        Badge.Show();
    }
}

internal sealed class Launch
{
    public bool? Online { get; private set; }

    public bool Host { get; private set; }

    public string? JoinAddress { get; private set; }

    public (int Index, int Count)? Tile { get; private set; }

    public static Launch From(string[] args)
    {
        var launch = new Launch();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--online":
                    launch.Online = true;
                    break;
                case "--lan":
                    launch.Online = false;
                    break;
                case "--host":
                    launch.Host = true;
                    break;
                case "--join" when i + 1 < args.Length:
                    launch.JoinAddress = args[++i];
                    break;
                case "--tile" when i + 1 < args.Length:
                    string[] parts = args[++i].Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int n) && int.TryParse(parts[1], out int count) &&
                        count > 0 && n >= 1 && n <= count)
                    {
                        launch.Tile = (n - 1, count);
                    }
                    break;
            }
        }
        return launch;
    }
}
