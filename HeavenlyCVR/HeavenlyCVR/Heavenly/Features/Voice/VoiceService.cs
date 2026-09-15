using System;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Communications;

namespace HeavenlyCVR.Heavenly.Features.Voice;

public static class VoiceService
{
    public static float MasterVolume
    {
        get
        {
            try { return Config.HeavenlyConfig.VolMaster; }
            catch { return 1f; }
        }
    }

    public static float AvatarVolume
    {
        get
        {
            try { return Config.HeavenlyConfig.VolAvatar; }
            catch { return 1f; }
        }
    }

    public static float WorldVolume
    {
        get
        {
            try { return Config.HeavenlyConfig.VolWorld; }
            catch { return 1f; }
        }
    }

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        ApplySaved();

        try
        {
            ABI_RC.Core.IO.CVRObjectLoader.OnWorldLoadedAfterEnable += OnWorldLoaded;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Voice: world-load hook failed: {ex.Message}");
        }
    }

    private static void OnWorldLoaded(string _)
    {
        ApplySaved();
    }

    /// <summary>Pushes saved volumes into CVR. Safe to call before MetaPort exists.</summary>
    public static void ApplySaved()
    {
        try
        {
            if (MetaPort.Instance == null)
                return;

            try { MetaPort.Instance.masterAudio = ClampVol(Config.HeavenlyConfig.VolMaster); } catch { }
            try { MetaPort.Instance.avatarAudio = Config.HeavenlyConfig.VolAvatar; } catch { }
            try { MetaPort.Instance.worldAudio = Config.HeavenlyConfig.VolWorld; } catch { }
        }
        catch { }
    }

    public static void SetMasterVolume(float value)
    {
        try
        {
            if (MetaPort.Instance == null)
            {
                API.HeavenlyAPI.Warning("Voice: MetaPort not ready.");
                return;
            }

            MetaPort.Instance.masterAudio = ClampVol(value);
            try { Config.HeavenlyConfig.VolMaster = MetaPort.Instance.masterAudio; } catch { }
            API.HeavenlyAPI.Log($"Master volume -> {MetaPort.Instance.masterAudio:0.00}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Set master volume failed: {ex.Message}");
        }
    }

    public static void SetAvatarVolume(float value)
    {
        try
        {
            if (MetaPort.Instance == null)
            {
                API.HeavenlyAPI.Warning("Voice: MetaPort not ready.");
                return;
            }

            MetaPort.Instance.avatarAudio = value;
            try { Config.HeavenlyConfig.VolAvatar = value; } catch { }
            API.HeavenlyAPI.Log($"Avatar (player voice) volume -> {value:0.00}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Set avatar volume failed: {ex.Message}");
        }
    }

    public static void SetWorldVolume(float value)
    {
        try
        {
            if (MetaPort.Instance == null)
            {
                API.HeavenlyAPI.Warning("Voice: MetaPort not ready.");
                return;
            }

            MetaPort.Instance.worldAudio = value;
            try { Config.HeavenlyConfig.VolWorld = value; } catch { }
            API.HeavenlyAPI.Log($"World volume -> {value:0.00}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Set world volume failed: {ex.Message}");
        }
    }

    public static bool IsMicMuted
    {
        get
        {
            try { return Comms_Manager.IsMicMuted; }
            catch { return false; }
        }
    }

    private static float _preDeafenVolume = 1f;

    public static void SetDeafened(bool deafened)
    {
        try
        {
            if (MetaPort.Instance == null)
            {
                API.HeavenlyAPI.Warning("Voice: MetaPort not ready.");
                return;
            }

            if (deafened)
            {
                _preDeafenVolume = MetaPort.Instance.masterAudio;
                MetaPort.Instance.masterAudio = 0f;
                API.HeavenlyAPI.Toast("[Heavenly] Deafened (all output muted).");
            }
            else
            {
                MetaPort.Instance.masterAudio = _preDeafenVolume;
                try { Config.HeavenlyConfig.VolMaster = _preDeafenVolume; } catch { }
                API.HeavenlyAPI.Toast("[Heavenly] Undeafened.");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Deafen failed: {ex.Message}");
        }
    }

    public static void SetMicMuted(bool muted)
    {
        try
        {
            Comms_Manager.IsMicMuted = muted;
            API.HeavenlyAPI.Toast(muted ? "[Heavenly] Mic muted." : "[Heavenly] Mic unmuted.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Mic mute failed: {ex.Message}");
        }
    }

    private static float ClampVol(float v)
    {
        if (v < 0f) return 0f;
        if (v > 2f) return 2f;
        return v;
    }
}
