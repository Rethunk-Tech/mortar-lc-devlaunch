using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DevLaunch;

// Ends Unity's splash screen. Plugins load while it's still up, and no script frame runs until the first scene is in,
// so a thread keeps asking it to stop until then. Test copies open on top of the first one before they move to their
// cell, so a splash left running would play over the host's window.
internal static class Splash
{
    private static readonly TimeSpan wait = TimeSpan.FromSeconds(30);
    private static volatile bool sceneLoaded;

    public static void Stop()
    {
        SceneManager.sceneLoaded += (_, _) => sceneLoaded = true;
        new Thread(Ask) { IsBackground = true, Name = "DevLaunchSplash" }.Start();
    }

    private static void Ask()
    {
        var waited = Stopwatch.StartNew();
        while (!sceneLoaded && waited.Elapsed < wait)
        {
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            Thread.Sleep(10);
        }
    }
}
