using System;
using System.Collections.Generic;
using System.IO;

namespace HeavenlyCVR.Heavenly.UI;

/// <summary>
/// User-supplied art for the Heavenly tab and pages. Drop 256x256 PNGs into
/// ChilloutVR/UserData/HeavenlyCVR/Icons/ (created on first run):
///   heavenly.png  -> the QuickMenu tab icon (your logo goes here)
///   main.png, world.png, avatar.png, ... -> per-page icons
/// No rebuild needed: files are picked up at startup. Missing files simply
/// fall back to CVR defaults (and are logged so you know what's absent).
/// Button icons stay hidden by the hex theme on purpose: UILib renders a
/// placeholder glyph for empty icon names, so showing them would put grey
/// X marks on every button without art.
/// </summary>
public static class HeavenlyIcons
{
    public const string FolderName = "Icons";

    private static readonly Dictionary<string, string> PageIcons = new(StringComparer.OrdinalIgnoreCase);

    private static string? _tabIcon;
    private static bool _initialized;

    public static string TabIcon => _tabIcon ?? "";

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        string folder;
        try
        {
            folder = IconsFolder();
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Icons: folder unavailable: {ex.Message}");
            return;
        }

        _tabIcon = TryPrepare(folder, "heavenly", "tab icon");
        if (_tabIcon == null)
            _tabIcon = TryPrepare(folder, "tab", "tab icon");

        foreach (API.HeavenlyPage page in HeavenlyUI.Pages)
            EnsurePageIcon(folder, page.Name);

        API.HeavenlyAPI.Log(
            _tabIcon != null
                ? $"Icons: tab art '{_tabIcon}.png' loaded."
                : "Icons: no heavenly.png/tab.png found; using default tab art. " +
                  $"Drop a 256x256 PNG in {folder}"
        );
    }

    public static string PageIcon(string pageName)
    {
        try
        {
            if (PageIcons.TryGetValue(pageName, out string? icon) && !string.IsNullOrEmpty(icon))
                return icon;
        }
        catch { }
        return "";
    }

    private static void EnsurePageIcon(string folder, string pageName)
    {
        try
        {
            if (PageIcons.ContainsKey(pageName))
                return;

            string? prepared = TryPrepare(folder, pageName.ToLowerInvariant(), $"page '{pageName}' icon");
            if (prepared != null)
                PageIcons[pageName] = prepared;
        }
        catch { }
    }

    private static string IconsFolder()
    {
        string userData;
        try
        {
            userData = Path.Combine(
                Path.GetDirectoryName(typeof(HeavenlyIcons).Assembly.Location) ?? ".",
                "..", "UserData", "HeavenlyCVR", FolderName);
        }
        catch
        {
            userData = Path.Combine("UserData", "HeavenlyCVR", FolderName);
        }

        try { return Path.GetFullPath(userData); }
        catch { return userData; }
    }

    private static string? TryPrepare(string folder, string iconName, string what)
    {
        string path;
        try
        {
            path = Path.Combine(folder, iconName + ".png");
            if (!File.Exists(path))
                return null;
        }
        catch
        {
            return null;
        }

        try
        {
            // Deliberately not disposed: UILib copies the art for Cohtml and
            // may read lazily; the OS handle is reclaimed by the finalizer.
            FileStream stream = File.OpenRead(path);
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon(
                HeavenlyUILibBridge.ModName, iconName, stream);
            API.HeavenlyAPI.Log($"Icons: {what} '{iconName}.png' prepared.");
            return iconName;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Icons: '{iconName}.png' failed: {ex.Message}");
            return null;
        }
    }
}
