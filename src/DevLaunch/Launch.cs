using System;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace DevLaunch;

internal static class Menus
{
    private const int JoinAttempts = 10;

    private static Hook? launchModeHook;
    private static Hook? menuHook;
    private static Hook? bootHook;

    // Only auto-host or join once. Quitting back to the menu shouldn't start another game.
    private static bool started;

    public static void Hook()
    {
        // Wait for the launch panels. Picking a mode before the game finishes reading its settings throws.
        launchModeHook = Detour(typeof(PreInitSceneScript).GetMethod(nameof(PreInitSceneScript.SetLaunchPanelsEnabled)), PickLaunchMode);
        // Start is private in the game. The publicized reference only makes it look public.
        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        menuHook = Detour(typeof(MenuManager).GetMethod(nameof(MenuManager.Start), hidden), StartGame);
        // After the launch mode, the game plays its logo animation and sometimes a cold-open cinematic before the menu.
        bootHook = Detour(typeof(InitializeGame).GetMethod("Awake", hidden), SkipBootUp);
    }

    private static void SkipBootUp(Action<InitializeGame> orig, InitializeGame self)
    {
        orig(self);
        self.runBootUpScreen = false;
        self.playColdOpenCinematic = false;
        self.playColdOpenCinematic2 = false;
    }

    // A game update can rename what DevLaunch hooks. Skip that hook and log it rather than fail to load.
    private static Hook? Detour(MethodInfo? method, Delegate to)
    {
        if (method == null)
        {
            Plugin.Log.LogError($"Couldn't find the game method that {to.Method.Name} hooks. A game update may have changed it, so that part is off");
            return null;
        }
        return new Hook(method, to);
    }

    private static void PickLaunchMode(Action<PreInitSceneScript> orig, PreInitSceneScript self)
    {
        if (Plugin.Launch.Online is bool online)
        {
            self.ChooseLaunchOption(online);
            return;
        }
        orig(self);
    }

    private static void StartGame(Action<MenuManager> orig, MenuManager self)
    {
        orig(self);
        // The game shows its LAN warning every time the menu loads. --lan already answered it.
        if (Plugin.Launch.Online == false && !self.isInitScene)
        {
            self.lanWarningContainer.SetActive(false);
        }
        if (started || self.isInitScene)
        {
            return;
        }
        started = true;
        if (Plugin.Launch.Host)
        {
            // Show the loading screen right away so the menu never flashes.
            self.SetLoadingScreen(isLoading: true);
            self.StartCoroutine(Host(self));
        }
        else if (Plugin.Launch.JoinAddress is string address)
        {
            if (!GameNetworkManager.Instance.disableSteam)
            {
                Plugin.Log.LogWarning("--join needs LAN mode; online games are joined through Steam");
                return;
            }
            self.SetLoadingScreen(isLoading: true);
            self.StartCoroutine(Join(self, address));
        }
    }

    private static IEnumerator Host(MenuManager menu)
    {
        yield return null;
        Plugin.Log.LogInfo("Hosting a game");
        menu.ClickHostButton();
        menu.ConfirmHostButton();
    }

    // The host may still be starting, and a client gives up after about 10 seconds without a server, so retry.
    private static IEnumerator Join(MenuManager menu, string address)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = address;
        for (int attempt = 1; attempt <= JoinAttempts; attempt++)
        {
            yield return new WaitForSeconds(2f);
            Plugin.Log.LogInfo($"Joining {address}, attempt {attempt}");
            // A failed attempt brings back the menu and an error box. Hide them before trying again.
            menu.menuNotification.SetActive(false);
            menu.StartAClient();
            menu.SetLoadingScreen(isLoading: true);
            float giveUp = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < giveUp)
            {
                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    yield break;
                }
                yield return null;
            }
            NetworkManager.Singleton.Shutdown();
        }
        Plugin.Log.LogWarning($"No game to join at {address}");
        menu.SetLoadingScreen(isLoading: false);
    }
}
