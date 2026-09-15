using System;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Movement;

/// <summary>
/// Headlamp: a spotlight parented to the active camera. Local-only,
/// destroyed on toggle-off and world transitions.
/// </summary>
public static class FlashlightService
{
    public static bool Enabled { get; private set; }

    private static GameObject? _lightObject;

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;

        if (!enabled)
        {
            DestroyLight();
            API.HeavenlyAPI.Toast("[Heavenly] Flashlight OFF.");
            return;
        }

        try
        {
            DestroyLight();

            Camera? cam = null;
            try { cam = Camera.main; } catch { }
            if (cam == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No camera yet.");
                Enabled = false;
                return;
            }

            _lightObject = new GameObject("HeavenlyFlashlight");
            _lightObject.transform.SetParent(cam.transform, false);
            _lightObject.transform.localPosition = new Vector3(0f, 0.05f, 0.1f);
            _lightObject.transform.localRotation = Quaternion.identity;

            Light light = _lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            ApplyValues(light);

            API.HeavenlyAPI.Toast("[Heavenly] Flashlight ON (follows your view).");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Flashlight failed: {ex.Message}");
            Enabled = false;
            DestroyLight();
        }
    }

    public static void ApplyValues()
    {
        try
        {
            if (_lightObject == null)
                return;
            Light? light = _lightObject.GetComponent<Light>();
            if (light == null)
                return;
            ApplyValues(light);
        }
        catch { }
    }

    public static void SetAngle(float angle)
    {
        try { Config.HeavenlyConfig.FlashAngle = angle; } catch { }
        ApplyValues();
    }

    public static void SetRange(float range)
    {
        try { Config.HeavenlyConfig.FlashRange = range; } catch { }
        ApplyValues();
    }

    public static void SetIntensity(float intensity)
    {
        try { Config.HeavenlyConfig.FlashIntensity = intensity; } catch { }
        ApplyValues();
    }

    /// <summary>Called on world load: the light object dies with the scene.</summary>
    public static void OnWorldLoaded()
    {
        _lightObject = null;
        if (Enabled)
        {
            Enabled = false;
            API.HeavenlyAPI.Log("Flashlight cleared by world transition (re-enable to restore).");
        }
    }

    private static void ApplyValues(Light light)
    {
        try
        {
            float angle = Config.HeavenlyConfig.FlashAngle;
            float range = Config.HeavenlyConfig.FlashRange;
            float intensity = Config.HeavenlyConfig.FlashIntensity;

            if (angle < 10f) angle = 10f;
            if (angle > 120f) angle = 120f;
            if (range < 2f) range = 2f;
            if (range > 80f) range = 80f;
            if (intensity < 0f) intensity = 0f;
            if (intensity > 8f) intensity = 8f;

            light.spotAngle = angle;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Flashlight values failed: {ex.Message}");
        }
    }

    private static void DestroyLight()
    {
        try
        {
            if (_lightObject != null)
                UnityEngine.Object.Destroy(_lightObject);
        }
        catch { }
        _lightObject = null;
    }
}
