using System;
using HeavenlyCVR.Heavenly.API;
using HeavenlyCVR.Heavenly.Features.Avatar;
using HeavenlyCVR.Heavenly.Features.Debug;
using HeavenlyCVR.Heavenly.Features.ESP;
using HeavenlyCVR.Heavenly.Features.Movement;
using HeavenlyCVR.Heavenly.Features.Protection;
using HeavenlyCVR.Heavenly.Features.Social;
using HeavenlyCVR.Heavenly.Features.Tags;
using HeavenlyCVR.Heavenly.Features.Voice;
using HeavenlyCVR.Heavenly.Features.World;

namespace HeavenlyCVR.Heavenly.UI;

/// <summary>
/// Defines the HeavenlyCVR QuickMenu.
/// Mirrors the original HeavenlyVRC layout (Main/Game/Avatar/Voice/Sit/ESP/Social/Debug)
/// adapted to ChilloutVR using native CVR APIs. Deliberately excludes
/// force-clone private, serialize/desync, earrape, and remote kill-switch.
/// </summary>
public static class HeavenlyMenuBuilder
{
    public static void Build()
    {
        HeavenlyUI.Clear();

        BuildMainPage();
        BuildWorldPage();
        BuildAvatarPage();
        BuildVoicePage();
        BuildMovementPage();
        BuildEspPage();
        BuildSocialPage();
        BuildProtectionPage();
        BuildDebugPage();
        BuildTagsPage();
        BuildKeysPage();
        BuildConfigPage();

        HeavenlyAPI.Log($"Menu built: {HeavenlyUI.Pages.Count} pages.");
    }

    private static void BuildMainPage()
    {
        HeavenlyPage main = HeavenlyUI.CreatePage("Main");

        main.AddLabel("HeavenlyCVR - ChilloutVR port");

        main.AddButton("Open World", () => HeavenlyUILibBridge.OpenPage("World"), "World / instance options");
        main.AddButton("Open Avatar", () => HeavenlyUILibBridge.OpenPage("Avatar"), "Avatar options");
        main.AddButton("Open Movement", () => HeavenlyUILibBridge.OpenPage("Movement"), "Fly / sit / speed");
        main.AddButton("Open ESP", () => HeavenlyUILibBridge.OpenPage("ESP"), "ESP markers");
        main.AddButton("Open Social", () => HeavenlyUILibBridge.OpenPage("Social"), "Player actions");
        main.AddButton("Open Protection", () => HeavenlyUILibBridge.OpenPage("Protection"), "Safety options");
        main.AddButton("Open Voice", () => HeavenlyUILibBridge.OpenPage("Voice"), "Voice options");
        main.AddButton("Open Debug", () => HeavenlyUILibBridge.OpenPage("Debug"), "Logs and status");
        main.AddButton("Open Tags", () => HeavenlyUILibBridge.OpenPage("Tags"), "Custom nameplate tags");
        main.AddButton("Open Keys", () => HeavenlyUILibBridge.OpenPage("Keys"), "Rebindable shortcuts");
        main.AddButton("Open Config", () => HeavenlyUILibBridge.OpenPage("Config"), "Every tunable, persisted");
    }

    private static void BuildWorldPage()
    {
        HeavenlyPage world = HeavenlyUI.CreatePage("World", "World / instance options");

        world.AddLabel("Instance controls via CVR instance system.");

        world.AddButton("Rejoin Current", WorldService.RejoinCurrent, "Rejoin this instance");
        world.AddButton("Rejoin Last", WorldService.RejoinLast, "Rejoin the previous instance");
        world.AddButton("Reload World", WorldService.ReloadWorld, "Reload (rejoin current)");
        world.AddButton("Go Home", WorldService.GoHome, "Go to home world");
        world.AddButton(
            "Copy Instance ID",
            WorldService.CopyInstanceId,
            "Copy/show current instance + world ID"
        );
        world.AddButton(
            "Log Status",
            () => HeavenlyAPI.Log(WorldService.StatusLine()),
            "Log world / instance IDs"
        );
        world.AddButton(
            "Log History",
            WorldService.LogHistory,
            "List past instances (newest first)"
        );
        world.AddButton(
            "Rejoin #...",
            WorldService.RejoinIndexKeyboard,
            "Enter a history number to rejoin"
        );
        world.AddButton(
            "Join Instance ID...",
            WorldService.JoinInstanceIdKeyboard,
            "Join an instance by pasting its ID"
        );
        world.AddButton(
            "Set Home Here",
            WorldService.SetHomeHere,
            "Make this world your home world"
        );
        world.AddButton(
            "Copy World ID",
            WorldService.CopyWorldId,
            "Log the current world ID"
        );
        world.AddButton(
            "World Info",
            WorldService.LogWorldInfo,
            "Log gravity, jump, portals, spawns"
        );
        world.AddButton(
            "Search Worlds",
            WorldService.SearchWorldsKeyboard,
            "Search the world list"
        );
        world.AddButton(
            "Instance Details",
            WorldService.OpenInstanceDetails,
            "Open this instance details page"
        );
    }

