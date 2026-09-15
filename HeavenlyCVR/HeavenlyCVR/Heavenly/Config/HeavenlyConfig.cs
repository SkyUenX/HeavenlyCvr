using System;
using MelonLoader;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Config;

/// <summary>
/// All user tunables. Persisted to MelonPreferences (UserData/MelonPreferences.cfg,
/// category "HeavenlyCVR") so settings survive restarts. Writes are throttled:
/// setters mark dirty, <see cref="Flush"/> (called every frame from the plugin)
/// saves at most every few seconds.
/// </summary>
public static class HeavenlyConfig
{
    private static MelonPreferences_Category? _cat;
    private static bool _initialized;
    private static bool _dirty;
    private static int _lastSaveTick;
    private const int SaveThrottleMs = 3000;

    private static readonly System.Collections.Generic.List<Action> _resetters = new();

    // ESP timings / sizes
    private static MelonPreferences_Entry<float>? _espPlayerRefresh;
    private static MelonPreferences_Entry<float>? _espPropRefresh;
    private static MelonPreferences_Entry<float>? _espNameplateRefresh;
    private static MelonPreferences_Entry<int>? _espPropMax;
    private static MelonPreferences_Entry<int>? _espOutlineMax;
    private static MelonPreferences_Entry<bool>? _espUseCustom;
    private static MelonPreferences_Entry<int>? _espCustomR, _espCustomG, _espCustomB;
    private static MelonPreferences_Entry<float>? _espPlayerScale;
    private static MelonPreferences_Entry<float>? _espPropSize;

    // ESP default states
    private static MelonPreferences_Entry<bool>? _espMaster;
    private static MelonPreferences_Entry<bool>? _espPlayers;
    private static MelonPreferences_Entry<bool>? _espPickups;
    private static MelonPreferences_Entry<bool>? _espSpawnables;
    private static MelonPreferences_Entry<bool>? _espNameplates;
    private static MelonPreferences_Entry<bool>? _espHideNameplates;

    // ESP colors (RGB 0-255)
    private static MelonPreferences_Entry<int>? _colDefaultR, _colDefaultG, _colDefaultB;
    private static MelonPreferences_Entry<int>? _colFriendR, _colFriendG, _colFriendB;
    private static MelonPreferences_Entry<int>? _colLegendR, _colLegendG, _colLegendB;
    private static MelonPreferences_Entry<int>? _colGuideR, _colGuideG, _colGuideB;
    private static MelonPreferences_Entry<int>? _colModR, _colModG, _colModB;
    private static MelonPreferences_Entry<int>? _colDevR, _colDevG, _colDevB;

    // Movement
    private static MelonPreferences_Entry<float>? _flySpeed;
    private static MelonPreferences_Entry<int>? _sitBone;
    private static MelonPreferences_Entry<float>? _sitTimeoutSec;
    private static MelonPreferences_Entry<float>? _sitOffset;

    // Protection
    private static MelonPreferences_Entry<bool>? _protHideAvatars;
    private static MelonPreferences_Entry<bool>? _protBlockPortals;
    private static MelonPreferences_Entry<bool>? _protSafeShaders;
    private static MelonPreferences_Entry<bool>? _protLimitLights;
    private static MelonPreferences_Entry<bool>? _protSkipFriends;
    private static MelonPreferences_Entry<int>? _protMaxLights;
    private static MelonPreferences_Entry<float>? _protScanDelay;

    // Social
    private static MelonPreferences_Entry<float>? _teleportRight;
    private static MelonPreferences_Entry<float>? _teleportUp;

    // Voice
    private static MelonPreferences_Entry<float>? _volMaster;
    private static MelonPreferences_Entry<float>? _volAvatar;
    private static MelonPreferences_Entry<float>? _volWorld;

    // Debug
    private static MelonPreferences_Entry<int>? _feedLines;

    // Performance (0 = off)
    private static MelonPreferences_Entry<int>? _fpsCap;

    // Tags (JSON blob, managed by TagService)
    private static MelonPreferences_Entry<string>? _tagsJson;
    private static MelonPreferences_Entry<float>? _tagBaseY;
    private static MelonPreferences_Entry<float>? _tagGap;

    // Notes (JSON blob, managed by PlayerNotes)
    private static MelonPreferences_Entry<string>? _playerNotesJson;

    // History / favorites (JSON blobs, managed by World/Avatar services)
    private static MelonPreferences_Entry<string>? _worldHistoryJson;
    private static MelonPreferences_Entry<string>? _avatarHistoryJson;
    private static MelonPreferences_Entry<string>? _avatarFavoritesJson;

