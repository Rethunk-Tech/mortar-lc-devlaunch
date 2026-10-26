using System;
using System.Collections;
using UnityEngine;

namespace DevLaunch;

// The game reapplies its screen mode while it loads settings, so Menus calls Apply again afterwards.
internal sealed class Tile : MonoBehaviour
{
    // Roughly the height of the title bar the window manager adds outside the game's size. If yours is taller, the
    // window overlaps the next row a little.
    private const int TitleBar = 40;

    private static Tile? instance;

    public static void Add(GameObject holder) => holder.AddComponent<Tile>();

    public static void Apply()
    {
        if (instance != null && Plugin.Launch.Tile is { } tile)
        {
            instance.StopAllCoroutines();
            instance.StartCoroutine(Place(tile.Index, tile.Count));
        }
    }

    private void Awake()
    {
        instance = this;
        Apply();
    }

    private static IEnumerator Place(int index, int count)
    {
        DisplayInfo display = Screen.mainWindowDisplayInfo;
        RectInt area = display.workArea.width > 0 ? display.workArea : new RectInt(0, 0, display.width, display.height);
        int columns = (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (int)Math.Ceiling(count / (double)columns);
        int width = area.width / columns;
        int height = area.height / rows;
        Screen.SetResolution(width, height - TitleBar, FullScreenMode.Windowed);
        // The resize lands next frame. Moving the window before that can get undone.
        yield return null;
        var at = new Vector2Int(area.x + index % columns * width, area.y + index / columns * height);
        Screen.MoveMainWindowTo(display, at);
        Plugin.Log.LogInfo($"Window {index + 1} of {count}: {width}x{height - TitleBar} at {at.x},{at.y} on {display.name}");
    }
}