    private static void BuildAvatarPage()
    {
        HeavenlyPage avatar = HeavenlyUI.CreatePage("Avatar", "Avatar options");

        avatar.AddLabel("Switching uses CVR's native avatar system.");

        avatar.AddButton(
            "Switch Avatar by ID",
            AvatarService.SwitchByIdKeyboard,
            "Enter an avatar GUID to switch"
        );

        avatar.AddButton(
            "Reload Avatar",
            AvatarService.ReloadCurrent,
            "Re-switch to your current avatar"
        );
        avatar.AddButton(
            "Log My Avatar ID",
            AvatarService.LogCurrentId,
            "Print current avatar ID to console"
        );
        avatar.AddButton(
            "Open in Hub",
            AvatarService.OpenCurrentInHub,
            "Open current avatar in the avatar hub"
        );
        avatar.AddButton(
            "Open Details / Favorite",
            AvatarService.OpenCurrentInDetails,
            "Open current avatar details (favorite natively)"
        );
        avatar.AddButton(
            "Search Avatars",
            AvatarService.SearchKeyboard,
            "Search CVR avatar list"
        );
        avatar.AddButton(
            "Favorite Current",
            AvatarService.FavoriteCurrent,
            "Save current avatar to favorites"
        );
        avatar.AddButton(
            "Favorite by ID...",
            AvatarService.FavoriteByIdKeyboard,
            "Save an avatar ID to favorites"
        );
        avatar.AddButton(
            "My Favorites",
            AvatarService.LogFavorites,
            "List favorite avatars with numbers"
        );
        avatar.AddButton(
            "Wear Favorite #...",
            AvatarService.WearFavoriteIndexKeyboard,
            "Enter a favorite number to wear"
        );
        avatar.AddButton(
            "Random Favorite",
            AvatarService.WearRandomFavorite,
            "Wear a random favorite"
        );
        avatar.AddButton(
            "Set Param...",
            AvatarService.SetParamKeyboard,
            "Set a local avatar float param by name"
        );
        avatar.AddButton(
            "Fire Trigger...",
            AvatarService.FireTriggerKeyboard,
            "Fire a local avatar trigger by name"
        );
        avatar.AddButton(
            "Log Core Params",
            AvatarService.LogCoreParams,
            "Log local avatar core animator state"
        );
        avatar.AddSlider(
            "Gesture Left",
            0f, 1f, 0f,
            AvatarService.SetGestureLeft,
            "Force left hand gesture (tracking fights back)"
        );
        avatar.AddSlider(
            "Gesture Right",
            0f, 1f, 0f,
            AvatarService.SetGestureRight,
            "Force right hand gesture (tracking fights back)"
        );
        avatar.AddButton(
            "Stop Emote",
            AvatarService.StopEmote,
            "Cancel the current emote"
        );
        avatar.AddButton(
            "Log Animator Params",
            AvatarService.LogAnimatorParams,
            "List every animator param with live values"
        );
        avatar.AddSlider(
            "Viseme Test",
            0f, 15f, 0f,
            AvatarService.SetViseme,
            "Force a viseme mouth shape"
        );
        avatar.AddButton(
            "Cycle Outfit Toggle",
            AvatarService.CycleOutfitToggle,
            "Step the avatar outfit toggle 0-8"
        );
        avatar.AddButton(
            "Unfavorite #...",
            AvatarService.UnfavoriteIndexKeyboard,
            "Enter a favorite number to remove"
        );
        avatar.AddButton(
            "Recent Avatars",
            AvatarService.LogHistory,
            "List recently worn avatars"
        );
        avatar.AddButton(
            "Wear Recent #...",
            AvatarService.WearHistoryIndexKeyboard,
            "Enter a recent number to wear"
        );
    }

    private static void BuildVoicePage()
    {
        HeavenlyPage voice = HeavenlyUI.CreatePage("Voice", "Voice options");

        voice.AddLabel("Output volumes via CVR audio. No earrape feature.");

        voice.AddSlider(
            "Master Volume",
            0f, 2f, VoiceService.MasterVolume,
            VoiceService.SetMasterVolume,
            "Master output volume"
        );

        voice.AddSlider(
            "Avatar Volume",
            0f, 2f, VoiceService.AvatarVolume,
            VoiceService.SetAvatarVolume,
            "Other players' voice volume"
        );

        voice.AddSlider(
            "World Volume",
            0f, 2f, VoiceService.WorldVolume,
            VoiceService.SetWorldVolume,
            "World audio volume"
        );

        HeavenlyToggle micToggle = voice.AddToggle(
            "Mute Mic",
            VoiceService.IsMicMuted,
            VoiceService.SetMicMuted,
            "Mute/unmute your microphone"
        );
        ToggleSync.Register("mic", micToggle);
        voice.AddToggle(
            "Deafen",
            false,
            VoiceService.SetDeafened,
            "Mute ALL output (restores volume after)"
        );
    }

