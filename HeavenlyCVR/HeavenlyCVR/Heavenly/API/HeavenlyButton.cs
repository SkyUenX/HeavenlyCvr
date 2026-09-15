using System;

namespace HeavenlyCVR.Heavenly.API;

public class HeavenlyButton
{
    public string Text { get; }

    public string Tooltip { get; }

    public Action? OnClick { get; }

    public HeavenlyButton(string text, Action? onClick = null, string tooltip = "")
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Button text cannot be empty.",
                nameof(text)
            );

        Text = text;
        Tooltip = tooltip;
        OnClick = onClick;
    }

    public void Click()
    {
        HeavenlyAPI.Log($"Button clicked: '{Text}'");

        OnClick?.Invoke();
    }
}
