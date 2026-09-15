using System;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Movement;

/// <summary>
/// Camera field of view. Untouched (-1) until the user moves the slider,
/// then applied live and re-applied on world load (cameras rebuild).
/// </summary>
public static class CameraService
{
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
            API.HeavenlyAPI.Warning($"Camera: world-load hook failed: {ex.Message}");
        }
    }

    public static float CurrentFov()
    {
        try
        {
            Camera? cam = Camera.main;
            if (cam != null)
                return cam.fieldOfView;
        }
        catch { }
        return 60f;
    }

    public static void SetFov(float fov)
    {
        if (fov < 40f) fov = 40f;
        if (fov > 120f) fov = 120f;

        try { Config.HeavenlyConfig.Fov = fov; } catch { }
        ApplySaved();
        API.HeavenlyAPI.Log($"FOV -> {fov:0}");
    }

    public static void ApplySaved()
    {
        float want;
        try { want = Config.HeavenlyConfig.Fov; }
        catch { return; }

        if (want < 0f)
            return;

        if (want < 40f) want = 40f;
        if (want > 120f) want = 120f;

        try
        {
            Camera? cam = Camera.main;
            if (cam == null)
                return;
            cam.fieldOfView = want;
        }
        catch { }
    }

    private static void OnWorldLoaded(string _)
    {
        ApplySaved();
    }
}
