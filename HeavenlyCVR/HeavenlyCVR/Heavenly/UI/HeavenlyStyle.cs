namespace HeavenlyCVR.Heavenly.UI;

/// <summary>
/// Heavenly honeycomb theme for CVR's Cohtml QuickMenu, matching the
/// original HeavenlyVRC look: dark hexagonal plates with complete red
/// edges, split red/dark toggle states, ON/OFF readouts.
///
/// Technique (deliberately primitive — only constructs already proven to
/// render in this Cohtml build): each cell is transparent and borderless;
/// ::after paints a full-size red hexagon behind everything as the edge,
/// ::before paints a slightly smaller dark hexagon as the face, and the
/// label is lifted above both. ON toggles turn the face red. State text
/// uses a nested ::after on the label itself.
/// </summary>
public static class HeavenlyStyle
{
    public const string Accent = "#ff1a1a";

    private const string Scope = "[id^=\"CVRUI-QMUI-HeavenlyCVR\"]";

    private const string Hex = "polygon(25% 0%, 75% 0%, 100% 50%, 75% 100%, 25% 100%, 0% 50%)";

    private const string Css = @"
:root {
  --menu-accent: #ff1a1a !important;
}
#quickmenu-wrapper .container-tabs .tab {
  position: relative;
  z-index: 0;
  background: transparent !important;
  background-color: transparent !important;
  border: none !important;
  border-width: 0 !important;
  border-style: none !important;
}
#quickmenu-wrapper .container-tabs .tab::before {
  content: """";
  position: absolute;
  top: 6px;
  left: 6px;
  right: 6px;
  bottom: 6px;
  background: #121216 !important;
  clip-path: HEXHEX;
}
#quickmenu-wrapper .container-tabs .tab::after {
  content: """";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: #ff1a1a !important;
  clip-path: HEXHEX;
  z-index: -1;
}
#quickmenu-wrapper .container-tabs .selected::before {
  background: #c81414 !important;
}
#quickmenu-wrapper .container-tabs .scroll-overlay {
  background-color: #ff1a1a !important;
}
CVRUI_HEX_SCOPE .button,
CVRUI_HEX_SCOPE .button-textOnly,
CVRUI_HEX_SCOPE .button-fullImage {
  position: relative;
  z-index: 0;
  background: transparent !important;
  background-color: transparent !important;
  border: none !important;
  border-width: 0 !important;
  border-style: none !important;
  color: #ffffff !important;
}
CVRUI_HEX_SCOPE .button::before,
CVRUI_HEX_SCOPE .button-textOnly::before,
CVRUI_HEX_SCOPE .button-fullImage::before {
  content: """";
  position: absolute;
  top: 6px;
  left: 6px;
  right: 6px;
  bottom: 6px;
  background: #121216 !important;
  clip-path: HEXHEX;
}
CVRUI_HEX_SCOPE .button::after,
CVRUI_HEX_SCOPE .button-textOnly::after,
CVRUI_HEX_SCOPE .button-fullImage::after {
  content: """";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: #ff1a1a !important;
  clip-path: HEXHEX;
  z-index: -1;
}
CVRUI_HEX_SCOPE .button .text,
CVRUI_HEX_SCOPE .button-textOnly .text,
CVRUI_HEX_SCOPE .button-fullImage .text {
  position: relative;
  z-index: 1;
  color: #ffffff !important;
  font-size: 26px !important;
}
CVRUI_HEX_SCOPE .button .icon {
  display: none !important;
}
CVRUI_HEX_SCOPE .toggle,
CVRUI_HEX_SCOPE .toggle-long {
  position: relative;
  z-index: 0;
  background: transparent !important;
  background-color: transparent !important;
  border: none !important;
  border-width: 0 !important;
  border-style: none !important;
  color: #ffffff !important;
}
CVRUI_HEX_SCOPE .toggle::before,
CVRUI_HEX_SCOPE .toggle-long::before {
  content: """";
  position: absolute;
  top: 6px;
  left: 6px;
  right: 6px;
  bottom: 6px;
  background: #121216 !important;
  clip-path: HEXHEX;
}
CVRUI_HEX_SCOPE .toggle::after,
CVRUI_HEX_SCOPE .toggle-long::after {
  content: """";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: #ff1a1a !important;
  clip-path: HEXHEX;
  z-index: -1;
}
CVRUI_HEX_SCOPE .toggle[data-toggleState=""true""]::before,
CVRUI_HEX_SCOPE .toggle-long[data-toggleState=""true""]::before {
  background: #c81414 !important;
}
CVRUI_HEX_SCOPE .toggle .text,
CVRUI_HEX_SCOPE .toggle-long .text,
CVRUI_HEX_SCOPE .toggle .text-sm {
  position: relative;
  z-index: 1;
  color: #ffffff !important;
  font-size: 26px !important;
}
CVRUI_HEX_SCOPE .toggle .text-sm::after {
  content: "" — OFF"";
  color: #888888 !important;
}
CVRUI_HEX_SCOPE .toggle[data-toggleState=""true""] .text-sm::after {
  content: "" — ON"";
  color: #ffffff !important;
}
CVRUI_HEX_SCOPE .toggle .enable,
CVRUI_HEX_SCOPE .toggle .disable,
CVRUI_HEX_SCOPE .toggle .circle {
  display: none !important;
}
CVRUI_HEX_SCOPE .slider-root .slider .slider-knob {
  background-color: #ff1a1a !important;
}
CVRUI_HEX_SCOPE .slider-root .slider .sliderBar {
  border-color: #ff1a1a !important;
}
CVRUI_HEX_SCOPE .slider-root .resetButton {
  border-color: #ff1a1a !important;
  color: #ffffff !important;
  background: rgba(18, 18, 22, 0.94) !important;
}
CVRUI_HEX_SCOPE .category-header {
  border-bottom-color: #ff1a1a !important;
}
CVRUI_HEX_SCOPE .category-header .header {
  color: #ff3030 !important;
}
CVRUI_HEX_SCOPE .text-block .title,
CVRUI_HEX_SCOPE .content-button .text-block .title {
  color: #ffffff !important;
}
";

    public static void Apply()
    {
        try
        {
            string css = Css
                .Replace("CVRUI_HEX_SCOPE", Scope)
                .Replace("HEXHEX", Hex);
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.InjectCSSStyle(css);
            API.HeavenlyAPI.Log($"Heavenly honeycomb style injected ({css.Length} chars).");
        }
        catch (System.Exception ex)
        {
            API.HeavenlyAPI.Warning($"Heavenly style injection failed: {ex.Message}");
        }
    }
}
