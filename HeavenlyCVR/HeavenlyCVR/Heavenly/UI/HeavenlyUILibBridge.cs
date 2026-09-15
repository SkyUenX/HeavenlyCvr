using System;
using System.Collections.Generic;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;

namespace HeavenlyCVR.Heavenly.UI;

/// <summary>
/// Renders HeavenlyAPI pages into ChilloutVR's built-in UILib (Cohtml QuickMenu).
/// This is the same backend BTKUILib uses, so the menu works with or without
/// the BTKUILib mod installed. BTKUILib mod is still recommended for extra tabs.
/// </summary>
public static class HeavenlyUILibBridge
{
    public const string ModName = "HeavenlyCVR";

    private static readonly Dictionary<API.HeavenlyPage, Page> PageMap = new();

    private static readonly Dictionary<API.HeavenlyToggle, ToggleButton> ToggleControls = new();

    private static readonly Dictionary<API.HeavenlyLabel, TextBlock> LabelControls = new();

    private static bool _pushingToggle;

    private static Page? _rootPage;
    private static bool _initialized;

    public static Page? RootPage => _rootPage;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            API.HeavenlyAPI.Log("Initializing UILib bridge (BTK-compatible)...");

            // User art first: tab + per-page icons from UserData (no rebuild).
            try { HeavenlyIcons.Initialize(); } catch { }

