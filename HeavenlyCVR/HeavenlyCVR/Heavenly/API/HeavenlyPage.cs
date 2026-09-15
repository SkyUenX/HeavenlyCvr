using System;
using System.Collections.Generic;

namespace HeavenlyCVR.Heavenly.API;

public class HeavenlyPage
{
    private readonly List<HeavenlyButton> _buttons = new();
    private readonly List<HeavenlyToggle> _toggles = new();
    private readonly List<HeavenlySlider> _sliders = new();
    private readonly List<HeavenlyLabel> _labels = new();
    private readonly List<HeavenlyPage> _subPages = new();

    public string Name { get; }

    public string Tooltip { get; }

    public IReadOnlyList<HeavenlyButton> Buttons => _buttons;
    public IReadOnlyList<HeavenlyToggle> Toggles => _toggles;
    public IReadOnlyList<HeavenlySlider> Sliders => _sliders;
    public IReadOnlyList<HeavenlyLabel> Labels => _labels;
    public IReadOnlyList<HeavenlyPage> SubPages => _subPages;

    public HeavenlyPage(string name, string tooltip = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Page name cannot be empty.",
                nameof(name)
            );

        Name = name;
        Tooltip = tooltip;
    }

    public HeavenlyButton AddButton(
        string text,
        Action? onClick = null,
        string tooltip = "")
    {
        HeavenlyButton button = new(text, onClick, tooltip);

        _buttons.Add(button);

        HeavenlyAPI.Log(
            $"Page '{Name}': added button '{text}'"
        );

        return button;
    }

    public HeavenlyToggle AddToggle(
        string text,
        bool defaultValue = false,
        Action<bool>? onChanged = null,
        string tooltip = "")
    {
        HeavenlyToggle toggle = new(text, defaultValue, onChanged, tooltip);

        _toggles.Add(toggle);

        HeavenlyAPI.Log(
            $"Page '{Name}': added toggle '{text}'"
        );

        return toggle;
    }

    public HeavenlySlider AddSlider(
        string text,
        float min,
        float max,
        float defaultValue,
        Action<float>? onChanged = null,
        string tooltip = "")
    {
        HeavenlySlider slider = new(text, min, max, defaultValue, onChanged, tooltip);

        _sliders.Add(slider);

        HeavenlyAPI.Log(
            $"Page '{Name}': added slider '{text}'"
        );

        return slider;
    }

    public HeavenlyLabel AddLabel(string text)
    {
        HeavenlyLabel label = new(text);

        _labels.Add(label);

        return label;
    }

    public HeavenlyPage AddSubPage(string name, string tooltip = "")
    {
        HeavenlyPage sub = new(name, tooltip);

        _subPages.Add(sub);

        HeavenlyAPI.Log(
            $"Page '{Name}': added sub-page '{name}'"
        );

        return sub;
    }

    public bool RemoveButton(HeavenlyButton button)
    {
        if (!_buttons.Remove(button))
            return false;

        HeavenlyAPI.Log(
            $"Page '{Name}': removed button '{button.Text}'"
        );

        return true;
    }

    public void ClearButtons()
    {
        _buttons.Clear();

        HeavenlyAPI.Log(
            $"Page '{Name}': cleared buttons"
        );
    }
}