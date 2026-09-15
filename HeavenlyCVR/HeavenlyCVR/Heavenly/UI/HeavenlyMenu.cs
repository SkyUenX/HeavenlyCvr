using System;
using HeavenlyCVR.Heavenly.API;

namespace HeavenlyCVR.Heavenly.UI;

public class HeavenlyMenu
{
    public string Title { get; }

    public bool IsOpen { get; private set; }

    public HeavenlyMenu(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "Menu title cannot be empty.",
                nameof(title)
            );

        Title = title;
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;

        HeavenlyAPI.Log(
            $"Menu opened: '{Title}'"
        );
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        HeavenlyAPI.Log(
            $"Menu closed: '{Title}'"
        );
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }
}
