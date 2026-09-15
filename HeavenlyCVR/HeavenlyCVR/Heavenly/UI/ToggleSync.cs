using System;
using System.Collections.Generic;
using HeavenlyCVR.Heavenly.API;

namespace HeavenlyCVR.Heavenly.UI;

/// <summary>
/// Pushes live game state back into Heavenly toggles (model + visible UI).
/// Covers settings the game can change behind our back: flight, noclip,
/// mic mute, distance culling. UILib controls are updated in place;
/// UIX settings rows are intentionally left alone (re-read on open).
/// </summary>
public static class ToggleSync
{
    private static readonly Dictionary<string, (HeavenlyToggle toggle, Action<bool>? apply)> Entries = new();
    private static float _nextSync;

    public static void Register(string key, HeavenlyToggle toggle, Action<bool>? apply = null)
    {
        try
        {
            Entries[key] = (toggle, apply);
        }
        catch { }
    }

    /// <summary>Called every frame; syncs at most every 5 seconds.</summary>
    public static void OnUpdate()
    {
        try
        {
            if (UnityEngine.Time.time < _nextSync)
                return;
            _nextSync = UnityEngine.Time.time + 5f;

            Push("fly", ReadFly());
            Push("noclip", ReadNoClip());
            Push("mic", ReadMic());
            Push("culling", ReadCulling());
        }
        catch { }
    }

    private static void Push(string key, bool? state)
    {
        try
        {
            if (state == null || !Entries.TryGetValue(key, out var entry) || entry.toggle == null)
                return;

            if (entry.toggle.Value == state.Value)
                return;

            entry.toggle.SetExternal(state.Value);
            try { entry.apply?.Invoke(state.Value); } catch { }
            HeavenlyUILibBridge.PushToggleState(entry.toggle);
            HeavenlyAPI.Log($"Toggle '{entry.toggle.Text}' synced -> {(state.Value ? "ON" : "OFF")} (game changed it).");
        }
        catch { }
    }

    private static bool? ReadFly()
    {
        try
        {
            var local = ABI_RC.API.PlayerAPI.LocalPlayerInternal;
            if (local != null)
                return local.IsFlying;
        }
        catch { }
        return null;
    }

    private static bool? ReadNoClip()
    {
        try
        {
            var controller = ABI_RC.Systems.Movement.BetterBetterCharacterController.Instance;
            if (controller != null)
                return controller.IsFlyingNoClipEnabled();
        }
        catch { }
        return null;
    }

    private static bool? ReadMic()
    {
        try
        {
            return ABI_RC.Systems.Communications.Comms_Manager.IsMicMuted;
        }
        catch { }
        return null;
    }

    private static bool? ReadCulling()
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager != null)
                return manager.disablePlayerAtDistance;
        }
        catch { }
        return null;
    }
}
