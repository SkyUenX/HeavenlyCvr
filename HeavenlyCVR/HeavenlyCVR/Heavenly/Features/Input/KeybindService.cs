using System;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Input;

/// <summary>
/// Rebindable Ctrl+Key shortcuts. The letter is user-set (Keys page or
/// MelonPreferences); LeftControl is always the modifier to avoid eating
/// game keys. Listens for the next keypress while rebinding.
/// </summary>
public static class KeybindService
{
    public enum HeavenlyAction
    {
        ToggleFly,
        Rejoin,
        ToggleNoClip,
        StandUp,
        Panic,
        Respawn,
        ReloadAvatar,
        ToggleEsp,
        ToggleFlashlight,
        ReloadMenus,
        Dash
    }

    private static HeavenlyAction? _listening;

    public static bool IsListening => _listening != null;

    public static KeyCode BoundKey(HeavenlyAction action)
    {
        try
        {
            int raw = action switch
            {
                HeavenlyAction.ToggleFly => Config.HeavenlyConfig.KeyFly,
                HeavenlyAction.Rejoin => Config.HeavenlyConfig.KeyRejoin,
                HeavenlyAction.ToggleNoClip => Config.HeavenlyConfig.KeyNoClip,
                HeavenlyAction.StandUp => Config.HeavenlyConfig.KeyStand,
                HeavenlyAction.Panic => Config.HeavenlyConfig.KeyPanic,
                HeavenlyAction.Respawn => Config.HeavenlyConfig.KeyRespawn,
                HeavenlyAction.ReloadAvatar => Config.HeavenlyConfig.KeyReloadAvatar,
                HeavenlyAction.ToggleEsp => Config.HeavenlyConfig.KeyEsp,
                HeavenlyAction.ToggleFlashlight => Config.HeavenlyConfig.KeyFlashlight,
                HeavenlyAction.ReloadMenus => Config.HeavenlyConfig.KeyMenus,
                HeavenlyAction.Dash => Config.HeavenlyConfig.KeyDash,
                _ => 0
            };
            return (KeyCode)raw;
        }
        catch
        {
            return KeyCode.None;
        }
    }

    public static string Label(HeavenlyAction action)
    {
        return action switch
        {
            HeavenlyAction.ToggleFly => "Fly",
            HeavenlyAction.Rejoin => "Rejoin",
            HeavenlyAction.ToggleNoClip => "NoClip",
            HeavenlyAction.StandUp => "Stand up",
            HeavenlyAction.Panic => "Panic",
            HeavenlyAction.Respawn => "Respawn",
            HeavenlyAction.ReloadAvatar => "Reload avatar",
            HeavenlyAction.ToggleEsp => "ESP master",
            HeavenlyAction.ToggleFlashlight => "Flashlight",
            HeavenlyAction.ReloadMenus => "Reload menus",
            HeavenlyAction.Dash => "Air dash",
            _ => action.ToString()
        };
    }

    public static void StartRebind(HeavenlyAction action)
    {
        if (_listening == action)
        {
            CancelRebind();
            API.HeavenlyAPI.Toast("[Heavenly] Rebind cancelled.");
            return;
        }

        _listening = action;
        API.HeavenlyAPI.Toast($"[Heavenly] Press a key for {Label(action)} (Esc cancels).");
    }

    public static void CancelRebind()
    {
        _listening = null;
    }

    /// <summary>Called every frame from HeavenlyPlugin.OnUpdate.</summary>
    public static void OnUpdate()
    {
        try
        {
            if (_listening != null)
            {
                PollRebind(_listening.Value);
                return;
            }

            bool ctrl = false;
            try { ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl); }
            catch { return; }
            if (!ctrl)
                return;

            if (WasPressed(BoundKey(HeavenlyAction.ToggleFly)))
                Movement.MovementService.ToggleFlying();
            else if (WasPressed(BoundKey(HeavenlyAction.Rejoin)))
                World.WorldService.RejoinCurrent();
            else if (WasPressed(BoundKey(HeavenlyAction.ToggleNoClip)))
                Movement.MovementService.SetNoClip(!Movement.MovementService.IsNoClip);
            else if (WasPressed(BoundKey(HeavenlyAction.StandUp)))
                Movement.MovementService.StandUp();
            else if (WasPressed(BoundKey(HeavenlyAction.Panic)))
                Protection.ProtectionService.Panic();
            else if (WasPressed(BoundKey(HeavenlyAction.Respawn)))
                Movement.MovementService.Respawn();
            else if (WasPressed(BoundKey(HeavenlyAction.ReloadAvatar)))
                Avatar.AvatarService.ReloadCurrent();
            else if (WasPressed(BoundKey(HeavenlyAction.ToggleEsp)))
                ESP.EspService.SetMaster(!ESP.EspService.MasterEnabled);
            else if (WasPressed(BoundKey(HeavenlyAction.ToggleFlashlight)))
                Movement.FlashlightService.SetEnabled(!Movement.FlashlightService.Enabled);
            else if (WasPressed(BoundKey(HeavenlyAction.ReloadMenus)))
                Debug.DebugService.ReloadMenus();
            else if (WasPressed(BoundKey(HeavenlyAction.Dash)))
                Movement.MovementService.Dash();
        }
        catch { }
    }

    private static bool WasPressed(KeyCode key)
    {
        if (key == KeyCode.None)
            return false;
        try { return UnityEngine.Input.GetKeyDown(key); } catch { return false; }
    }

    private static void PollRebind(HeavenlyAction action)
    {
        try
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None)
                    continue;

                bool down = false;
                try { down = UnityEngine.Input.GetKeyDown(key); } catch { continue; }
                if (!down)
                    continue;

                if (key == KeyCode.Escape)
                {
                    _listening = null;
                    API.HeavenlyAPI.Toast("[Heavenly] Rebind cancelled.");
                    return;
                }

                if (key == KeyCode.LeftControl || key == KeyCode.RightControl)
                    continue;

                SetBoundKey(action, key);
                _listening = null;
                API.HeavenlyAPI.Toast($"[Heavenly] {Label(action)} is now Ctrl+{key}.");
                return;
            }
        }
        catch { }
    }

    private static void SetBoundKey(HeavenlyAction action, KeyCode key)
    {
        try
        {
            int raw = (int)key;
            switch (action)
            {
                case HeavenlyAction.ToggleFly: Config.HeavenlyConfig.KeyFly = raw; break;
                case HeavenlyAction.Rejoin: Config.HeavenlyConfig.KeyRejoin = raw; break;
                case HeavenlyAction.ToggleNoClip: Config.HeavenlyConfig.KeyNoClip = raw; break;
                case HeavenlyAction.StandUp: Config.HeavenlyConfig.KeyStand = raw; break;
                case HeavenlyAction.Panic: Config.HeavenlyConfig.KeyPanic = raw; break;
                case HeavenlyAction.Respawn: Config.HeavenlyConfig.KeyRespawn = raw; break;
                case HeavenlyAction.ReloadAvatar: Config.HeavenlyConfig.KeyReloadAvatar = raw; break;
                case HeavenlyAction.ToggleEsp: Config.HeavenlyConfig.KeyEsp = raw; break;
                case HeavenlyAction.ToggleFlashlight: Config.HeavenlyConfig.KeyFlashlight = raw; break;
                case HeavenlyAction.ReloadMenus: Config.HeavenlyConfig.KeyMenus = raw; break;
                case HeavenlyAction.Dash: Config.HeavenlyConfig.KeyDash = raw; break;
            }
        }
        catch { }
    }
}