    private static void BuildMovementPage()
    {
        HeavenlyPage movement = HeavenlyUI.CreatePage("Movement", "Fly / sit / speed");

        movement.AddLabel("Sit target: Choose Player list, override, or QuickMenu selection.");

        HeavenlyToggle flyToggle = movement.AddToggle(
            "Fly",
            false,
            MovementService.SetFlying,
            "CVR native fly (allows fly even where world disallows)"
        );
        ToggleSync.Register("fly", flyToggle, MovementService.SyncExternalFly);

        HeavenlyToggle noclipToggle = movement.AddToggle(
            "Fly NoClip",
            false,
            MovementService.SetNoClip,
            "Fly through walls"
        );
        ToggleSync.Register("noclip", noclipToggle, MovementService.SyncExternalNoClip);

        movement.AddSlider(
            "Fly Speed",
            0.5f, 10f, Config.HeavenlyConfig.FlySpeed,
            MovementService.ApplyFlySpeed,
            "Flight speed multiplier (CVR world + controller)"
        );

        movement.AddSlider(
            "Jump",
            0.25f, 3f, Config.HeavenlyConfig.JumpMult,
            MovementService.SetJumpMult,
            "Jump impulse multiplier"
        );

        movement.AddSlider(
            "Multi-Jump",
            1f, 5f, Config.HeavenlyConfig.JumpCount,
            MovementService.SetJumpCount,
            "Air jumps allowed (1 = normal)"
        );

        movement.AddToggle(
            "Bunny Hop",
            MovementService.BunnyHop,
            MovementService.SetBunnyHop,
            "Auto-jump on every landing"
        );

        movement.AddSlider(
            "Walk Speed",
            0.25f, 3f, Config.HeavenlyConfig.WalkSpeed,
            MovementService.SetWalkSpeed,
            "Walk speed multiplier"
        );

        movement.AddSlider(
            "Sprint",
            0.25f, 3f, Config.HeavenlyConfig.SprintMult,
            MovementService.SetSprintMult,
            "Sprint multiplier"
        );

        movement.AddSlider(
            "Camera FOV",
            40f, 120f, CameraService.CurrentFov(),
            CameraService.SetFov,
            "Field of view (saved once moved)"
        );

        movement.AddLabel("Spots (same world only):");
        movement.AddButton("Save Spot 1", () => BookmarkService.SaveSlot(0), "Remember this position");
        movement.AddButton("Save Spot 2", () => BookmarkService.SaveSlot(1), "Remember this position");
        movement.AddButton("Save Spot 3", () => BookmarkService.SaveSlot(2), "Remember this position");
        movement.AddButton("Go Spot 1", () => BookmarkService.GoSlot(0), "Teleport to saved position");
        movement.AddButton("Go Spot 2", () => BookmarkService.GoSlot(1), "Teleport to saved position");
        movement.AddButton("Go Spot 3", () => BookmarkService.GoSlot(2), "Teleport to saved position");

        movement.AddButton(
            "Play Emote #...",
            MovementService.PlayEmoteKeyboard,
            "Enter an emote number to play"
        );

        movement.AddToggle(
            "Crouch",
            MovementService.IsCrouched,
            MovementService.SetCrouch,
            "Sticky crouch"
        );

        movement.AddToggle(
            "Prone",
            MovementService.IsProne,
            MovementService.SetProne,
            "Sticky prone"
        );

        movement.AddButton(
            "Sit on Player",
            MovementService.SitOnSelectedPlayer,
            "Snap to selected player's bone every frame"
        );
        movement.AddButton(
            "Stand Up",
            MovementService.StandUp,
            "Stop sitting on player"
        );

        movement.AddSlider(
            "Sit Offset",
            0f, 2f, Config.HeavenlyConfig.SitOffset,
            MovementService.SetSitOffset,
            "Extra height above the sit bone"
        );

        movement.AddLabel("Sit bone:");
        movement.AddButton("Sit: Head", () => SetSitBone(UnityEngine.HumanBodyBones.Head, "Head"));
        movement.AddButton("Sit: Hips", () => SetSitBone(UnityEngine.HumanBodyBones.Hips, "Hips"));
        movement.AddButton("Sit: Left Hand", () => SetSitBone(UnityEngine.HumanBodyBones.LeftHand, "LeftHand"));
        movement.AddButton("Sit: Right Hand", () => SetSitBone(UnityEngine.HumanBodyBones.RightHand, "RightHand"));
        movement.AddButton("Sit: Left Foot", () => SetSitBone(UnityEngine.HumanBodyBones.LeftFoot, "LeftFoot"));
        movement.AddButton("Sit: Right Foot", () => SetSitBone(UnityEngine.HumanBodyBones.RightFoot, "RightFoot"));

        movement.AddButton(
            "Respawn",
            MovementService.Respawn,
            "Respawn at world spawn"
        );
        movement.AddButton(
            "Go to Spawn",
            MovementService.TeleportToSpawn,
            "Teleport to the world spawn point"
        );
        movement.AddButton(
            "Stop Motion",
            MovementService.StopMotion,
            "Zero all movement forces"
        );
        movement.AddButton(
            "Nudge Up",
            MovementService.NudgeUp,
            "Teleport 1m straight up (unstick)"
        );
        movement.AddButton(
            "Log Position",
            MovementService.LogPosition,
            "Print your coordinates to console"
        );
        movement.AddButton(
            "Blink 5m",
            () => MovementService.Blink(5f),
            "Teleport 5m forward"
        );
        movement.AddButton(
            "Dash 6m",
            () => MovementService.Dash(6f),
            "Teleport along your view (even up/down)"
        );

        movement.AddToggle(
            "Flashlight",
            false,
            FlashlightService.SetEnabled,
            "Headlamp following your view"
        );
        movement.AddToggle(
            "Mirror",
            false,
            MovementService.SetMirror,
            "Personal mirror 2m ahead (game prefab)"
        );
        movement.AddSlider(
            "Flash Angle",
            10f, 120f, Config.HeavenlyConfig.FlashAngle,
            FlashlightService.SetAngle,
            "Spotlight cone angle"
        );
        movement.AddSlider(
            "Flash Range",
            5f, 60f, Config.HeavenlyConfig.FlashRange,
            FlashlightService.SetRange,
            "Spotlight distance"
        );
        movement.AddSlider(
            "Flash Intensity",
            0f, 8f, Config.HeavenlyConfig.FlashIntensity,
            FlashlightService.SetIntensity,
            "Spotlight brightness"
        );
    }

    private static void SetSitBone(UnityEngine.HumanBodyBones bone, string label)
    {
        MovementService.SitBone = bone;
        HeavenlyAPI.Toast($"[Heavenly] Sit bone -> {label}");
    }

