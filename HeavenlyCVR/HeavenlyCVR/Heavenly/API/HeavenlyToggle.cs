using System;

namespace HeavenlyCVR.Heavenly.API;

public class HeavenlyToggle
{
    public string Text { get; }

    public string Tooltip { get; }

    public bool DefaultValue { get; }

    public bool Value { get; private set; }

    public Action<bool>? OnChanged { get; }

    public HeavenlyToggle(string text, bool defaultValue = false, Action<bool>? onChanged = null, string tooltip = "")
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Toggle text cannot be empty.",
                nameof(text)
            );

        Text = text;
        Tooltip = tooltip;
        DefaultValue = defaultValue;
        Value = defaultValue;
        OnChanged = onChanged;
    }

    public void Set(bool value, bool invoke = true)
    {
        Value = value;

        HeavenlyAPI.Log($"Toggle '{Text}' -> {(value ? "ON" : "OFF")}");

        if (invoke)
            OnChanged?.Invoke(value);
    }

    /// <summary>
    /// Updates the model from the live game state without firing handlers.
    /// The bridge mirrors the value into the visible control.
    /// </summary>
    public void SetExternal(bool value)
    {
        Value = value;
    }
}