    // Keybinds (KeyCode ints, always combined with LeftControl)
    private static MelonPreferences_Entry<int>? _keyFly;
    private static MelonPreferences_Entry<int>? _keyRejoin;
    private static MelonPreferences_Entry<int>? _keyNoClip;
    private static MelonPreferences_Entry<int>? _keyStand;
    private static MelonPreferences_Entry<int>? _keyPanic;
    private static MelonPreferences_Entry<int>? _keyRespawn;
    private static MelonPreferences_Entry<int>? _keyReloadAvatar;
    private static MelonPreferences_Entry<int>? _keyEsp;
    private static MelonPreferences_Entry<int>? _keyFlashlight;
    private static MelonPreferences_Entry<bool>? _protSafetyNet;
    private static MelonPreferences_Entry<bool>? _dbgJoinToasts;

    // Movement multipliers
    private static MelonPreferences_Entry<float>? _walkSpeed;
    private static MelonPreferences_Entry<float>? _sprintMult;
    private static MelonPreferences_Entry<float>? _jumpMult;
    private static MelonPreferences_Entry<int>? _jumpCount;
    private static MelonPreferences_Entry<int>? _keyDash;
    private static MelonPreferences_Entry<int>? _protMaxParticles;
    private static MelonPreferences_Entry<int>? _protMaxAudio;
    private static MelonPreferences_Entry<bool>? _protLimitParticles;
    private static MelonPreferences_Entry<bool>? _protLimitAudio;
    private static MelonPreferences_Entry<int>? _protMaxConstraints;
    private static MelonPreferences_Entry<int>? _protMaxColliders;
    private static MelonPreferences_Entry<int>? _protMaxRigidbodies;
    private static MelonPreferences_Entry<int>? _protMaxJoints;
    private static MelonPreferences_Entry<int>? _protMaxTrails;
    private static MelonPreferences_Entry<bool>? _protLimitConstraints;
    private static MelonPreferences_Entry<bool>? _protLimitColliders;
    private static MelonPreferences_Entry<bool>? _protLimitRigidbodies;
    private static MelonPreferences_Entry<bool>? _protLimitJoints;
    private static MelonPreferences_Entry<bool>? _protLimitTrails;
    private static MelonPreferences_Entry<bool>? _protStripCameras;
    private static MelonPreferences_Entry<int>? _protMaxCloth;
    private static MelonPreferences_Entry<bool>? _protLimitCloth;
    private static MelonPreferences_Entry<int>? _protMaxVideos;
    private static MelonPreferences_Entry<int>? _protMaxBlendshapes;
    private static MelonPreferences_Entry<int>? _protMaxContacts;
    private static MelonPreferences_Entry<bool>? _protLimitVideos;
    private static MelonPreferences_Entry<bool>? _protLimitBlendshapes;
    private static MelonPreferences_Entry<bool>? _protLimitContacts;
    private static MelonPreferences_Entry<string>? _blockedAvatarsJson;
    private static MelonPreferences_Entry<bool>? _protMuteAvatarAudio;
    private static MelonPreferences_Entry<bool>? _protAutoRescan;

    // Camera (-1 = untouched)
    private static MelonPreferences_Entry<float>? _fov;

    // Bookmarks (JSON blob)
    private static MelonPreferences_Entry<string>? _bookmarkJson;

    // Flashlight
    private static MelonPreferences_Entry<float>? _flashAngle;
    private static MelonPreferences_Entry<float>? _flashRange;
    private static MelonPreferences_Entry<float>? _flashIntensity;

    // Protection caps
    private static MelonPreferences_Entry<int>? _protMaxMaterials;
    private static MelonPreferences_Entry<int>? _protMaxVerts;
    private static MelonPreferences_Entry<bool>? _protLimitMaterials;
    private static MelonPreferences_Entry<bool>? _protLimitPoly;
    private static MelonPreferences_Entry<bool>? _protAutoHide;
    private static MelonPreferences_Entry<int>? _keyMenus;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            _cat = MelonPreferences.CreateCategory("HeavenlyCVR", "HeavenlyCVR");

            _espPlayerRefresh = E("EspPlayerRefresh", 3f, "ESP player refresh (s)");
            _espPropRefresh = E("EspPropRefresh", 4f, "ESP prop refresh (s)");
            _espNameplateRefresh = E("EspNameplateRefresh", 5f, "ESP nameplate refresh (s)");
            _espPropMax = E("EspPropMax", 50, "ESP max prop markers");
            _espOutlineMax = E("EspOutlineMax", 8, "ESP max outlined players");
            _espUseCustom = E("EspUseCustom", false, "ESP custom color override");
            _espCustomR = E("EspCustomR", 255, "ESP custom color R");
            _espCustomG = E("EspCustomG", 8, "ESP custom color G");
            _espCustomB = E("EspCustomB", 90, "ESP custom color B");
            _espPlayerScale = E("EspPlayerScale", 1f, "ESP outline width multiplier");
            _espPropSize = E("EspPropSize", 0.25f, "ESP prop marker size");