    private static void BuildEspPage()
    {
        HeavenlyPage esp = HeavenlyUI.CreatePage("ESP", "ESP marker options");

        esp.AddLabel("X-ray outlines via HighlightPlus. Nothing added to avatars.");

        esp.AddToggle("ESP Master", EspService.MasterEnabled, EspService.SetMaster);
        esp.AddToggle("Player ESP", EspService.PlayerEsp, EspService.SetPlayers);
        esp.AddToggle("Nameplate ESP", EspService.NameplateEsp, EspService.SetNameplates);
        esp.AddToggle("Pickup ESP", EspService.PickupEsp, EspService.SetPickups);
        esp.AddToggle("Spawnable ESP", EspService.SpawnableEsp, EspService.SetSpawnables);
        esp.AddToggle("Hide Nameplates", EspService.HideNameplatesEnabled, EspService.SetHideNameplates);
        esp.AddSlider(
            "Outline Width",
            0.1f, 2f, Config.HeavenlyConfig.EspPlayerScale,
            EspService.SetOutlineWidth,
            "ESP outline thickness"
        );
        esp.AddToggle(
            "Custom Color",
            Config.HeavenlyConfig.EspUseCustom,
            EspService.SetCustomColorEnabled,
            "One color for all outlines (overrides ranks)"
        );
        esp.AddSlider("Color R", 0f, 255f, Config.HeavenlyConfig.EspCustomR,
            v =>
            {
                EspService.SetCustomColor((int)v, Config.HeavenlyConfig.EspCustomG, Config.HeavenlyConfig.EspCustomB);
            }, "Red 0-255");
        esp.AddSlider("Color G", 0f, 255f, Config.HeavenlyConfig.EspCustomG,
            v =>
            {
                EspService.SetCustomColor(Config.HeavenlyConfig.EspCustomR, (int)v, Config.HeavenlyConfig.EspCustomB);
            }, "Green 0-255");
        esp.AddSlider("Color B", 0f, 255f, Config.HeavenlyConfig.EspCustomB,
            v =>
            {
                EspService.SetCustomColor(Config.HeavenlyConfig.EspCustomR, Config.HeavenlyConfig.EspCustomG, (int)v);
            }, "Blue 0-255");
        esp.AddButton(
            "Diagnose ESP",
            EspService.Diagnose,
            "Log players, ranks, and outline state"
        );
    }

    private static void BuildSocialPage()
    {
        HeavenlyPage social = HeavenlyUI.CreatePage("Social", "Player actions");

        social.AddLabel("Target: Choose Player list, override, or QuickMenu selection.");
        social.AddLabel("Replaces HeavenlyVRC's custom Tag-Along websocket (server is dead).");

        social.AddButton(
            "Choose Player...",
            PlayerTarget.ChooseViaSelector,
            "Pick a target from CVR's player list"
        );
        social.AddButton(
            "Clear Target",
            PlayerTarget.ClearOverride,
            "Forget the picked target"
        );
        social.AddButton(
            "Teleport to Player",
            SocialService.TeleportToSelectedPlayer,
            "Teleport next to target player"
        );
        social.AddButton("Invite to Instance", SocialService.InviteSelected);
        social.AddButton("Request Invite", SocialService.RequestInviteSelected);
        social.AddButton("Friend Request", SocialService.SendFriendRequestSelected);
        social.AddButton("Unfriend Target", SocialService.UnfriendSelected);
        social.AddButton(
            "Friend Requests",
            SocialService.LogFriendRequests,
            "Fetch and list pending requests"
        );
        social.AddButton(
            "Accept Request #...",
            SocialService.AcceptRequestIndexKeyboard,
            "Accept a listed request by number"
        );
        social.AddButton(
            "Deny Request #...",
            SocialService.DenyRequestIndexKeyboard,
            "Deny a listed request by number"
        );
        social.AddButton("Block", SocialService.BlockSelected);
        social.AddButton("Unblock", SocialService.UnblockSelected);
        social.AddButton("Hide Avatar", SocialService.HideAvatarSelected);
        social.AddButton("Show Avatar", SocialService.ShowAvatarSelected);
        social.AddButton(
            "Wear Target Avatar",
            SocialService.WearTargetAvatar,
            "Wear target's avatar (CVR enforces permissions)"
        );
        social.AddButton(
            "Avatar Details",
            SocialService.OpenSelectedAvatarDetails,
            "Open target avatar details page"
        );
        social.AddButton(
            "Log Avatar ID",
            SocialService.LogSelectedAvatarId,
            "Log target avatar ID to console"
        );
        social.AddButton(
            "Log User ID",
            SocialService.LogSelectedUserId,
            "Log target user ID to console"
        );
        social.AddButton(
            "Note Player...",
            PlayerNotes.NoteSelected,
            "Save a local note (empty clears)"
        );
        social.AddButton(
            "Show Note",
            PlayerNotes.ShowSelectedNote,
            "Show the saved note for target"
        );
        social.AddButton(
            "Face Target",
            SocialService.FaceTarget,
            "Turn to face the target"
        );
        social.AddButton(
            "Target Info",
            SocialService.LogTargetInfo,
            "Log everything known about target"
        );
        social.AddButton(
            "Copy Target Info",
            SocialService.CopyTargetInfo,
            "Copy target info to clipboard"
        );
        social.AddButton(
            "ChatBox...",
            SocialService.ChatBoxKeyboard,
            "Send a chatbox message as yourself"
        );
        social.AddToggle(
            "Mute Voice (saved)",
            false,
            SocialService.SetTargetMuted,
            "Persistently mute target's voice"
        );
        social.AddSlider(
            "Target Volume",
            0f, 2f, 1f,
            SocialService.SetTargetVolume,
            "Persistently set target's voice volume"
        );
        social.AddToggle(
            "Hide Avatar (saved)",
            false,
            SocialService.SetTargetAvatarHidden,
            "Persistently hide target's avatar"
        );
        social.AddToggle(
            "Hide Props (saved)",
            false,
            SocialService.SetTargetPropsHidden,
            "Persistently hide target's props"
        );
        social.AddToggle(
            "Normalize Voice (saved)",
            false,
            SocialService.SetTargetNormalized,
            "Persistently normalize target's voice"
        );
        social.AddToggle(
            "Hide ChatBox (saved)",
            false,
            SocialService.SetTargetChatBoxHidden,
            "Persistently hide target's chatbox"
        );
    }

