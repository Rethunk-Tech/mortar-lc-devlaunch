using BepInEx.Configuration;
using Unity.Netcode;
using UnityEngine;

namespace DevLaunch;

internal enum Corner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

internal sealed class Badge : MonoBehaviour
{
    private static ConfigEntry<bool> inTitle = null!;
    private static ConfigEntry<bool> inHud = null!;
    private static ConfigEntry<Corner> corner = null!;
    private static ConfigEntry<string> color = null!;
    private static ConfigEntry<float> size = null!;

    private GUIStyle? style;
    private string shown = "";
    // The host is -1 and a copy that isn't connected is -2; a connected client is its id. The host has id 0, so the
    // first client to join is Client 2, counting the host as the first player.
    private long state = long.MinValue;

    public static void Bind(ConfigFile config)
    {
        inTitle = config.Bind("Label", "Show in title", true, "Put which copy this is (Host, Client 2, ...) in the window title.");
        inHud = config.Bind("Label", "Show in game HUD", true, "Draw which copy this is over the game.");
        corner = config.Bind("Label", "Corner in HUD", Corner.TopLeft, "Which corner of the window to draw the name in.");
        // A hex string rather than a Color: LethalConfig has no editor for Color settings and leaves them out.
        color = config.Bind("Label", "Text color", "#FFFFFF", "The colour of the name drawn over the game, as a hex colour like #FFFFFF.");
        size = config.Bind(
            "Label",
            "Text size",
            1f,
            new ConfigDescription(
                "The size of the name drawn over the game, relative to the window. At 1 it's a thirtieth of the window's height.",
                new AcceptableValueRange<float>(0.5f, 4f)));
    }

    public static void Show()
    {
        var holder = new GameObject(nameof(DevLaunch)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(holder);
        holder.AddComponent<Badge>();
        Tile.Add(holder);
    }

    private void Awake() => inTitle.SettingChanged += (_, _) => state = long.MinValue;

    private void Update()
    {
        NetworkManager? network = NetworkManager.Singleton;
        long now = network != null && network.IsHost ? -1
            : network != null && network.IsConnectedClient ? (long)network.LocalClientId
            : -2;
        if (now == state)
        {
            return;
        }
        state = now;
        shown = now == -1 ? "Host" : now >= 0 ? $"Client {now + 1}" : Plugin.Launch.Host ? "Host" : "Client";
        Plugin.Log.LogInfo($"This copy: {shown}");
        WindowTitle.Set(inTitle.Value ? $"Lethal Company - {shown}" : "Lethal Company");
    }

    private static Color TextColor()
    {
        string hex = color.Value.Trim();
        return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color parsed) ? parsed : Color.white;
    }

    private void OnGUI()
    {
        if (!inHud.Value)
        {
            return;
        }
        // OnGUI sizes are in pixels, so scale with the window or the text is tiny at high resolutions.
        int px = Mathf.Max(12, (int)(Screen.height / 30f * size.Value));
        style ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        style.fontSize = px;
        style.normal.textColor = TextColor();
        bool left = corner.Value is Corner.TopLeft or Corner.BottomLeft;
        bool top = corner.Value is Corner.TopLeft or Corner.TopRight;
        style.alignment = top
            ? left ? TextAnchor.UpperLeft : TextAnchor.UpperRight
            : left ? TextAnchor.LowerLeft : TextAnchor.LowerRight;
        float width = px * 12;
        float height = px * 2;
        float x = left ? px : Screen.width - px - width;
        float y = top ? px / 2f : Screen.height - px / 2f - height;
        GUI.Label(new Rect(x, y, width, height), shown, style);
    }
}