            _espMaster = E("EspMaster", false, "ESP master default");
            _espPlayers = E("EspPlayers", true, "Player ESP default");
            _espPickups = E("EspPickups", false, "Pickup ESP default");
            _espSpawnables = E("EspSpawnables", false, "Spawnable ESP default");
            _espNameplates = E("EspNameplates", false, "Nameplate ESP default");
            _espHideNameplates = E("EspHideNameplates", false, "Hide nameplates default");

            _colDefaultR = E("EspColDefaultR", 255, "ESP default color R");
            _colDefaultG = E("EspColDefaultG", 8, "ESP default color G");
            _colDefaultB = E("EspColDefaultB", 90, "ESP default color B");
            _colFriendR = E("EspColFriendR", 255, "ESP friend color R");
            _colFriendG = E("EspColFriendG", 251, "ESP friend color G");
            _colFriendB = E("EspColFriendB", 0, "ESP friend color B");
            _colLegendR = E("EspColLegendR", 227, "ESP legend color R");
            _colLegendG = E("EspColLegendG", 129, "ESP legend color G");
            _colLegendB = E("EspColLegendB", 0, "ESP legend color B");
            _colGuideR = E("EspColGuideR", 0, "ESP guide color R");
            _colGuideG = E("EspColGuideG", 199, "ESP guide color G");
            _colGuideB = E("EspColGuideB", 7, "ESP guide color B");
            _colModR = E("EspColModR", 158, "ESP moderator color R");
            _colModG = E("EspColModG", 0, "ESP moderator color G");
            _colModB = E("EspColModB", 29, "ESP moderator color B");
            _colDevR = E("EspColDevR", 77, "ESP developer color R");
            _colDevG = E("EspColDevG", 0, "ESP developer color G");
            _colDevB = E("EspColDevB", 14, "ESP developer color B");

            _flySpeed = E("FlySpeed", 3f, "Fly speed multiplier");
            _sitBone = E("SitBone", (int)HumanBodyBones.Head, "Sit bone");
            _sitTimeoutSec = E("SitTimeoutSec", 5f, "Sit target timeout (s)");
            _sitOffset = E("SitOffset", 0f, "Extra sit height (m)");

            _protHideAvatars = E("ProtHideAvatars", false, "Hide avatars default");
            _protBlockPortals = E("ProtBlockPortals", false, "Block portals+props default");
            _protSafeShaders = E("ProtSafeShaders", false, "Safe shaders default");
            _protLimitLights = E("ProtLimitLights", false, "Limit lights default");
            _protSkipFriends = E("ProtSkipFriends", true, "Skip friends in scans");
            _protMaxLights = E("ProtMaxLights", 6, "Max lights per avatar");
            _protScanDelay = E("ProtScanDelay", 12f, "Auto-scan delay (s)");

            _teleportRight = E("TeleportRight", 1.2f, "Teleport sideways offset");
            _teleportUp = E("TeleportUp", 0.3f, "Teleport up offset");

            _volMaster = E("VolMaster", 1f, "Master volume");
            _volAvatar = E("VolAvatar", 1f, "Avatar volume");
            _volWorld = E("VolWorld", 1f, "World volume");

            _feedLines = E("FeedLines", 30, "Debug feed lines kept");

            _fpsCap = E("FpsCap", 0, "FPS cap (0 = off)");

            _tagsJson = E("TagsJson", "[]", "Custom nameplate tags (JSON)");
            _tagBaseY = E("TagBaseY", -0.12f, "Tag vertical base offset");
            _tagGap = E("TagGap", 0.07f, "Tag vertical spacing");

            _playerNotesJson = E("PlayerNotesJson", "{}", "Player notes (JSON)");

            _worldHistoryJson = E("WorldHistoryJson", "[]", "Instance history (JSON)");
            _avatarHistoryJson = E("AvatarHistoryJson", "[]", "Avatar history (JSON)");
            _avatarFavoritesJson = E("AvatarFavoritesJson", "[]", "Avatar favorites (JSON)");

