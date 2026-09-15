using System;
using System.Collections.Generic;

namespace HeavenlyCVR.Heavenly.UI;

public static class HeavenlyUI
{
    private static readonly List<API.HeavenlyPage> _pages = new();

    public static IReadOnlyList<API.HeavenlyPage> Pages => _pages;

    public static API.HeavenlyPage? CurrentPage { get; private set; }

    public static API.HeavenlyPage CreatePage(string name, string tooltip = "")
    {
        API.HeavenlyPage page = new(name, tooltip);

        _pages.Add(page);

        if (CurrentPage == null)
            CurrentPage = page;

        API.HeavenlyAPI.Log(
            $"Created page: '{name}'"
        );

        return page;
    }

    public static void OpenPage(API.HeavenlyPage page)
    {
        if (!_pages.Contains(page))
        {
            API.HeavenlyAPI.Warning(
                $"Cannot open unregistered page: '{page.Name}'"
            );

            return;
        }

        CurrentPage = page;

        API.HeavenlyAPI.Log(
            $"Opened page: '{page.Name}'"
        );
    }

    public static void OpenPage(string name)
    {
        API.HeavenlyPage? page = _pages.Find(
            p => string.Equals(
                p.Name,
                name,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (page == null)
        {
            API.HeavenlyAPI.Warning(
                $"Page not found: '{name}'"
            );

            return;
        }

        OpenPage(page);
    }

    public static void RemovePage(API.HeavenlyPage page)
    {
        if (!_pages.Remove(page))
            return;

        if (CurrentPage == page)
            CurrentPage = _pages.Count > 0
                ? _pages[0]
                : null;

        API.HeavenlyAPI.Log(
            $"Removed page: '{page.Name}'"
        );
    }

    public static void Clear()
    {
        _pages.Clear();
        CurrentPage = null;

        API.HeavenlyAPI.Log(
            "Cleared all Heavenly pages."
        );
    }
}
