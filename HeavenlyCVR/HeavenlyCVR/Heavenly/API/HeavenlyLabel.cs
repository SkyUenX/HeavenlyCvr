using System;

namespace HeavenlyCVR.Heavenly.API;

public class HeavenlyLabel
{
    public string Text { get; private set; }

    /// <summary>
    /// Fired when the text changes. Handlers run on the caller's thread:
    /// only call <see cref="SetText"/> from the main thread (service
    /// OnUpdate ticks) when the label is rendered in the menu.
    /// </summary>
    public event Action? Changed;

    public HeavenlyLabel(string text)
    {
        Text = text ?? string.Empty;
    }

    public void SetText(string text)
    {
        Text = text ?? string.Empty;
        try { Changed?.Invoke(); } catch { }
    }
}