            _keyFly = E("KeyFly", 102, "Key: toggle fly (Ctrl+)");
            _keyRejoin = E("KeyRejoin", 114, "Key: rejoin instance (Ctrl+)");
            _keyNoClip = E("KeyNoClip", 110, "Key: toggle noclip (Ctrl+)");
            _keyStand = E("KeyStand", 116, "Key: stand up (Ctrl+)");
            _keyPanic = E("KeyPanic", 112, "Key: panic button (Ctrl+)");
            _keyRespawn = E("KeyRespawn", 0, "Key: respawn (Ctrl+, 0 = none)");
            _keyReloadAvatar = E("KeyReloadAvatar", 0, "Key: reload avatar (Ctrl+, 0 = none)");
            _keyEsp = E("KeyEsp", 0, "Key: toggle ESP master (Ctrl+, 0 = none)");
            _keyFlashlight = E("KeyFlashlight", 0, "Key: toggle flashlight (Ctrl+, 0 = none)");
            _protSafetyNet = E("ProtSafetyNet", false, "Safety net default");
            _dbgJoinToasts = E("DbgJoinToasts", false, "Join/leave toasts default");

            _walkSpeed = E("WalkSpeed", 1f, "Walk speed multiplier");
            _sprintMult = E("SprintMult", 1f, "Sprint multiplier");
            _jumpMult = E("JumpMult", 1f, "Jump impulse multiplier");
            _jumpCount = E("JumpCount", 1, "Air jumps allowed");
            _keyDash = E("KeyDash", 0, "Key: air dash (Ctrl+, 0 = none)");
            _protMaxParticles = E("ProtMaxParticles", 100, "Max particle systems per avatar");
            _protMaxAudio = E("ProtMaxAudio", 16, "Max audio sources per avatar");
            _protLimitParticles = E("ProtLimitParticles", false, "Limit particles default");
            _protLimitAudio = E("ProtLimitAudio", false, "Limit audio default");
            _protMaxConstraints = E("ProtMaxConstraints", 100, "Max constraints per avatar");
            _protMaxColliders = E("ProtMaxColliders", 200, "Max colliders per avatar");
            _protMaxRigidbodies = E("ProtMaxRigidbodies", 30, "Max rigidbodies per avatar");
            _protMaxJoints = E("ProtMaxJoints", 30, "Max joints per avatar");
            _protMaxTrails = E("ProtMaxTrails", 60, "Max trails/lines per avatar");
            _protLimitConstraints = E("ProtLimitConstraints", false, "Limit constraints default");
            _protLimitColliders = E("ProtLimitColliders", false, "Limit colliders default");
            _protLimitRigidbodies = E("ProtLimitRigidbodies", false, "Limit rigidbodies default");
            _protLimitJoints = E("ProtLimitJoints", false, "Limit joints default");
            _protLimitTrails = E("ProtLimitTrails", false, "Limit trails default");
            _protStripCameras = E("ProtStripCameras", false, "Strip avatar cameras default");
            _protMaxCloth = E("ProtMaxCloth", 12, "Max cloth sims per avatar");
            _protLimitCloth = E("ProtLimitCloth", false, "Limit cloth default");
            _protMaxVideos = E("ProtMaxVideos", 2, "Max video players per avatar");
            _protMaxBlendshapes = E("ProtMaxBlendshapes", 2000, "Max blendshapes per avatar");
            _protMaxContacts = E("ProtMaxContacts", 64, "Max contacts per avatar");
            _protLimitVideos = E("ProtLimitVideos", false, "Limit video players default");
            _protLimitBlendshapes = E("ProtLimitBlendshapes", false, "Limit blendshapes default");
            _protLimitContacts = E("ProtLimitContacts", false, "Limit contacts default");
            _blockedAvatarsJson = E("BlockedAvatarsJson", "[]", "Blocked avatar IDs (JSON)");
            _protMuteAvatarAudio = E("ProtMuteAvatarAudio", false, "Mute avatar audio default");
            _protAutoRescan = E("ProtAutoRescan", false, "Periodic rescan default");

            _fov = E("Fov", -1f, "Camera FOV (-1 = untouched)");

            _bookmarkJson = E("BookmarkJson", "[]", "Teleport bookmarks (JSON)");

            _flashAngle = E("FlashAngle", 60f, "Flashlight spot angle");
            _flashRange = E("FlashRange", 20f, "Flashlight range");
            _flashIntensity = E("FlashIntensity", 1.5f, "Flashlight intensity");

            _protMaxMaterials = E("ProtMaxMaterials", 100, "Max renderers per avatar");
            _protMaxVerts = E("ProtMaxVerts", 1000000, "Max verts per mesh");
            _protLimitMaterials = E("ProtLimitMaterials", false, "Limit renderers default");
            _protLimitPoly = E("ProtLimitPoly", false, "Limit poly default");
            _protAutoHide = E("ProtAutoHide", false, "Auto-hide laggy avatars");
            _keyMenus = E("KeyMenus", 0, "Key: reload menus (Ctrl+, 0 = none)");