    private static void BuildProtectionPage()
    {
        HeavenlyPage protection = HeavenlyUI.CreatePage("Protection", "Safety options");

        protection.AddLabel("QoL safety only. No crash / desync / serialize abuse.");

        protection.AddToggle(
            "Hide All Avatars",
            ProtectionService.HideAvatarsEnabled,
            ProtectionService.SetHideAvatars,
            "Hide remote avatars (restores on rejoin)"
        );

        protection.AddToggle(
            "Block Portals + Props",
            ProtectionService.BlockPortalsPropsEnabled,
            ProtectionService.SetBlockPortalsProps,
            "Disable portals/spawnables for this world"
        );

        protection.AddToggle(
            "Safe Shaders",
            ProtectionService.SafeShadersEnabled,
            ProtectionService.SetSafeShaders,
            "Replace remote avatar shaders with Standard (local-only)"
        );

        protection.AddToggle(
            "Limit Avatar Lights",
            ProtectionService.LimitLightsEnabled,
            ProtectionService.SetLimitLights,
            "Remove excess lights on remote avatars"
        );

        protection.AddToggle(
            "Limit Materials",
            ProtectionService.LimitMaterialsEnabled,
            ProtectionService.SetLimitMaterials,
            "Remove renderers beyond the per-avatar cap"
        );

        protection.AddToggle(
            "Limit Poly",
            ProtectionService.LimitPolyEnabled,
            ProtectionService.SetLimitPoly,
            "Remove meshes beyond the vertex cap"
        );

        protection.AddToggle(
            "Auto-hide Laggy",
            ProtectionService.AutoHideLaggyEnabled,
            ProtectionService.SetAutoHideLaggy,
            "Hide avatars beyond 2x scan caps (reversible)"
        );

        protection.AddToggle(
            "Limit Particles",
            ProtectionService.LimitParticlesEnabled,
            ProtectionService.SetLimitParticles,
            "Remove particle systems beyond the cap"
        );

        protection.AddToggle(
            "Limit Audio",
            ProtectionService.LimitAudioEnabled,
            ProtectionService.SetLimitAudio,
            "Remove audio sources beyond the cap"
        );
        protection.AddToggle(
            "Mute Avatar Audio",
            ProtectionService.MuteAvatarAudioEnabled,
            ProtectionService.SetMuteAvatarAudio,
            "Remove ALL avatar audio (voice chat unaffected)"
        );
        protection.AddToggle(
            "Auto Rescan",
            ProtectionService.AutoRescanEnabled,
            ProtectionService.SetAutoRescan,
            "Re-run safety scan every 60s"
        );

        protection.AddToggle(
            "Limit Constraints",
            ProtectionService.LimitConstraintsEnabled,
            ProtectionService.SetLimitConstraints,
            "Remove constraint components beyond the cap"
        );
        protection.AddToggle(
            "Limit Colliders",
            ProtectionService.LimitCollidersEnabled,
            ProtectionService.SetLimitColliders,
            "Remove collider components beyond the cap"
        );
        protection.AddToggle(
            "Limit Rigidbodies",
            ProtectionService.LimitRigidbodiesEnabled,
            ProtectionService.SetLimitRigidbodies,
            "Remove rigidbodies beyond the cap"
        );
        protection.AddToggle(
            "Limit Joints",
            ProtectionService.LimitJointsEnabled,
            ProtectionService.SetLimitJoints,
            "Remove joints beyond the cap"
        );
        protection.AddToggle(
            "Limit Trails",
            ProtectionService.LimitTrailsEnabled,
            ProtectionService.SetLimitTrails,
            "Remove line/trail renderers beyond the cap"
        );
        protection.AddToggle(
            "Strip Avatar Cameras",
            ProtectionService.StripCamerasEnabled,
            ProtectionService.SetStripCameras,
            "Remove camera components from avatars"
        );
        protection.AddToggle(
            "Limit Cloth Sims",
            ProtectionService.LimitClothEnabled,
            ProtectionService.SetLimitCloth,
            "Remove MagicaCloth sims beyond the cap"
        );
        protection.AddButton(
            "Remove Portals",
            ProtectionService.RemovePortals,
            "Delete world portals locally (back on reload)"
        );

        protection.AddToggle(
            "Limit Video Players",
            ProtectionService.LimitVideosEnabled,
            ProtectionService.SetLimitVideos,
            "Remove video players beyond the cap"
        );
        protection.AddToggle(
            "Limit Blendshapes",
            ProtectionService.LimitBlendshapesEnabled,
            ProtectionService.SetLimitBlendshapes,
            "Remove blendshape-heavy meshes beyond the cap"
        );
        protection.AddToggle(
            "Limit Contacts",
            ProtectionService.LimitContactsEnabled,
            ProtectionService.SetLimitContacts,
            "Remove contact senders/receivers beyond the cap"
        );
        protection.AddButton(
            "Block Avatar ID...",
            ProtectionService.BlockAvatarByIdKeyboard,
            "Persistently hide an avatar by ID"
        );
        protection.AddButton(
            "Unblock Avatar ID...",
            ProtectionService.UnblockAvatarByIdKeyboard,
            "Restore a blocked avatar by ID"
        );
        protection.AddButton(
            "Blocked Avatars",
            ProtectionService.LogBlockedAvatars,
            "List blocked avatar IDs"
        );

        protection.AddToggle(
            "Skip Friends in Scans",
            ProtectionService.SkipFriends,
            ProtectionService.SetSkipFriends,
            "Leave friends' avatars untouched"
        );

        HeavenlyToggle cullingToggle = protection.AddToggle(
            "Distance Culling",
            ProtectionService.DistanceCullingEnabled,
            ProtectionService.SetDistanceCulling,
            "Hide avatars beyond a distance (native CVR)"
        );
        ToggleSync.Register("culling", cullingToggle);
        protection.AddSlider(
            "Culling Distance",
            5f, 200f, ProtectionService.CullingDistance,
            ProtectionService.SetCullingDistance,
            "Meters before avatars hide"
        );
        protection.AddToggle(
            "Culling Friends Only",
            ProtectionService.CullingFilterFriends,
            ProtectionService.SetCullingFriendsOnly,
            "Only cull non-friends"
        );

        protection.AddToggle(
            "Safety Net",
            ProtectionService.SafetyNetEnabled,
            ProtectionService.SetSafetyNet,
            "Auto-respawn on void fall (not while flying)"
        );

        protection.AddToggle(
            "Block Pickups",
            ProtectionService.BlockPickupsEnabled,
            ProtectionService.SetBlockPickups,
            "Disable all pickup interactions (local-only)"
        );
        protection.AddButton(
            "Audit World",
            ProtectionService.AuditWorld,
            "Count scene lights/particles/audio/renderers"
        );
        protection.AddButton(
            "Protection Status",
            ProtectionService.LogStatus,
            "Log every protection flag"
        );
        protection.AddButton(
            "Audit Target Avatar",
            ProtectionService.AuditSelectedAvatar,
            "Log target avatar component counts (read-only)"
        );
        protection.AddButton(
            "Audit My Avatar",
            ProtectionService.AuditMyAvatar,
            "Log your avatar component counts (read-only)"
        );
        protection.AddButton(
            "Scan Avatars Now",
            ProtectionService.ScanNow,
            "Run safety scan over loaded remote avatars"
        );

        protection.AddButton(
            "Panic Button",
            ProtectionService.Panic,
            "Stop tricks + restore safe avatar"
        );
    }

