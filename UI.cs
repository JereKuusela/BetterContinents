using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BetterContinents;

public static class UI
{
    private static readonly Dictionary<string, Action> UICallbacks = [];

    private static readonly Color ValheimColor = new(1, 0.714f, 0.361f, 1);
#nullable disable
    private static Texture BorderTexture;
    private static Texture FrontTexture;
    private static Texture BackTexture;
    private static GUIStyle BigTextStyle;
    private static GUIStyle NormalTextStyle;
#nullable enable

    private static bool WindowVisible;

    private const int Spacing = 10;
    private const int ButtonHeight = 30;
    private const int ButtonWidth = 150;

    public static void Init()
    {
        // Always reset the UI callbacks on scene change
        SceneManager.activeSceneChanged += (_, __) =>
        {
            ColorTextures.Clear();
            UICallbacks.Clear();

            // Only need these on the client
            BorderTexture = CreateFillTexture(Color.Lerp(ValheimColor, Color.white, 0.25f));
            FrontTexture = CreateFillTexture(Color.Lerp(ValheimColor, Color.black, 0.5f));
            BackTexture = CreateFillTexture(Color.Lerp(ValheimColor, Color.black, 0.85f));
            BigTextStyle = null; // We are "resetting" this in-case it got invalidated. We can only actually create it in a GUI function
            NormalTextStyle = null;

            Add("Debug Mode", () =>
            {
                if (BetterContinents.AllowDebugActions)
                {
                    Text("Better Continents Debug Mode Enabled!", 10, 10, Color.red);

                    if (Menu.IsVisible() || Minimap.instance.m_mode == Minimap.MapMode.Large)
                    {
                        DoDebugMenu();
                    }

                    if (WindowVisible)
                    {
                        DebugUtils.Command.CmdUI.DrawSettingsWindow();
                    }
                }
            });

            Add("Active Hint", () =>
            {
                if (Menu.IsVisible() || Game.instance && Game.instance.WaitingForRespawn())
                {
                    if (BetterContinents.Settings.EnabledForThisWorld)
                    {
                        DisplayMessage($"<color=gray><size=20><b>{ModInfo.Name} v{ModInfo.Version}</b>: <color=green>ENABLED</color> for this world</size></color>");
                    }
                    else
                    {
                        DisplayMessage($"<color=gray><size=20><b>{ModInfo.Name} v{ModInfo.Version}</b>: <color=#505050ff>DISABLED</color> for this world</size></color>");
                    }
                }
            });
        };
    }

    private static void DoDebugMenu()
    {
        if (Event.current.type is EventType.KeyUp
            && Event.current.modifiers is (EventModifiers.Alt | EventModifiers.FunctionKey)
            && Event.current.keyCode is KeyCode.F8)
        {
            WindowVisible = !WindowVisible;
            Event.current.Use();
        }
        if (Button("Better Continents", Spacing, 150))
        {
            WindowVisible = !WindowVisible;
        }
    }
    public static void CloseDebugMenu()
    {
        WindowVisible = false;
    }


    public static void OnGUI()
    {
        foreach (var callback in UICallbacks.Values.ToList())
        {
            callback();
        }
    }

    public static void Add(string key, Action action) => UICallbacks[key] = action;

    public static bool Exists(string key) => UICallbacks.ContainsKey(key);

    public static void Remove(string key) => UICallbacks.Remove(key);

    private static readonly Dictionary<Color, Texture2D> ColorTextures = [];
    public static Texture2D CreateFillTexture(Color color)
    {
        if (ColorTextures.TryGetValue(color, out var texture) && texture != null)
            return texture;
        texture = new Texture2D(1, 1);
        texture.SetPixels([color]);
        texture.Apply(false);
        ColorTextures[color] = texture;
        return texture;
    }

    public static void ProgressBar(int percent, string text)
    {
        CreateTextStyle();

        int yOffs = Screen.height - 75;
        GUI.DrawTexture(new Rect(50 - 4, yOffs - 4, Screen.width - 100 + 8, 50 + 8), BorderTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(50, yOffs, Screen.width - 100, 50), BackTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(50, yOffs, (Screen.width - 100) * percent / 100f, 50), FrontTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(75, yOffs, Screen.width - 50, 50), text, BigTextStyle);
    }

    public static void DisplayMessage(string msg)
    {
        CreateTextStyle();
        int yOffs = Screen.height - 75;
        GUI.Label(new Rect(75, yOffs, Screen.width - 50, 50), msg, BigTextStyle);
    }

    public static void Text(string msg, int x, int y) => Text(msg, x, y, ValheimColor);

    public static void Text(string msg, int x, int y, Color color)
    {
        CreateTextStyle();
        NormalTextStyle.normal.textColor = color;
        GUI.Label(new Rect(x, y, Screen.width - 50, 50), msg, NormalTextStyle);
    }

    public static bool Button(string label, int x, int y)
    {
        return GUI.Button(new Rect(x, y, ButtonWidth, ButtonHeight), label);
    }

    private static void CreateTextStyle()
    {
        if (BigTextStyle != null)
        {
            return;
        }

        BigTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
        BigTextStyle.font = Resources.FindObjectsOfTypeAll<Text>()
            .Select(t => t.font)
            .FirstOrDefault(f => f.name == "AveriaSerifLibre-Bold") ?? BigTextStyle.font;
        ;
        // Trying to assign alignment crashes with method not found exception
        // BigTextStyle.alignment = TextAnchor.MiddleCenter;
        BigTextStyle.normal.textColor = Color.Lerp(ValheimColor, Color.white, 0.75f);

        NormalTextStyle = new GUIStyle(BigTextStyle) { fontSize = 20, fontStyle = FontStyle.Normal };
    }
}