            API.HeavenlyAPI.Log("HeavenlyConfig loaded.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"HeavenlyConfig init failed: {ex.Message}");
        }
    }

    private static MelonPreferences_Entry<T> E<T>(string id, T def, string display)
    {
        MelonPreferences_Entry<T> entry = _cat!.CreateEntry(id, def, display, display);
        T captured = def;
        _resetters.Add(() =>
        {
            try { entry.Value = captured; } catch { }
        });
        return entry;
    }

    private static float GetF(MelonPreferences_Entry<float>? e, float fb) => e != null ? e.Value : fb;
    private static int GetI(MelonPreferences_Entry<int>? e, int fb) => e != null ? e.Value : fb;
    private static bool GetB(MelonPreferences_Entry<bool>? e, bool fb) => e != null ? e.Value : fb;

    private static void SetF(MelonPreferences_Entry<float>? e, float v)
    {
        if (e == null) return;
        e.Value = v;
        MarkDirty();
    }

    private static void SetI(MelonPreferences_Entry<int>? e, int v)
    {
        if (e == null) return;
        e.Value = v;
        MarkDirty();
    }

    private static void SetB(MelonPreferences_Entry<bool>? e, bool v)
    {
        if (e == null) return;
        e.Value = v;
        MarkDirty();
    }

    public static float EspPlayerRefresh { get => GetF(_espPlayerRefresh, 3f); set => SetF(_espPlayerRefresh, value); }
    public static float EspPropRefresh { get => GetF(_espPropRefresh, 4f); set => SetF(_espPropRefresh, value); }
    public static float EspNameplateRefresh { get => GetF(_espNameplateRefresh, 5f); set => SetF(_espNameplateRefresh, value); }
    public static int EspPropMax { get => GetI(_espPropMax, 50); set => SetI(_espPropMax, value); }
    public static int EspOutlineMax { get => GetI(_espOutlineMax, 8); set => SetI(_espOutlineMax, value); }
    public static bool EspUseCustom { get => GetB(_espUseCustom, false); set => SetB(_espUseCustom, value); }
    public static int EspCustomR { get => GetI(_espCustomR, 255); set => SetI(_espCustomR, value); }
    public static int EspCustomG { get => GetI(_espCustomG, 8); set => SetI(_espCustomG, value); }
    public static int EspCustomB { get => GetI(_espCustomB, 90); set => SetI(_espCustomB, value); }

    public static Color32 EspColorCustom => new Color32(B(_espCustomR), B(_espCustomG), B(_espCustomB), 255);

    public static void SetEspColorCustom(int r, int g, int b) { EspCustomR = r; EspCustomG = g; EspCustomB = b; }
    public static float EspPlayerScale { get => GetF(_espPlayerScale, 1f); set => SetF(_espPlayerScale, value); }
    public static float EspPropSize { get => GetF(_espPropSize, 0.25f); set => SetF(_espPropSize, value); }

    public static bool EspMaster { get => GetB(_espMaster, false); set => SetB(_espMaster, value); }
    public static bool EspPlayers { get => GetB(_espPlayers, true); set => SetB(_espPlayers, value); }
    public static bool EspPickups { get => GetB(_espPickups, false); set => SetB(_espPickups, value); }
    public static bool EspSpawnables { get => GetB(_espSpawnables, false); set => SetB(_espSpawnables, value); }
    public static bool EspNameplates { get => GetB(_espNameplates, false); set => SetB(_espNameplates, value); }
    public static bool EspHideNameplates { get => GetB(_espHideNameplates, false); set => SetB(_espHideNameplates, value); }

    public static int EspColDefaultR { get => GetI(_colDefaultR, 255); set => SetI(_colDefaultR, value); }
    public static int EspColDefaultG { get => GetI(_colDefaultG, 8); set => SetI(_colDefaultG, value); }
    public static int EspColDefaultB { get => GetI(_colDefaultB, 90); set => SetI(_colDefaultB, value); }
    public static int EspColFriendR { get => GetI(_colFriendR, 255); set => SetI(_colFriendR, value); }
    public static int EspColFriendG { get => GetI(_colFriendG, 251); set => SetI(_colFriendG, value); }
    public static int EspColFriendB { get => GetI(_colFriendB, 0); set => SetI(_colFriendB, value); }
    public static int EspColLegendR { get => GetI(_colLegendR, 227); set => SetI(_colLegendR, value); }
    public static int EspColLegendG { get => GetI(_colLegendG, 129); set => SetI(_colLegendG, value); }
    public static int EspColLegendB { get => GetI(_colLegendB, 0); set => SetI(_colLegendB, value); }
    public static int EspColGuideR { get => GetI(_colGuideR, 0); set => SetI(_colGuideR, value); }
    public static int EspColGuideG { get => GetI(_colGuideG, 199); set => SetI(_colGuideG, value); }
    public static int EspColGuideB { get => GetI(_colGuideB, 7); set => SetI(_colGuideB, value); }
    public static int EspColModR { get => GetI(_colModR, 158); set => SetI(_colModR, value); }
    public static int EspColModG { get => GetI(_colModG, 0); set => SetI(_colModG, value); }
    public static int EspColModB { get => GetI(_colModB, 29); set => SetI(_colModB, value); }
    public static int EspColDevR { get => GetI(_colDevR, 77); set => SetI(_colDevR, value); }
    public static int EspColDevG { get => GetI(_colDevG, 0); set => SetI(_colDevG, value); }
    public static int EspColDevB { get => GetI(_colDevB, 14); set => SetI(_colDevB, value); }

    public static Color32 EspColorDefault => new Color32(B(_colDefaultR), B(_colDefaultG), B(_colDefaultB), 255);
    public static Color32 EspColorFriend => new Color32(B(_colFriendR), B(_colFriendG), B(_colFriendB), 255);
    public static Color32 EspColorLegend => new Color32(B(_colLegendR), B(_colLegendG), B(_colLegendB), 255);
    public static Color32 EspColorGuide => new Color32(B(_colGuideR), B(_colGuideG), B(_colGuideB), 255);
    public static Color32 EspColorMod => new Color32(B(_colModR), B(_colModG), B(_colModB), 255);
    public static Color32 EspColorDev => new Color32(B(_colDevR), B(_colDevG), B(_colDevB), 255);

    private static byte B(MelonPreferences_Entry<int>? e)
    {
        int v = e != null ? e.Value : 0;
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }

    public static void SetEspColorDefault(int r, int g, int b) { EspColDefaultR = r; EspColDefaultG = g; EspColDefaultB = b; }
    public static void SetEspColorFriend(int r, int g, int b) { EspColFriendR = r; EspColFriendG = g; EspColFriendB = b; }
    public static void SetEspColorLegend(int r, int g, int b) { EspColLegendR = r; EspColLegendG = g; EspColLegendB = b; }
    public static void SetEspColorGuide(int r, int g, int b) { EspColGuideR = r; EspColGuideG = g; EspColGuideB = b; }
    public static void SetEspColorMod(int r, int g, int b) { EspColModR = r; EspColModG = g; EspColModB = b; }
    public static void SetEspColorDev(int r, int g, int b) { EspColDevR = r; EspColDevG = g; EspColDevB = b; }

    public static float FlySpeed { get => GetF(_flySpeed, 3f); set => SetF(_flySpeed, value); }
    public static int SitBone { get => GetI(_sitBone, (int)HumanBodyBones.Head); set => SetI(_sitBone, value); }
    public static float SitTimeoutSec { get => GetF(_sitTimeoutSec, 5f); set => SetF(_sitTimeoutSec, value); }
    public static float SitOffset { get => GetF(_sitOffset, 0f); set => SetF(_sitOffset, value); }

    public static bool ProtHideAvatars { get => GetB(_protHideAvatars, false); set => SetB(_protHideAvatars, value); }
    public static bool ProtBlockPortals { get => GetB(_protBlockPortals, false); set => SetB(_protBlockPortals, value); }
    public static bool ProtSafeShaders { get => GetB(_protSafeShaders, false); set => SetB(_protSafeShaders, value); }
    public static bool ProtLimitLights { get => GetB(_protLimitLights, false); set => SetB(_protLimitLights, value); }
    public static bool ProtSkipFriends { get => GetB(_protSkipFriends, true); set => SetB(_protSkipFriends, value); }
    public static int ProtMaxLights { get => GetI(_protMaxLights, 6); set => SetI(_protMaxLights, value); }
    public static float ProtScanDelay { get => GetF(_protScanDelay, 12f); set => SetF(_protScanDelay, value); }

    public static float TeleportRight { get => GetF(_teleportRight, 1.2f); set => SetF(_teleportRight, value); }
    public static float TeleportUp { get => GetF(_teleportUp, 0.3f); set => SetF(_teleportUp, value); }

    public static float VolMaster { get => GetF(_volMaster, 1f); set => SetF(_volMaster, value); }
    public static float VolAvatar { get => GetF(_volAvatar, 1f); set => SetF(_volAvatar, value); }
    public static float VolWorld { get => GetF(_volWorld, 1f); set => SetF(_volWorld, value); }

    public static int FeedLines { get => GetI(_feedLines, 30); set => SetI(_feedLines, value); }
    public static int FpsCap { get => GetI(_fpsCap, 0); set => SetI(_fpsCap, value); }

    public static int KeyFly { get => GetI(_keyFly, 102); set => SetI(_keyFly, value); }
    public static int KeyRejoin { get => GetI(_keyRejoin, 114); set => SetI(_keyRejoin, value); }
    public static int KeyNoClip { get => GetI(_keyNoClip, 110); set => SetI(_keyNoClip, value); }
    public static int KeyStand { get => GetI(_keyStand, 116); set => SetI(_keyStand, value); }
    public static int KeyPanic { get => GetI(_keyPanic, 112); set => SetI(_keyPanic, value); }
    public static int KeyRespawn { get => GetI(_keyRespawn, 0); set => SetI(_keyRespawn, value); }
    public static int KeyReloadAvatar { get => GetI(_keyReloadAvatar, 0); set => SetI(_keyReloadAvatar, value); }
    public static int KeyEsp { get => GetI(_keyEsp, 0); set => SetI(_keyEsp, value); }
    public static int KeyFlashlight { get => GetI(_keyFlashlight, 0); set => SetI(_keyFlashlight, value); }
    public static bool ProtSafetyNet { get => GetB(_protSafetyNet, false); set => SetB(_protSafetyNet, value); }
    public static bool DbgJoinToasts { get => GetB(_dbgJoinToasts, false); set => SetB(_dbgJoinToasts, value); }

    public static float WalkSpeed { get => GetF(_walkSpeed, 1f); set => SetF(_walkSpeed, value); }
    public static float SprintMult { get => GetF(_sprintMult, 1f); set => SetF(_sprintMult, value); }
    public static float JumpMult { get => GetF(_jumpMult, 1f); set => SetF(_jumpMult, value); }
    public static int JumpCount { get => GetI(_jumpCount, 1); set => SetI(_jumpCount, value); }
    public static int KeyDash { get => GetI(_keyDash, 0); set => SetI(_keyDash, value); }
    public static int ProtMaxParticles { get => GetI(_protMaxParticles, 100); set => SetI(_protMaxParticles, value); }
    public static int ProtMaxAudio { get => GetI(_protMaxAudio, 16); set => SetI(_protMaxAudio, value); }
    public static bool ProtLimitParticles { get => GetB(_protLimitParticles, false); set => SetB(_protLimitParticles, value); }
    public static bool ProtLimitAudio { get => GetB(_protLimitAudio, false); set => SetB(_protLimitAudio, value); }
    public static int ProtMaxConstraints { get => GetI(_protMaxConstraints, 100); set => SetI(_protMaxConstraints, value); }
    public static int ProtMaxColliders { get => GetI(_protMaxColliders, 200); set => SetI(_protMaxColliders, value); }
    public static int ProtMaxRigidbodies { get => GetI(_protMaxRigidbodies, 30); set => SetI(_protMaxRigidbodies, value); }
    public static int ProtMaxJoints { get => GetI(_protMaxJoints, 30); set => SetI(_protMaxJoints, value); }
    public static int ProtMaxTrails { get => GetI(_protMaxTrails, 60); set => SetI(_protMaxTrails, value); }
    public static bool ProtLimitConstraints { get => GetB(_protLimitConstraints, false); set => SetB(_protLimitConstraints, value); }
    public static bool ProtLimitColliders { get => GetB(_protLimitColliders, false); set => SetB(_protLimitColliders, value); }
    public static bool ProtLimitRigidbodies { get => GetB(_protLimitRigidbodies, false); set => SetB(_protLimitRigidbodies, value); }
    public static bool ProtLimitJoints { get => GetB(_protLimitJoints, false); set => SetB(_protLimitJoints, value); }
    public static bool ProtLimitTrails { get => GetB(_protLimitTrails, false); set => SetB(_protLimitTrails, value); }
    public static bool ProtStripCameras { get => GetB(_protStripCameras, false); set => SetB(_protStripCameras, value); }
    public static int ProtMaxCloth { get => GetI(_protMaxCloth, 12); set => SetI(_protMaxCloth, value); }
    public static bool ProtLimitCloth { get => GetB(_protLimitCloth, false); set => SetB(_protLimitCloth, value); }
    public static int ProtMaxVideos { get => GetI(_protMaxVideos, 2); set => SetI(_protMaxVideos, value); }
    public static int ProtMaxBlendshapes { get => GetI(_protMaxBlendshapes, 2000); set => SetI(_protMaxBlendshapes, value); }
    public static int ProtMaxContacts { get => GetI(_protMaxContacts, 64); set => SetI(_protMaxContacts, value); }
    public static bool ProtLimitVideos { get => GetB(_protLimitVideos, false); set => SetB(_protLimitVideos, value); }
    public static bool ProtLimitBlendshapes { get => GetB(_protLimitBlendshapes, false); set => SetB(_protLimitBlendshapes, value); }
    public static bool ProtLimitContacts { get => GetB(_protLimitContacts, false); set => SetB(_protLimitContacts, value); }

    public static string BlockedAvatarsJson
    {
        get
        {
            try { return _blockedAvatarsJson != null ? _blockedAvatarsJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_blockedAvatarsJson == null) return;
            try { _blockedAvatarsJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }
    public static bool ProtMuteAvatarAudio { get => GetB(_protMuteAvatarAudio, false); set => SetB(_protMuteAvatarAudio, value); }
    public static bool ProtAutoRescan { get => GetB(_protAutoRescan, false); set => SetB(_protAutoRescan, value); }

    public static float Fov { get => GetF(_fov, -1f); set => SetF(_fov, value); }

    public static string BookmarkJson
    {
        get
        {
            try { return _bookmarkJson != null ? _bookmarkJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_bookmarkJson == null) return;
            try { _bookmarkJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }

    public static float FlashAngle { get => GetF(_flashAngle, 60f); set => SetF(_flashAngle, value); }
    public static float FlashRange { get => GetF(_flashRange, 20f); set => SetF(_flashRange, value); }
    public static float FlashIntensity { get => GetF(_flashIntensity, 1.5f); set => SetF(_flashIntensity, value); }

    public static int ProtMaxMaterials { get => GetI(_protMaxMaterials, 100); set => SetI(_protMaxMaterials, value); }
    public static int ProtMaxVerts { get => GetI(_protMaxVerts, 1000000); set => SetI(_protMaxVerts, value); }
    public static bool ProtLimitMaterials { get => GetB(_protLimitMaterials, false); set => SetB(_protLimitMaterials, value); }
    public static bool ProtLimitPoly { get => GetB(_protLimitPoly, false); set => SetB(_protLimitPoly, value); }
    public static bool ProtAutoHide { get => GetB(_protAutoHide, false); set => SetB(_protAutoHide, value); }
    public static int KeyMenus { get => GetI(_keyMenus, 0); set => SetI(_keyMenus, value); }

    public static string WorldHistoryJson
    {
        get
        {
            try { return _worldHistoryJson != null ? _worldHistoryJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_worldHistoryJson == null) return;
            try { _worldHistoryJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }

    public static string AvatarHistoryJson
    {
        get
        {
            try { return _avatarHistoryJson != null ? _avatarHistoryJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_avatarHistoryJson == null) return;
            try { _avatarHistoryJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }

    public static string AvatarFavoritesJson
    {
        get
        {
            try { return _avatarFavoritesJson != null ? _avatarFavoritesJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_avatarFavoritesJson == null) return;
            try { _avatarFavoritesJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }

    public static string PlayerNotesJson
    {
        get
        {
            try { return _playerNotesJson != null ? _playerNotesJson.Value ?? "{}" : "{}"; }
            catch { return "{}"; }
        }
        set
        {
            if (_playerNotesJson == null) return;
            try { _playerNotesJson.Value = value ?? "{}"; } catch { return; }
            MarkDirty();
        }
    }

    public static float TagBaseY { get => GetF(_tagBaseY, -0.12f); set => SetF(_tagBaseY, value); }
    public static float TagGap { get => GetF(_tagGap, 0.07f); set => SetF(_tagGap, value); }

    public static string TagsJson
    {
        get
        {
            try { return _tagsJson != null ? _tagsJson.Value ?? "[]" : "[]"; }
            catch { return "[]"; }
        }
        set
        {
            if (_tagsJson == null) return;
            try { _tagsJson.Value = value ?? "[]"; } catch { return; }
            MarkDirty();
        }
    }

    public static void MarkDirty()
    {
        _dirty = true;
    }

    /// <summary>Called every frame; saves at most every few seconds when dirty.</summary>
    public static void Flush()
    {
        if (!_dirty)
            return;

        try
        {
            int now = Environment.TickCount;
            if (now - _lastSaveTick < SaveThrottleMs)
                return;

            _lastSaveTick = now;
            _dirty = false;
            MelonPreferences.Save();
        }
        catch { }
    }

    public static void ResetDefaults()
    {
        try
        {
            foreach (Action reset in _resetters)
            {
                try { reset(); } catch { }
            }

            SaveNow();
            API.HeavenlyAPI.Toast("[Heavenly] Config reset to defaults.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Reset defaults failed: {ex.Message}");
        }
    }

    public static void SaveNow()
    {
        try
        {
            _dirty = false;
            _lastSaveTick = Environment.TickCount;
            MelonPreferences.Save();
        }
        catch { }
    }
}