    private static void BuildDebugPage()
    {
        HeavenlyPage debug = HeavenlyUI.CreatePage("Debug", "Logs and status");

        debug.AddLabel($"HeavenlyCVR v{HeavenlyAPI.Version} by {HeavenlyAPI.Author}");
        debug.AddLabel("Join/leave feed auto-logs to console.");

        HeavenlyLabel feedLabel = debug.AddLabel("Feed starting...");
        DebugService.SetFeedLabel(feedLabel);

        debug.AddButton(
            "Test Toast",
            () => HeavenlyAPI.Toast("[Heavenly] UI is working."),
            "Shows a QuickMenu toast"
        );

        debug.AddButton(
            "Log Status",
            DebugService.LogStatus,
            "Log joins, players, world / instance"
        );

        debug.AddButton(
            "Log Roster",
            DebugService.LogRoster,
            "Log every player in this instance"
        );

        debug.AddToggle(
            "Join Toasts",
            DebugService.JoinToasts,
            DebugService.SetJoinToasts,
            "Toast on player join/leave"
        );
        debug.AddButton(
            "Copy Status",
            DebugService.CopyStatus,
            "Copy status snapshot to clipboard"
        );
        debug.AddButton(
            "Reload Menus",
            DebugService.ReloadMenus,
            "Reload the game menus (fixes stuck UI)"
        );
        debug.AddButton(
            "Quit Game",
            DebugService.QuitGame,
            "Close ChilloutVR"
        );
    }

    private static void BuildKeysPage()
    {
        HeavenlyPage keys = HeavenlyUI.CreatePage("Keys", "Rebindable shortcuts");

        keys.AddLabel("All shortcuts use LeftControl + key.");
        keys.AddLabel("Press Rebind, then any key (Esc cancels).");

        foreach (Features.Input.KeybindService.HeavenlyAction action in Enum.GetValues(typeof(Features.Input.KeybindService.HeavenlyAction)))
        {
            Features.Input.KeybindService.HeavenlyAction captured = action;
            keys.AddButton(
                $"Rebind {Features.Input.KeybindService.Label(captured)} (now {Features.Input.KeybindService.BoundKey(captured)})",
                () => Features.Input.KeybindService.StartRebind(captured),
                $"Rebind Ctrl+ key for {Features.Input.KeybindService.Label(captured)}"
            );
        }
    }

