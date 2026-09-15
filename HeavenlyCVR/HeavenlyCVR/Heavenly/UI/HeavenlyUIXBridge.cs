using System;
using System.Collections.Generic;
using UIExpansionKit.API;
using UIExpansionKit.API.Layout;

namespace HeavenlyCVR.Heavenly.UI;

public static class HeavenlyUIXBridge
{
    private static ICustomLayoutedMenu<TableLayout.Param>? _settingsMenu;

    private static readonly HashSet<API.HeavenlyPage> AddedPages = new();

    public static void Initialize()
    {
        try
        {
            Heavenly.API.HeavenlyAPI.Log(
                "Initializing UI Expansion Kit bridge..."
            );

            _settingsMenu =
                ExpansionKitApi.GetSettingsCategory("HeavenlyCVR");

            Heavenly.API.HeavenlyAPI.Log(
                "Created HeavenlyCVR settings category."
            );

            BuildFromHeavenlyPages();

            Heavenly.API.HeavenlyAPI.Log(
                "UI Expansion Kit bridge initialized."
            );
        }
        catch (Exception ex)
        {
            Heavenly.API.HeavenlyAPI.Error(
                $"UIX bridge initialization failed: {ex}"
            );
        }
    }

    public static void BuildFromHeavenlyPages()
    {
        if (_settingsMenu == null)
        {
            Heavenly.API.HeavenlyAPI.Warning(
                "Cannot build UI because the UIX settings menu is not initialized."
            );

            return;
        }

        foreach (API.HeavenlyPage page in HeavenlyUI.Pages)
        {
            AddPage(page);
        }
    }

    public static void AddPage(API.HeavenlyPage page)
    {
        if (_settingsMenu == null)
        {
            Heavenly.API.HeavenlyAPI.Warning(
                $"Cannot add page '{page.Name}' because UIX is not initialized."
            );

            return;
        }

        if (!AddedPages.Add(page))
        {
            Heavenly.API.HeavenlyAPI.Warning(
                $"Page '{page.Name}' is already registered with UIX."
            );

            return;
        }

        _settingsMenu.AddLabel(
            $"── {page.Name} ──"
        );

        foreach (API.HeavenlyLabel label in page.Labels)
        {
            API.HeavenlyLabel captured = label;
            _settingsMenu.AddLabel(captured.Text);
        }

        foreach (API.HeavenlyButton button in page.Buttons)
        {
            AddButton(button);
        }

        foreach (API.HeavenlyToggle toggle in page.Toggles)
        {
            AddToggle(toggle);
        }

        // UIX settings menus have no slider control: point at the QuickMenu tab.
        if (page.Sliders.Count > 0)
        {
            _settingsMenu.AddLabel(
                $"{page.Sliders.Count} slider(s) on this page live in the Heavenly QuickMenu tab."
            );
        }

        foreach (API.HeavenlyPage sub in page.SubPages)
        {
            AddPage(sub);
        }

        Heavenly.API.HeavenlyAPI.Log(
            $"UIX: Added Heavenly page '{page.Name}' " +
            $"({page.Buttons.Count} button(s), {page.Toggles.Count} toggle(s), " +
            $"{page.Labels.Count} label(s))."
        );
    }

    private static void AddButton(API.HeavenlyButton button)
    {
        if (_settingsMenu == null)
            return;

        _settingsMenu.AddSimpleButton(
            button.Text,
            () =>
            {
                Heavenly.API.HeavenlyAPI.Log(
                    $"UIX button pressed: '{button.Text}'"
                );

                button.Click();
            }
        );
    }

    private static void AddToggle(API.HeavenlyToggle toggle)
    {
        if (_settingsMenu == null)
            return;

        API.HeavenlyToggle captured = toggle;

        _settingsMenu.AddToggleButton(
            captured.Text,
            state =>
            {
                Heavenly.API.HeavenlyAPI.Log(
                    $"UIX toggle '{captured.Text}' -> {(state ? "ON" : "OFF")}"
                );

                captured.Set(state);
            },
            () => captured.Value
        );
    }
}
