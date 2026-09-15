using System;

namespace HeavenlyCVR.Heavenly.API;

public static class HeavenlyAPI
{
    public const string Name = "HeavenlyCVR";
    public const string Version = "0.2.0";
    public const string Author = "SkyUenX";

    public static void Log(string message)
    {
        MelonLoader.MelonLogger.Msg($"[HeavenlyCVR] {message}");
    }

    public static void Warning(string message)
    {
        MelonLoader.MelonLogger.Warning($"[HeavenlyCVR] {message}");
    }

    public static void Error(string message)
    {
        MelonLoader.MelonLogger.Error($"[HeavenlyCVR] {message}");
    }

    /// <summary>
    /// Best-effort in-game toast. Falls back to log if UILib is not ready.
    /// </summary>
    public static void Toast(string message, int delaySeconds = 3)
    {
        try
        {
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.ShowAlertToast(message, delaySeconds);
        }
        catch
        {
            // UILib not ready yet (menu not generated) - log only.
        }

        Log(message);
    }

    public static void NotImplemented(string feature)
    {
        Toast($"[Heavenly] {feature}: not implemented yet (shell placeholder).");
    }
}
