using MelonLoader;

[assembly: MelonInfo(
    typeof(HeavenlyCVR.HeavenlyPlugin),
    "HeavenlyCVR",
    "0.2.0",
    "SkyUenX"
)]

[assembly: MelonGame(
    "ChilloutVR",
    "ChilloutVR"
)]

namespace HeavenlyCVR;

public class HeavenlyPlugin : MelonMod
{
    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("================================");
        MelonLogger.Msg(" HeavenlyCVR");
        MelonLogger.Msg(" Version 0.2.0");
        MelonLogger.Msg(" Made by SkyUenX");
        MelonLogger.Msg("================================");
        MelonLogger.Msg("HEAVENLYCVR HAS LOADED!");

        Heavenly.API.HeavenlyAPI.Log("Heavenly API initialized!");

        // 0. Persistent config first: everything below reads defaults from it.
        try
        {
            Heavenly.Config.HeavenlyConfig.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Movement.MovementService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.ESP.EspService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.World.WorldService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Debug.DebugService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Protection.ProtectionService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Tags.TagService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Social.PlayerNotes.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Avatar.AvatarService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Voice.VoiceService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Movement.BookmarkService.Initialize();
        }
        catch { }

        try
        {
            Heavenly.Features.Movement.CameraService.Initialize();
        }
        catch { }

        // 1. Build the Heavenly page model.
        Heavenly.UI.HeavenlyMenuBuilder.Build();

        // 2. Render into CVR's built-in UILib (Cohtml QuickMenu).
        //    Same backend BTKUILib uses - works with or without BTKUILib mod.
        Heavenly.UI.HeavenlyUILibBridge.Initialize();

        // 3. Optional UIX settings bridge (safe if UIX mod is missing).
        Heavenly.UI.HeavenlyUIXBridge.Initialize();

        // NOTE: Legacy Unity uGUI HeavenlyPanel is deprecated.
        // CVR's QuickMenu is Cohtml, so parenting raw Unity UI to
        // "Cohtml/QuickMenu" is fragile and disabled by default.
        // See HeavenlyPanel.Initialize() if you want to experiment.
        // Heavenly.UI.HeavenlyPanel.Initialize();

        var menu = new Heavenly.UI.HeavenlyMenu("HeavenlyCVR");
        menu.Open();

        Heavenly.API.HeavenlyAPI.Log(
            $"Menu open: {menu.Title} (IsOpen={menu.IsOpen})"
        );

        Heavenly.API.HeavenlyAPI.Log(
            $"Pages: {Heavenly.UI.HeavenlyUI.Pages.Count}, Current: {Heavenly.UI.HeavenlyUI.CurrentPage?.Name}"
        );
    }

    public override void OnUpdate()
    {
        try
        {
            Heavenly.Features.Input.KeybindService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.Movement.MovementService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.World.WorldService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.ESP.EspService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.Protection.ProtectionService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.Tags.TagService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Features.Debug.DebugService.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.UI.ToggleSync.OnUpdate();
        }
        catch { }

        try
        {
            Heavenly.Config.HeavenlyConfig.Flush();
        }
        catch { }
    }
}
