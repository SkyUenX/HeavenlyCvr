using System;
using ABI_RC.Systems.UI.UILib;

namespace HeavenlyCVR.Heavenly.Features.Social;

/// <summary>
/// Shared player target: an explicit override picked via CVR's player
/// selector, falling back to the QuickMenu's selected player.
/// </summary>
public static class PlayerTarget
{
    private static string? _overrideId;
    private static string? _overrideName;

    public static bool HasOverride => !string.IsNullOrEmpty(_overrideId);

    public static void ChooseViaSelector()
    {
        try
        {
            QuickMenuAPI.OpenPlayerSelector(
                "Heavenly target",
                obj =>
                {
                    try
                    {
                        if (obj == null || string.IsNullOrEmpty(obj.Uuid))
                            return;

                        _overrideId = obj.Uuid;
                        _overrideName = obj.Username;
                        API.HeavenlyAPI.Toast($"[Heavenly] Target: {_overrideName}.");
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Warning($"Player select failed: {ex.Message}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Player selector failed: {ex.Message}");
        }
    }

    public static void ClearOverride()
    {
        _overrideId = null;
        _overrideName = null;
        API.HeavenlyAPI.Toast("[Heavenly] Target cleared (using QuickMenu selection).");
    }

    public static string? CurrentId()
    {
        if (!string.IsNullOrEmpty(_overrideId))
            return _overrideId;
        try { return QuickMenuAPI.SelectedPlayerID; } catch { return null; }
    }

    public static string? CurrentName()
    {
        if (!string.IsNullOrEmpty(_overrideId))
            return string.IsNullOrEmpty(_overrideName) ? _overrideId : _overrideName;
        try { return QuickMenuAPI.SelectedPlayerName; } catch { return null; }
    }
}