    private static void BuildConfigPage()
    {
        HeavenlyPage config = HeavenlyUI.CreatePage("Config", "Every tunable, persisted");

        config.AddLabel("Saved to MelonPreferences (HeavenlyCVR category).");

        config.AddLabel("── ESP timing ──");
        config.AddSlider("Player Refresh (s)", 1f, 10f, Config.HeavenlyConfig.EspPlayerRefresh,
            v => Config.HeavenlyConfig.EspPlayerRefresh = v, "How often player markers refresh");
        config.AddSlider("Prop Refresh (s)", 1f, 10f, Config.HeavenlyConfig.EspPropRefresh,
            v => Config.HeavenlyConfig.EspPropRefresh = v, "How often prop markers refresh");
        config.AddSlider("Nameplate Refresh (s)", 1f, 15f, Config.HeavenlyConfig.EspNameplateRefresh,
            v => Config.HeavenlyConfig.EspNameplateRefresh = v, "How often nameplate overlay re-applies");

        config.AddLabel("── ESP sizes ──");
        config.AddSlider("Prop Max Markers", 5f, 100f, Config.HeavenlyConfig.EspPropMax,
            v => Config.HeavenlyConfig.EspPropMax = (int)v, "Cap on pickup/spawnable markers");
        config.AddSlider("Outline Max Players", 5f, 50f, Config.HeavenlyConfig.EspOutlineMax,
            v => Config.HeavenlyConfig.EspOutlineMax = (int)v, "Nearest players outlined");
        config.AddSlider("Prop Marker Size", 0.1f, 1f, Config.HeavenlyConfig.EspPropSize,
            v => Config.HeavenlyConfig.EspPropSize = v, "Pickup/spawnable marker size");

        config.AddLabel("── ESP colors (RGB) ──");
        RgbSliders(config, "Default", () => (Config.HeavenlyConfig.EspColDefaultR, Config.HeavenlyConfig.EspColDefaultG, Config.HeavenlyConfig.EspColDefaultB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorDefault(r, g, b));
        RgbSliders(config, "Friend", () => (Config.HeavenlyConfig.EspColFriendR, Config.HeavenlyConfig.EspColFriendG, Config.HeavenlyConfig.EspColFriendB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorFriend(r, g, b));
        RgbSliders(config, "Legend", () => (Config.HeavenlyConfig.EspColLegendR, Config.HeavenlyConfig.EspColLegendG, Config.HeavenlyConfig.EspColLegendB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorLegend(r, g, b));
        RgbSliders(config, "Guide", () => (Config.HeavenlyConfig.EspColGuideR, Config.HeavenlyConfig.EspColGuideG, Config.HeavenlyConfig.EspColGuideB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorGuide(r, g, b));
        RgbSliders(config, "Moderator", () => (Config.HeavenlyConfig.EspColModR, Config.HeavenlyConfig.EspColModG, Config.HeavenlyConfig.EspColModB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorMod(r, g, b));
        RgbSliders(config, "Developer", () => (Config.HeavenlyConfig.EspColDevR, Config.HeavenlyConfig.EspColDevG, Config.HeavenlyConfig.EspColDevB),
            (r, g, b) => Config.HeavenlyConfig.SetEspColorDev(r, g, b));

        config.AddLabel("── Protection ──");
        config.AddSlider("Max Lights / Avatar", 1f, 20f, Config.HeavenlyConfig.ProtMaxLights,
            v => Config.HeavenlyConfig.ProtMaxLights = (int)v, "Light limiter cap");
        config.AddSlider("Auto-scan Delay (s)", 5f, 60f, Config.HeavenlyConfig.ProtScanDelay,
            v => Config.HeavenlyConfig.ProtScanDelay = v, "Delay before scanning joiners");

        config.AddSlider("Max Renderers / Avatar", 10f, 200f, Config.HeavenlyConfig.ProtMaxMaterials,
            v => Config.HeavenlyConfig.ProtMaxMaterials = (int)v, "Material limiter cap");
        config.AddSlider("Max Verts / Mesh", 100000f, 5000000f, Config.HeavenlyConfig.ProtMaxVerts,
            v => Config.HeavenlyConfig.ProtMaxVerts = (int)v, "Poly limiter cap");
        config.AddSlider("Max Particles / Avatar", 10f, 500f, Config.HeavenlyConfig.ProtMaxParticles,
            v => Config.HeavenlyConfig.ProtMaxParticles = (int)v, "Particle limiter cap");
        config.AddSlider("Max Audio / Avatar", 2f, 64f, Config.HeavenlyConfig.ProtMaxAudio,
            v => Config.HeavenlyConfig.ProtMaxAudio = (int)v, "Audio limiter cap");
        config.AddSlider("Max Constraints / Avatar", 10f, 1000f, Config.HeavenlyConfig.ProtMaxConstraints,
            v => Config.HeavenlyConfig.ProtMaxConstraints = (int)v, "Constraint limiter cap");
        config.AddSlider("Max Colliders / Avatar", 20f, 2000f, Config.HeavenlyConfig.ProtMaxColliders,
            v => Config.HeavenlyConfig.ProtMaxColliders = (int)v, "Collider limiter cap");
        config.AddSlider("Max Rigidbodies / Avatar", 5f, 200f, Config.HeavenlyConfig.ProtMaxRigidbodies,
            v => Config.HeavenlyConfig.ProtMaxRigidbodies = (int)v, "Rigidbody limiter cap");
        config.AddSlider("Max Joints / Avatar", 5f, 200f, Config.HeavenlyConfig.ProtMaxJoints,
            v => Config.HeavenlyConfig.ProtMaxJoints = (int)v, "Joint limiter cap");
        config.AddSlider("Max Trails / Avatar", 10f, 500f, Config.HeavenlyConfig.ProtMaxTrails,
            v => Config.HeavenlyConfig.ProtMaxTrails = (int)v, "Trail limiter cap");
        config.AddSlider("Max Cloth / Avatar", 2f, 100f, Config.HeavenlyConfig.ProtMaxCloth,
            v => Config.HeavenlyConfig.ProtMaxCloth = (int)v, "Cloth limiter cap");
        config.AddSlider("Max Videos / Avatar", 0f, 8f, Config.HeavenlyConfig.ProtMaxVideos,
            v => Config.HeavenlyConfig.ProtMaxVideos = (int)v, "Video limiter cap");
        config.AddSlider("Max Blendshapes / Avatar", 200f, 10000f, Config.HeavenlyConfig.ProtMaxBlendshapes,
            v => Config.HeavenlyConfig.ProtMaxBlendshapes = (int)v, "Blendshape limiter cap");
        config.AddSlider("Max Contacts / Avatar", 8f, 512f, Config.HeavenlyConfig.ProtMaxContacts,
            v => Config.HeavenlyConfig.ProtMaxContacts = (int)v, "Contact limiter cap");

        config.AddLabel("── Social ──");
        config.AddSlider("Teleport Right", 0f, 5f, Config.HeavenlyConfig.TeleportRight,
            v => Config.HeavenlyConfig.TeleportRight = v, "Sideways teleport offset");
        config.AddSlider("Teleport Up", 0f, 3f, Config.HeavenlyConfig.TeleportUp,
            v => Config.HeavenlyConfig.TeleportUp = v, "Up teleport offset");

        config.AddLabel("── Movement ──");
        config.AddSlider("Sit Timeout (s)", 1f, 15f, Config.HeavenlyConfig.SitTimeoutSec,
            v => Config.HeavenlyConfig.SitTimeoutSec = v, "Give up sitting after target is gone this long");

        config.AddLabel("── Debug ──");
        config.AddSlider("Feed Lines Kept", 10f, 100f, Config.HeavenlyConfig.FeedLines,
            v => Config.HeavenlyConfig.FeedLines = (int)v, "Join/leave feed history size");

        config.AddLabel("── Performance ──");
        config.AddSlider("FPS Cap (0 = off)", 0f, 240f, Config.HeavenlyConfig.FpsCap,
            v => DebugService.SetFpsCap(v), "Max frames per second");

        config.AddButton(
            "Reset All Defaults",
            Config.HeavenlyConfig.ResetDefaults,
            "Restore every setting to its default"
        );
    }

    private static void RgbSliders(
        HeavenlyPage page,
        string label,
        Func<(int R, int G, int B)> get,
        Action<int, int, int> set)
    {
        (int R, int G, int B) current = get();
        page.AddLabel($"{label} color:");
        page.AddSlider($"{label} R", 0f, 255f, current.R,
            v => { (int R, int G, int B) c = get(); set((int)v, c.G, c.B); }, "Red 0-255");
        page.AddSlider($"{label} G", 0f, 255f, current.G,
            v => { (int R, int G, int B) c = get(); set(c.R, (int)v, c.B); }, "Green 0-255");
        page.AddSlider($"{label} B", 0f, 255f, current.B,
            v => { (int R, int G, int B) c = get(); set(c.R, c.G, (int)v); }, "Blue 0-255");
    }

    private static void BuildTagsPage()
    {
        HeavenlyPage tags = HeavenlyUI.CreatePage("Tags", "Custom nameplate tags");

        tags.AddLabel("Your own plates (self-tags are shared).");
        tags.AddLabel("Rich text works (<color=red>hi</color>).");

        for (int i = 0; i < TagService.SlotCount; i++)
            BuildTagSlot(tags, i);

        tags.AddToggle(
            "Show Players' Tags",
            TagService.ShowRemoteTags,
            TagService.SetShowRemoteTags,
            "Display tags shared by other Heavenly users"
        );
        tags.AddButton(
            "Clear All Tags",
            TagService.ClearAll,
            "Remove every custom tag"
        );

        tags.AddSlider(
            "Tag Height",
            -0.5f, 0.3f, Config.HeavenlyConfig.TagBaseY,
            v => Config.HeavenlyConfig.TagBaseY = v,
            "Up/down for all tag plates (applies live)"
        );
        tags.AddSlider(
            "Tag Spacing",
            0.02f, 0.25f, Config.HeavenlyConfig.TagGap,
            v => Config.HeavenlyConfig.TagGap = v,
            "Gap between stacked tag plates"
        );
        tags.AddButton(
            "Diagnose Nameplates",
            TagService.Diagnose,
            "Log the real nameplate hierarchy to console"
        );
    }

    private static void BuildTagSlot(HeavenlyPage tags, int slot)
    {
        int captured = slot;
        TagRecord tag = TagService.Slot(captured);

        HeavenlyPage page = tags.AddSubPage(
            $"Tag {captured + 1}",
            string.IsNullOrEmpty(tag.Text) ? "Empty slot" : tag.Text
        );

        page.AddToggle(
            "Enabled",
            tag.Enabled,
            enabled => TagService.SetEnabled(captured, enabled),
            "Show this tag"
        );

        page.AddButton(
            "Set Text...",
            () => TagService.EditText(captured),
            "Tag text (rich text allowed)"
        );

        page.AddLabel($"Target: {(string.IsNullOrEmpty(tag.TargetName) ? "none" : tag.TargetName)}");
        page.AddButton("Target: Self", () => TagService.TargetSelf(captured), "Tag yourself");
        page.AddButton("Target: Choose...", () => TagService.TargetChoose(captured), "Pick from player list");
        page.AddButton("Target: By ID...", () => TagService.TargetById(captured), "Paste a user ID");

        page.AddSlider("Color R", 0f, 255f, tag.R,
            v => TagService.SetColor(captured, (int)v, TagService.Slot(captured).G, TagService.Slot(captured).B), "Red 0-255");
        page.AddSlider("Color G", 0f, 255f, tag.G,
            v => TagService.SetColor(captured, TagService.Slot(captured).R, (int)v, TagService.Slot(captured).B), "Green 0-255");
        page.AddSlider("Color B", 0f, 255f, tag.B,
            v => TagService.SetColor(captured, TagService.Slot(captured).R, TagService.Slot(captured).G, (int)v), "Blue 0-255");

        page.AddButton(
            "Clear Tag",
            () => TagService.ClearSlot(captured),
            "Empty this slot"
        );
    }
}
