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
        if (Launch.Online == null && !Launch.Host && Launch.JoinAddress == null)
        {
            return;
        }
        Splash.Stop();
        Menus.Hook();
        Badge.Show();
    }
}

// Command line: --online or --lan picks the launch mode, --host hosts a lobby and --join <address> joins a LAN game.
internal sealed class Launch
{
    public bool? Online { get; private set; }

    public bool Host { get; private set; }

    public string? JoinAddress { get; private set; }

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
            }
        }
        return launch;
    }
}