            // Root tab. Empty icon = default tab icon handling in UILib.
            try
            {
                _rootPage = Page.GetOrCreatePage(ModName, "Heavenly", true, HeavenlyIcons.TabIcon);
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"UILib bridge: root page failed: {ex.Message}");
                return;
            }

            try
            {
                _rootPage.MenuTitle = "HeavenlyCVR";
                _rootPage.MenuSubtitle = "Heavenly client for ChilloutVR";
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib bridge: titles failed: {ex.Message}");
            }

            // Render any pages that were already created via HeavenlyUI API.
            // Each page is independently guarded, so one bad page can't
            // prevent the rest from rendering.
            foreach (API.HeavenlyPage page in HeavenlyUI.Pages)
                AddPage(page);

            // Heavenly hex-button theme (dark glass + crimson + hexagons).
            HeavenlyStyle.Apply();

            API.HeavenlyAPI.Log("UILib bridge initialized. Root tab: Heavenly");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"UILib bridge initialization failed: {ex}");
        }
    }

    public static void AddPage(API.HeavenlyPage page)
    {
        try
        {
            if (_rootPage == null)
            {
                API.HeavenlyAPI.Warning(
                    $"Cannot add page '{page.Name}': UILib root not ready."
                );
                return;
            }

            if (PageMap.ContainsKey(page))
                return;

            // Each Heavenly page becomes a UILib sub-page under the root tab.
            Category rootCat = GetOrCreateRootCategory();

            Page sub = rootCat.AddPage(page.Name, HeavenlyIcons.PageIcon(page.Name), page.Tooltip, ModName);
            sub.MenuTitle = $"Heavenly - {page.Name}";
            sub.MenuSubtitle = page.Tooltip;

            PageMap[page] = sub;

            RenderPageContents(page, sub);

            API.HeavenlyAPI.Log($"UILib: Added Heavenly page '{page.Name}'.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"UILib: Failed to add page '{page.Name}': {ex}");
        }
    }

    private static Category? _rootCat;

    private static Category GetOrCreateRootCategory()
    {
        if (_rootCat == null && _rootPage != null)
            _rootCat = _rootPage.AddCategory("Heavenly Pages");

        return _rootCat!;
    }

    private static void RenderPageContents(API.HeavenlyPage heavenlyPage, Page uiPage)
    {
        Category main = uiPage.AddCategory($"{heavenlyPage.Name}");

        // Labels first (info text). Bound live: model changes push to UI.
        foreach (API.HeavenlyLabel label in heavenlyPage.Labels)
        {
            try
            {
                API.HeavenlyLabel captured = label;
                TextBlock block = main.AddTextBlock(captured.Text);
                try
                {
                    LabelControls[captured] = block;
                    captured.Changed += () =>
                    {
                        try
                        {
                            if (LabelControls.TryGetValue(captured, out TextBlock? bound) && bound != null)
                                bound.Text = captured.Text;
                        }
                        catch { }
                    };
                }
                catch { }
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib: label failed on '{heavenlyPage.Name}': {ex.Message}");
            }
        }

        // Buttons.
        foreach (API.HeavenlyButton button in heavenlyPage.Buttons)
        {
            try
            {
                API.HeavenlyButton captured = button;
                Button uiBtn = main.AddButton(
                    captured.Text,
                    "",
                    string.IsNullOrEmpty(captured.Tooltip) ? captured.Text : captured.Tooltip
                );
                uiBtn.OnPress += () =>
                {
                    try
                    {
                        captured.Click();
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Error($"Button '{captured.Text}' handler failed: {ex}");
                    }
                };
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib: button '{button.Text}' failed: {ex.Message}");
            }
        }

        // Toggles.
        foreach (API.HeavenlyToggle toggle in heavenlyPage.Toggles)
        {
            try
            {
                API.HeavenlyToggle captured = toggle;
                ToggleButton uiToggle = main.AddToggle(
                    captured.Text,
                    string.IsNullOrEmpty(captured.Tooltip) ? captured.Text : captured.Tooltip,
                    captured.DefaultValue
                );
                try { ToggleControls[captured] = uiToggle; } catch { }
                uiToggle.OnValueUpdated += state =>
                {
                    // Skip events caused by our own PushToggleState below.
                    if (_pushingToggle)
                        return;

                    try
                    {
                        captured.Set(state);
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Error($"Toggle '{captured.Text}' handler failed: {ex}");
                    }
                };
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib: toggle '{toggle.Text}' failed: {ex.Message}");
            }
        }

        // Sliders.
        foreach (API.HeavenlySlider slider in heavenlyPage.Sliders)
        {
            try
            {
                API.HeavenlySlider captured = slider;
                float initial = Clamp(
                    captured.Value,
                    captured.Min,
                    captured.Max
                );
                SliderFloat uiSlider = main.AddSlider(
                    captured.Text,
                    string.IsNullOrEmpty(captured.Tooltip) ? captured.Text : captured.Tooltip,
                    initial,
                    captured.Min,
                    captured.Max
                );
                uiSlider.OnValueUpdated += value =>
                {
                    try
                    {
                        captured.Set(value);
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Error($"Slider '{captured.Text}' handler failed: {ex}");
                    }
                };
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib: slider '{slider.Text}' failed: {ex.Message}");
            }
        }

        // Nested sub-pages (Tags slots).
        foreach (API.HeavenlyPage sub in heavenlyPage.SubPages)
        {
            try
            {
                Page nested = main.AddPage(sub.Name, HeavenlyIcons.PageIcon(sub.Name), sub.Tooltip, ModName);
                nested.MenuTitle = $"Heavenly - {sub.Name}";
                nested.MenuSubtitle = sub.Tooltip;
                RenderPageContents(sub, nested);
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"UILib: sub-page '{sub.Name}' failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Opens a Heavenly page in the QuickMenu (syncs the model + opens real UI).
    /// Unlike <see cref="HeavenlyUI.OpenPage(string)"/> this visibly navigates.
    /// </summary>
    public static void OpenPage(string name)
    {
        HeavenlyUI.OpenPage(name);

        try
        {
            foreach (var kv in PageMap)
            {
                if (!string.Equals(kv.Key.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                try { kv.Value.OpenPage(); }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"UILib: open page '{name}' failed: {ex.Message}");
                }
                return;
            }

            API.HeavenlyAPI.Warning($"UILib: no rendered page for '{name}' yet.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"UILib: OpenPage('{name}') failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Mirrors a model change into the visible UILib toggle, if rendered.
    /// </summary>
    public static void PushToggleState(API.HeavenlyToggle toggle)
    {
        try
        {
            if (ToggleControls.TryGetValue(toggle, out ToggleButton? control) && control != null)
            {
                try
                {
                    _pushingToggle = true;
                    control.ToggleValue = toggle.Value;
                }
                finally
                {
                    _pushingToggle = false;
                }
            }
        }
        catch { }
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
