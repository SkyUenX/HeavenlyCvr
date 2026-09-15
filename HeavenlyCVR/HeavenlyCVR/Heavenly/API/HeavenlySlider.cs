using System;

namespace HeavenlyCVR.Heavenly.API;

public class HeavenlySlider
{
    public string Text { get; }

    public string Tooltip { get; }

    public float Min { get; }

    public float Max { get; }

    public float Value { get; private set; }

    public Action<float>? OnChanged { get; }

    public HeavenlySlider(string text, float min, float max, float defaultValue, Action<float>? onChanged = null, string tooltip = "")
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                "Slider text cannot be empty.",
                nameof(text)
            );

        if (min >= max)
            throw new ArgumentException("Slider min must be less than max.");

        Text = text;
        Tooltip = tooltip;
        Min = min;
        Max = max;
        Value = Clamp(defaultValue, min, max);
        OnChanged = onChanged;
    }

    public void Set(float value, bool invoke = true)
    {
        Value = Clamp(value, Min, Max);

        HeavenlyAPI.Log($"Slider '{Text}' -> {Value}");

        if (invoke)
            OnChanged?.Invoke(Value);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
