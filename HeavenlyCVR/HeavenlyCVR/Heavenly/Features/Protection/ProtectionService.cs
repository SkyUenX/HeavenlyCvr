using System;
using System.Collections.Generic;
using ABI_RC.API;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Protection;

public static class ProtectionService
{
    public static bool HideAvatarsEnabled { get; private set; }
    public static bool BlockPortalsPropsEnabled { get; private set; }

    /// <summary>Replace remote avatar shaders with Standard (local-only).</summary>
    public static bool SafeShadersEnabled { get; private set; }

    /// <summary>Destroy excess Light components on remote avatars.</summary>
    public static bool LimitLightsEnabled { get; private set; }

    /// <summary>Destroy renderers beyond a per-avatar count.</summary>
    public static bool LimitMaterialsEnabled { get; private set; }

    /// <summary>Destroy renderers whose mesh exceeds a vertex cap.</summary>
    public static bool LimitPolyEnabled { get; private set; }

    /// <summary>Auto-respawn when fallen below the world (unless flying).</summary>
    public static bool SafetyNetEnabled { get; private set; }

    /// <summary>Auto-hide avatars far beyond the scan caps (2x). Reversible on reload.</summary>
    public static bool AutoHideLaggyEnabled { get; private set; }

    /// <summary>Disable all pickup interactions (local-only).</summary>
    public static bool BlockPickupsEnabled { get; private set; }

    /// <summary>Destroy particle systems beyond a per-avatar count.</summary>
    public static bool LimitParticlesEnabled { get; private set; }

    /// <summary>Destroy audio sources beyond a per-avatar count.</summary>
    public static bool LimitAudioEnabled { get; private set; }

    public static bool LimitConstraintsEnabled { get; private set; }
    public static bool LimitCollidersEnabled { get; private set; }
    public static bool LimitRigidbodiesEnabled { get; private set; }
    public static bool LimitJointsEnabled { get; private set; }
    public static bool LimitTrailsEnabled { get; private set; }
    public static bool StripCamerasEnabled { get; private set; }

    public static bool LimitClothEnabled { get; private set; }

    public static bool LimitVideosEnabled { get; private set; }
    public static bool LimitBlendshapesEnabled { get; private set; }
    public static bool LimitContactsEnabled { get; private set; }

    /// <summary>Destroy ALL audio sources on remote avatars (voice unaffected).</summary>
    public static bool MuteAvatarAudioEnabled { get; private set; }

    /// <summary>Re-run the safety scan every 60s while any limiter is on.</summary>
    public static bool AutoRescanEnabled { get; private set; }

    private static float _nextRescan;

    public static bool DistanceCullingEnabled => ReadCulling().enabled;
    public static int CullingDistance => ReadCulling().distance;
    public static bool CullingFilterFriends => ReadCulling().friendsOnly;

    private static float _nextSafetyCheck;

    public static bool SkipFriends { get; private set; } = true;

    private static bool _origAllowPortals = true;
    private static bool _origAllowSpawnables = true;
    private static bool _origCaptured;
    private static bool _initialized;

    private static readonly List<PendingScan> PendingScans = new();

    private sealed class PendingScan
    {
        public string UserId = "";
        public float DueTime;
    }

    public static void SetHideAvatars(bool enabled)
    {
        HideAvatarsEnabled = enabled;
        try { Config.HeavenlyConfig.ProtHideAvatars = enabled; } catch { }
        try
        {
            int count = 0;
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity?.PuppetMaster == null) continue;
                    try
                    {
                        entity.PuppetMaster.SetAvatarVisibility(!enabled);
                        count++;
                    }
                    catch { }
                }
            }

            API.HeavenlyAPI.Toast(enabled
                ? $"[Heavenly] Hid {count} avatar(s)."
                : "[Heavenly] Avatars restored (re-hidden avatars show on rejoin).");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Hide avatars failed: {ex.Message}");
        }
    }

    public static void SetBlockPortalsProps(bool enabled)
    {
        BlockPortalsPropsEnabled = enabled;
        try { Config.HeavenlyConfig.ProtBlockPortals = enabled; } catch { }
        try
        {
            var world = ABI.CCK.Components.CVRWorld.Instance;
            if (world == null)
            {
                // World loads later: the world-load hook applies the flag then.
                API.HeavenlyAPI.Toast("[Heavenly] No world loaded yet (applies on load).");
                return;
            }

            ApplyBlockToWorld(world, enabled);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Block portals/props failed: {ex.Message}");
        }
    }

    private static void ApplyBlockToWorld(ABI.CCK.Components.CVRWorld world, bool enabled)
    {
        if (!_origCaptured)
        {
            try
            {
                _origAllowPortals = world.allowPortals;
                _origAllowSpawnables = world.allowSpawnables;
                _origCaptured = true;
            }
            catch { }
        }

        try { world.allowPortals = !enabled; } catch { }
        try { world.allowSpawnables = !enabled; } catch { }

        if (!enabled && _origCaptured)
        {
            try { world.allowPortals = _origAllowPortals; } catch { }
            try { world.allowSpawnables = _origAllowSpawnables; } catch { }
        }

        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Portals + spawnables blocked for this world."
            : "[Heavenly] Portals + spawnables restored.");
    }

    public static void Panic()
    {
        try
        {
            // Safe-mode: stop movement tricks, restore controller, respawn cleanly.
            try { Movement.MovementService.StandUp(); } catch { }
            try { Movement.MovementService.SetFlying(false); } catch { }

            try
            {
                string? def = ABI_RC.Core.Savior.MetaPort.Instance?.defaultAvatarGuid;
                if (def != null && def.Length > 0)
                {
                    try { Avatar.AvatarService.SwitchTo(def); } catch { }
                }
            }
            catch { }

            try { Movement.MovementService.Respawn(); } catch { }

            API.HeavenlyAPI.Toast("[Heavenly] Panic: stopped tricks, restoring safe avatar.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Panic failed: {ex.Message}");
        }
    }

    public static void SetSafeShaders(bool enabled)
    {
        SafeShadersEnabled = enabled;
        try { Config.HeavenlyConfig.ProtSafeShaders = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Safe shaders ON (applies to loaded + joining avatars)."
            : "[Heavenly] Safe shaders OFF (already-downgraded avatars restore on reload).");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitLights(bool enabled)
    {
        LimitLightsEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitLights = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Light limiter ON (max {MaxLights()}/avatar)."
            : "[Heavenly] Light limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitMaterials(bool enabled)
    {
        LimitMaterialsEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitMaterials = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Material limiter ON (max {MaxMaterials()}/avatar)."
            : "[Heavenly] Material limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitPoly(bool enabled)
    {
        LimitPolyEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitPoly = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Poly limiter ON (see Config for cap)."
            : "[Heavenly] Poly limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetSafetyNet(bool enabled)
    {
        SafetyNetEnabled = enabled;
        try { Config.HeavenlyConfig.ProtSafetyNet = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Safety net ON (auto-respawn on void fall)."
            : "[Heavenly] Safety net OFF.");
    }

    public static void SetDistanceCulling(bool enabled)
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Player manager not ready.");
                return;
            }

            manager.disablePlayerAtDistance = enabled;
            API.HeavenlyAPI.Toast(enabled
                ? "[Heavenly] Distance culling ON."
                : "[Heavenly] Distance culling OFF.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Distance culling failed: {ex.Message}");
        }
    }

    public static void SetCullingDistance(float meters)
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager == null)
                return;

            int m = (int)meters;
            if (m < 5) m = 5;
            if (m > 200) m = 200;
            manager.disablePlayerAtDistanceDistance = m;
            API.HeavenlyAPI.Log($"Culling distance -> {m}m");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Culling distance failed: {ex.Message}");
        }
    }

    public static void SetCullingFriendsOnly(bool friendsOnly)
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager == null)
                return;

            manager.disablePlayerAtDistanceFilterFriends = friendsOnly;
            API.HeavenlyAPI.Log($"Culling friends-only filter -> {(friendsOnly ? "ON" : "OFF")}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Culling filter failed: {ex.Message}");
        }
    }

    private static (bool enabled, int distance, bool friendsOnly) ReadCulling()
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager != null)
                return (manager.disablePlayerAtDistance, manager.disablePlayerAtDistanceDistance, manager.disablePlayerAtDistanceFilterFriends);
        }
        catch { }
        return (false, 25, false);
    }

    public static void SetAutoHideLaggy(bool enabled)
    {
        AutoHideLaggyEnabled = enabled;
        try { Config.HeavenlyConfig.ProtAutoHide = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Auto-hide laggy ON (2x scan caps, reversible)."
            : "[Heavenly] Auto-hide laggy OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitParticles(bool enabled)
    {
        LimitParticlesEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitParticles = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Particle limiter ON (max {MaxParticles()}/avatar)."
            : "[Heavenly] Particle limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitAudio(bool enabled)
    {
        LimitAudioEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitAudio = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Audio limiter ON (max {MaxAudio()}/avatar)."
            : "[Heavenly] Audio limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetMuteAvatarAudio(bool enabled)
    {
        MuteAvatarAudioEnabled = enabled;
        try { Config.HeavenlyConfig.ProtMuteAvatarAudio = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Avatar audio muted (voice chat unaffected)."
            : "[Heavenly] Avatar audio restored on reload.");
        if (enabled)
            ScanAll();
    }

    public static void SetAutoRescan(bool enabled)
    {
        AutoRescanEnabled = enabled;
        try { Config.HeavenlyConfig.ProtAutoRescan = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Auto rescan ON (every 60s)."
            : "[Heavenly] Auto rescan OFF.");
    }

    public static void SetBlockPickups(bool enabled)
    {
        BlockPickupsEnabled = enabled;
        try
        {
            int count = SetPickupsEnabled(enabled);
            API.HeavenlyAPI.Toast(enabled
                ? $"[Heavenly] Pickups disabled ({count})."
                : $"[Heavenly] Pickups restored ({count}).");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Pickup block failed: {ex.Message}");
        }
    }

    private static int SetPickupsEnabled(bool enabled)
    {
        int count = 0;
        try
        {
            ABI.CCK.Components.CVRPickupObject[]? pickups = null;
            try { pickups = UnityEngine.Object.FindObjectsOfType<ABI.CCK.Components.CVRPickupObject>(); } catch { }
            if (pickups == null)
                return 0;

            foreach (var pickup in pickups)
            {
                if (pickup == null)
                    continue;
                try
                {
                    pickup.enabled = enabled;
                    count++;
                }
                catch { }
            }
        }
        catch { }
        return count;
    }

    public static void RemovePortals()
    {
        try
        {
            ABI_RC.Systems.PortalsV2.AbstractPortalController[]? portals = null;
            try { portals = UnityEngine.Object.FindObjectsOfType<ABI_RC.Systems.PortalsV2.AbstractPortalController>(); } catch { }
            if (portals == null || portals.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No portals in this world.");
                return;
            }

            int count = 0;
            foreach (var portal in portals)
            {
                if (portal == null)
                    continue;
                try
                {
                    UnityEngine.Object.Destroy(portal.gameObject);
                    count++;
                }
                catch { }
            }

            API.HeavenlyAPI.Log($"Removed {count} portal(s) locally (back on reload).");
            API.HeavenlyAPI.Toast($"[Heavenly] Removed {count} portal(s) locally.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Remove portals failed: {ex.Message}");
        }
    }

    public static void AuditWorld()
    {
        try
        {
            int lights = 0, particles = 0, audios = 0, renderers = 0, pickups = 0;
            try { lights = UnityEngine.Object.FindObjectsOfType<Light>().Length; } catch { }
            try { particles = UnityEngine.Object.FindObjectsOfType<ParticleSystem>().Length; } catch { }
            try { audios = UnityEngine.Object.FindObjectsOfType<AudioSource>().Length; } catch { }
            try { renderers = UnityEngine.Object.FindObjectsOfType<Renderer>().Length; } catch { }
            try
            {
                var found = UnityEngine.Object.FindObjectsOfType<ABI.CCK.Components.CVRPickupObject>();
                if (found != null)
                    pickups = found.Length;
            }
            catch { }

            API.HeavenlyAPI.Log(
                "--- World audit (whole scene, incl. avatars) ---\n" +
                $"Lights: {lights} | Particles: {particles} | Audio: {audios} | " +
                $"Renderers: {renderers} | Pickups: {pickups}"
            );
            API.HeavenlyAPI.Toast("[Heavenly] World audit logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"World audit failed: {ex.Message}");
        }
    }

    public static void LogStatus()
    {
        try
        {
            API.HeavenlyAPI.Log(
                "--- Protection status ---\n" +
                $"HideAvatars={HideAvatarsEnabled} BlockPortals={BlockPortalsPropsEnabled} " +
                $"BlockPickups={BlockPickupsEnabled} Shaders={SafeShadersEnabled}\n" +
                $"Lights={LimitLightsEnabled} Materials={LimitMaterialsEnabled} Poly={LimitPolyEnabled} " +
                $"Particles={LimitParticlesEnabled} Audio={LimitAudioEnabled} MuteAudio={MuteAvatarAudioEnabled}\n" +
                $"AutoHide={AutoHideLaggyEnabled} Rescan={AutoRescanEnabled} SkipFriends={SkipFriends} " +
                $"SafetyNet={SafetyNetEnabled} Culling={DistanceCullingEnabled}"
            );
            API.HeavenlyAPI.Toast("[Heavenly] Protection status logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Protection status failed: {ex.Message}");
        }
    }

    private static void SetLimiterToggle(string which, bool enabled, string label, string onExtra = "")
    {
        try
        {
            switch (which)
            {
                case "constraints": LimitConstraintsEnabled = enabled; Config.HeavenlyConfig.ProtLimitConstraints = enabled; break;
                case "colliders": LimitCollidersEnabled = enabled; Config.HeavenlyConfig.ProtLimitColliders = enabled; break;
                case "rigidbodies": LimitRigidbodiesEnabled = enabled; Config.HeavenlyConfig.ProtLimitRigidbodies = enabled; break;
                case "joints": LimitJointsEnabled = enabled; Config.HeavenlyConfig.ProtLimitJoints = enabled; break;
                case "trails": LimitTrailsEnabled = enabled; Config.HeavenlyConfig.ProtLimitTrails = enabled; break;
                case "cameras": StripCamerasEnabled = enabled; Config.HeavenlyConfig.ProtStripCameras = enabled; break;
            }
        }
        catch { }

        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] {label} ON{onExtra}."
            : $"[Heavenly] {label} OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitConstraints(bool enabled) =>
        SetLimiterToggle("constraints", enabled, "Constraint limiter");

    public static void SetLimitColliders(bool enabled) =>
        SetLimiterToggle("colliders", enabled, "Collider limiter");

    public static void SetLimitRigidbodies(bool enabled) =>
        SetLimiterToggle("rigidbodies", enabled, "Rigidbody limiter");

    public static void SetLimitJoints(bool enabled) =>
        SetLimiterToggle("joints", enabled, "Joint limiter");

    public static void SetLimitTrails(bool enabled) =>
        SetLimiterToggle("trails", enabled, "Trail limiter");

    public static void SetStripCameras(bool enabled) =>
        SetLimiterToggle("cameras", enabled, "Avatar camera strip");

    public static void SetLimitCloth(bool enabled)
    {
        LimitClothEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitCloth = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Cloth limiter ON (max {MaxCloth()}/avatar)."
            : "[Heavenly] Cloth limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitVideos(bool enabled)
    {
        LimitVideosEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitVideos = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Video limiter ON (max {MaxVideos()}/avatar)."
            : "[Heavenly] Video limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitBlendshapes(bool enabled)
    {
        LimitBlendshapesEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitBlendshapes = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Blendshape limiter ON (max {MaxBlendshapes()} total/avatar)."
            : "[Heavenly] Blendshape limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void SetLimitContacts(bool enabled)
    {
        LimitContactsEnabled = enabled;
        try { Config.HeavenlyConfig.ProtLimitContacts = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Contact limiter ON (max {MaxContacts()}/avatar)."
            : "[Heavenly] Contact limiter OFF.");
        if (enabled)
            ScanAll();
    }

    public static void BlockAvatarByIdKeyboard()
    {
        try
        {
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    string id = (text ?? "").Trim();
                    if (id.Length == 0)
                        return;
                    BlockAvatarById(id, true);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Block keyboard failed: {ex.Message}");
        }
    }

    public static void UnblockAvatarByIdKeyboard()
    {
        try
        {
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    string id = (text ?? "").Trim();
                    if (id.Length == 0)
                        return;
                    BlockAvatarById(id, false);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Unblock keyboard failed: {ex.Message}");
        }
    }

    public static void BlockAvatarById(string avatarId, bool block)
    {
        try
        {
            ABI_RC.Core.Savior.CVRSelfModerationManager? mod = null;
            try { mod = ABI_RC.Core.Savior.MetaPort.Instance?.SelfModerationManager; } catch { }
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                // CVR persists this itself; our JSON list is only for display.
                mod.SetAvatarVisibility(avatarId, !block);
                TrackBlockedAvatar(avatarId, block);
                API.HeavenlyAPI.Toast(block
                    ? "[Heavenly] Avatar blocked (saved by CVR)."
                    : "[Heavenly] Avatar unblocked.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Avatar block failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar block failed: {ex.Message}");
        }
    }

    public static void LogBlockedAvatars()
    {
        try
        {
            List<string> list = BlockedAvatarIds();
            if (list.Count == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No blocked avatars recorded.");
                return;
            }

            API.HeavenlyAPI.Log($"--- Blocked avatars ({list.Count}) ---");
            foreach (string id in list)
                API.HeavenlyAPI.Log(id);
            API.HeavenlyAPI.Toast("[Heavenly] Blocked avatars logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Blocked list failed: {ex.Message}");
        }
    }

    private static List<string> BlockedAvatarIds()
    {
        try
        {
            List<string>? loaded =
                Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(Config.HeavenlyConfig.BlockedAvatarsJson);
            if (loaded == null)
                return new List<string>();
            loaded.RemoveAll(string.IsNullOrEmpty);
            return loaded;
        }
        catch
        {
            return new List<string>();
        }
    }

    private static void TrackBlockedAvatar(string avatarId, bool blocked)
    {
        try
        {
            List<string> list = BlockedAvatarIds();
            if (blocked)
            {
                if (!list.Contains(avatarId))
                    list.Add(avatarId);
            }
            else
            {
                list.RemoveAll(id => id == avatarId);
            }
            Config.HeavenlyConfig.BlockedAvatarsJson = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        }
        catch { }
    }

    public static void SetSkipFriends(bool enabled)
    {
        SkipFriends = enabled;
        try { Config.HeavenlyConfig.ProtSkipFriends = enabled; } catch { }
        API.HeavenlyAPI.Log($"Avatar scans skip friends -> {(enabled ? "ON" : "OFF")}");
    }

    public static void AuditSelectedAvatar()
    {
        try
        {
            string userId = Social.PlayerTarget.CurrentId() ?? "";
            if (userId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No target: use Choose Player or select one in the QuickMenu.");
                return;
            }

            GameObject? root = FindPlayerRoot(userId);
            if (root == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Target avatar not loaded.");
                return;
            }

            LogAudit(Social.PlayerTarget.CurrentName() ?? userId, root);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Audit failed: {ex.Message}");
        }
    }

    public static void AuditMyAvatar()
    {
        try
        {
            GameObject? root = null;
            try
            {
                var controller = ABI_RC.Systems.Movement.BetterBetterCharacterController.Instance;
                if (controller != null)
                    root = controller.gameObject;
            }
            catch { }

            if (root == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local avatar not ready.");
                return;
            }

            LogAudit("My avatar", root);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Audit failed: {ex.Message}");
        }
    }

    private static GameObject? FindPlayerRoot(string userId)
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity != null && entity.Uuid == userId)
                        return entity.PlayerObject;
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>Read-only component census. Never modifies anything.</summary>
    private static void LogAudit(string who, GameObject root)
    {
        try
        {
            int lights = 0, renderers = 0, materials = 0, particles = 0, audios = 0;
            int maxVerts = 0, meshesOver100k = 0, videos = 0, contacts = 0;
            long totalVerts = 0, totalShapes = 0;

            try { lights = root.GetComponentsInChildren<Light>().Length; } catch { }
            try
            {
                Renderer[] found = root.GetComponentsInChildren<Renderer>();
                renderers = found.Length;
                foreach (Renderer renderer in found)
                {
                    if (renderer == null)
                        continue;
                    try { materials += renderer.materials.Length; } catch { }
                    try
                    {
                        Mesh? mesh = null;
                        if (renderer is SkinnedMeshRenderer skinned)
                            mesh = skinned.sharedMesh;
                        else if (renderer is MeshRenderer)
                        {
                            MeshFilter? filter = renderer.GetComponent<MeshFilter>();
                            if (filter != null)
                                mesh = filter.sharedMesh;
                        }
                        if (mesh != null)
                        {
                            int verts = mesh.vertexCount;
                            totalVerts += verts;
                            if (verts > maxVerts)
                                maxVerts = verts;
                            if (verts > 100000)
                                meshesOver100k++;
                        }

                        try
                        {
                            if (renderer is SkinnedMeshRenderer skinnedOnly && skinnedOnly.sharedMesh != null)
                                totalShapes += skinnedOnly.sharedMesh.blendShapeCount;
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch { }
            try { particles = root.GetComponentsInChildren<ParticleSystem>().Length; } catch { }
            try { audios = root.GetComponentsInChildren<AudioSource>().Length; } catch { }
            try { videos = root.GetComponentsInChildren<ABI.CCK.Components.CVRVideoPlayer>().Length; } catch { }
            try { contacts = root.GetComponentsInChildren<NAK.Contacts.ContactBase>().Length; } catch { }

            API.HeavenlyAPI.Log(
                $"--- Avatar audit: {who} ---\n" +
                $"Lights: {lights} | Renderers: {renderers} | Materials: {materials} | " +
                $"Particles: {particles} | AudioSources: {audios} | " +
                $"TotalVerts: {totalVerts} | MaxVerts: {maxVerts} | Meshes>100k: {meshesOver100k}\n" +
                $"Videos: {videos} | Blendshapes: {totalShapes} | Contacts: {contacts}"
            );
            API.HeavenlyAPI.Toast("[Heavenly] Audit logged to console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Audit failed: {ex.Message}");
        }
    }

    public static void ScanNow()
    {
        int players = ScanAll();
        API.HeavenlyAPI.Toast($"[Heavenly] Scanned {players} remote avatar(s). See console.");
    }

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            LimitMaterialsEnabled = Config.HeavenlyConfig.ProtLimitMaterials;
            LimitPolyEnabled = Config.HeavenlyConfig.ProtLimitPoly;
            LimitParticlesEnabled = Config.HeavenlyConfig.ProtLimitParticles;
            LimitAudioEnabled = Config.HeavenlyConfig.ProtLimitAudio;
            LimitConstraintsEnabled = Config.HeavenlyConfig.ProtLimitConstraints;
            LimitCollidersEnabled = Config.HeavenlyConfig.ProtLimitColliders;
            LimitRigidbodiesEnabled = Config.HeavenlyConfig.ProtLimitRigidbodies;
            LimitJointsEnabled = Config.HeavenlyConfig.ProtLimitJoints;
            LimitTrailsEnabled = Config.HeavenlyConfig.ProtLimitTrails;
            StripCamerasEnabled = Config.HeavenlyConfig.ProtStripCameras;
            LimitClothEnabled = Config.HeavenlyConfig.ProtLimitCloth;
            LimitVideosEnabled = Config.HeavenlyConfig.ProtLimitVideos;
            LimitBlendshapesEnabled = Config.HeavenlyConfig.ProtLimitBlendshapes;
            LimitContactsEnabled = Config.HeavenlyConfig.ProtLimitContacts;
            MuteAvatarAudioEnabled = Config.HeavenlyConfig.ProtMuteAvatarAudio;
            AutoRescanEnabled = Config.HeavenlyConfig.ProtAutoRescan;
            AutoHideLaggyEnabled = Config.HeavenlyConfig.ProtAutoHide;
            SafetyNetEnabled = Config.HeavenlyConfig.ProtSafetyNet;
            HideAvatarsEnabled = Config.HeavenlyConfig.ProtHideAvatars;
            BlockPortalsPropsEnabled = Config.HeavenlyConfig.ProtBlockPortals;
            SafeShadersEnabled = Config.HeavenlyConfig.ProtSafeShaders;
            LimitLightsEnabled = Config.HeavenlyConfig.ProtLimitLights;
            SkipFriends = Config.HeavenlyConfig.ProtSkipFriends;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Protection: config load failed: {ex.Message}");
        }

        try
        {
            PlayerAPI.OnPlayerJoined += OnPlayerJoined;
            PlayerAPI.OnPlayerLeft += OnPlayerLeft;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Protection: join hook failed: {ex.Message}");
        }

        try
        {
            ABI_RC.Core.IO.CVRObjectLoader.OnWorldLoadedAfterEnable += OnWorldLoaded;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Protection: world-load hook failed: {ex.Message}");
        }
    }

    private static void OnWorldLoaded(string _)
    {
        // Fresh world, fresh defaults: recapture and re-apply the block flag.
        _origCaptured = false;

        try
        {
            if (BlockPickupsEnabled)
            {
                int count = SetPickupsEnabled(false);
                API.HeavenlyAPI.Log($"Pickups re-blocked on world load ({count}).");
            }
        }
        catch { }

        try
        {
            if (BlockPortalsPropsEnabled)
            {
                var world = ABI.CCK.Components.CVRWorld.Instance;
                if (world != null)
                    ApplyBlockToWorld(world, true);
            }
        }
        catch { }
    }

    private static int MaxMaterials()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxMaterials;
            if (v < 5) return 5;
            if (v > 500) return 500;
            return v;
        }
        catch
        {
            return 100;
        }
    }

    private static int MaxVerts()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxVerts;
            if (v < 10000) return 10000;
            if (v > 10000000) return 10000000;
            return v;
        }
        catch
        {
            return 1000000;
        }
    }

    private static int LimitMaterials(GameObject root)
    {
        int removed = 0;
        try
        {
            List<Renderer> renderers = new List<Renderer>(root.GetComponentsInChildren<Renderer>());
            for (int i = MaxMaterials(); i < renderers.Count; i++)
            {
                try
                {
                    if (renderers[i] == null)
                        continue;
                    UnityEngine.Object.Destroy(renderers[i]);
                    removed++;
                }
                catch { }
            }

            if (removed > 0)
                API.HeavenlyAPI.Log($"Material limiter: removed {removed} renderer(s) on {root.name}.");
        }
        catch { }
        return removed;
    }

    private static int LimitPoly(GameObject root)
    {
        int removed = 0;
        try
        {
            int cap = MaxVerts();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null)
                    continue;

                Mesh? mesh = null;
                try
                {
                    if (renderer is SkinnedMeshRenderer skinned)
                        mesh = skinned.sharedMesh;
                    else if (renderer is MeshRenderer)
                    {
                        MeshFilter? filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null)
                            mesh = filter.sharedMesh;
                    }
                }
                catch { continue; }

                if (mesh == null)
                    continue;

                int verts = 0;
                try { verts = mesh.vertexCount; } catch { continue; }

                if (verts > cap)
                {
                    try
                    {
                        API.HeavenlyAPI.Log($"Poly limiter: {verts} verts on {root.name}, removing renderer.");
                        UnityEngine.Object.Destroy(renderer);
                        removed++;
                    }
                    catch { }
                }
            }
        }
        catch { }
        return removed;
    }

    private static int MaxLights()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxLights;
            if (v < 1) return 1;
            if (v > 32) return 32;
            return v;
        }
        catch
        {
            return 6;
        }
    }

    private static float ScanDelay()
    {
        try
        {
            float v = Config.HeavenlyConfig.ProtScanDelay;
            if (v < 3f) return 3f;
            if (v > 120f) return 120f;
            return v;
        }
        catch
        {
            return 12f;
        }
    }

    /// <summary>Processes delayed auto-scans. Called from HeavenlyPlugin.OnUpdate.</summary>
    public static void OnUpdate()
    {
        CheckSafetyNet();
        EnforceHiddenAvatars();

        try
        {
            if (AutoRescanEnabled && AnyLimiterOn() && Time.time >= _nextRescan)
            {
                _nextRescan = Time.time + 60f;
                ScanAll();
            }
        }
        catch { }

        if (PendingScans.Count == 0)
            return;

        try
        {
            float now = Time.time;
            List<PendingScan> due = new();
            lock (PendingScans)
            {
                for (int i = PendingScans.Count - 1; i >= 0; i--)
                {
                    if (now >= PendingScans[i].DueTime)
                    {
                        due.Add(PendingScans[i]);
                        PendingScans.RemoveAt(i);
                    }
                }
            }

            foreach (PendingScan scan in due)
                ScanPlayer(scan.UserId, true);
        }
        catch { }
    }

    private static float _nextHideEnforce;

    /// <summary>
    /// Re-hides remote avatars every few seconds while enabled. Covers joiners
    /// that arrived before their entity existed and avatar reloads, which reset
    /// visibility. The call is idempotent.
    /// </summary>
    private static void CheckSafetyNet()
    {
        if (!SafetyNetEnabled || Time.time < _nextSafetyCheck)
            return;
        _nextSafetyCheck = Time.time + 1f;

        try
        {
            if (Movement.MovementService.IsFlying || Movement.MovementService.SittingOnPlayer)
                return;

            var world = ABI.CCK.Components.CVRWorld.Instance;
            if (world == null)
                return;

            float floor = 0f;
            try { floor = world.GetRespawnHeight(); } catch { return; }

            Vector3? pos = null;
            try
            {
                var setup = ABI_RC.Core.Player.PlayerSetup.Instance;
                if (setup != null)
                    pos = setup.GetPlayerPosition();
            }
            catch { }
            if (pos == null)
                return;

            if (pos.Value.y < floor - 2f)
            {
                API.HeavenlyAPI.Log($"Safety net: fell to {pos.Value.y:0.0} (floor {floor:0.0}), respawning.");
                Movement.MovementService.Respawn();
            }
        }
        catch { }
    }

    private static void EnforceHiddenAvatars()
    {
        if (!HideAvatarsEnabled || Time.time < _nextHideEnforce)
            return;
        _nextHideEnforce = Time.time + 5f;

        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity?.PuppetMaster == null)
                    continue;
                try { entity.PuppetMaster.SetAvatarVisibility(false); } catch { }
            }
        }
        catch { }
    }

    private static void OnPlayerJoined(Player player)
    {
        try
        {
            string? id = null;
            try { id = player?.UserID; } catch { }
            string scanId = id ?? "";
            if (scanId.Length == 0)
                return;

            if (HideAvatarsEnabled)
                HidePlayerAvatar(scanId);

            if (!AnyLimiterOn())
                return;

            lock (PendingScans)
            {
                PendingScans.Add(new PendingScan
                {
                    UserId = scanId,
                    DueTime = Time.time + ScanDelay()
                });
            }
        }
        catch { }
    }

    private static void HidePlayerAvatar(string userId)
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null || entity.Uuid != userId)
                    continue;
                try { entity.PuppetMaster?.SetAvatarVisibility(false); } catch { }
                return;
            }
        }
        catch { }
    }

    private static void OnPlayerLeft(Player player)
    {
        try
        {
            string? id = null;
            try { id = player?.UserID; } catch { }
            if (string.IsNullOrEmpty(id))
                return;

            lock (PendingScans)
            {
                PendingScans.RemoveAll(s => s.UserId == id);
            }
        }
        catch { }
    }

    private static int ScanAll()
    {
        int count = 0;
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return 0;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null || string.IsNullOrEmpty(entity.Uuid))
                    continue;
                if (ScanPlayer(entity.Uuid, false))
                    count++;
            }

            API.HeavenlyAPI.Log($"Avatar safety scan: {count} avatar(s) processed.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Scan all failed: {ex.Message}");
        }
        return count;
    }

    private static bool ScanPlayer(string userId, bool delayed)
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return false;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null || entity.Uuid != userId)
                    continue;

                if (SkipFriends && IsFriend(userId))
                {
                    API.HeavenlyAPI.Log($"Scan: skipping friend {entity.Username}.");
                    return false;
                }

                GameObject? root = entity.PlayerObject;
                if (root == null)
                    return false;

                int lightsRemoved = 0;
                int shadersFixed = 0;
                int renderersRemoved = 0;
                int polyRemoved = 0;

                if (LimitLightsEnabled)
                    lightsRemoved = LimitLights(root);

                if (SafeShadersEnabled)
                    shadersFixed = DowngradeShaders(root);

                if (LimitMaterialsEnabled)
                    renderersRemoved = LimitMaterials(root);

                if (LimitPolyEnabled)
                    polyRemoved = LimitPoly(root);

                int particlesRemoved = 0;
                int audioRemoved = 0;

                if (LimitParticlesEnabled)
                    particlesRemoved = LimitComponents<ParticleSystem>(root, MaxParticles(), "particle systems");

                if (LimitAudioEnabled)
                    audioRemoved = LimitComponents<AudioSource>(root, MaxAudio(), "audio sources");

                if (MuteAvatarAudioEnabled)
                    audioRemoved += LimitComponents<AudioSource>(root, 0, "avatar audio");

                int constraintsRemoved = 0;
                int collidersRemoved = 0;
                int rigidbodiesRemoved = 0;
                int jointsRemoved = 0;
                int trailsRemoved = 0;
                int camerasRemoved = 0;

                if (LimitConstraintsEnabled)
                    constraintsRemoved = LimitConstraints(root);

                if (LimitCollidersEnabled)
                    collidersRemoved = LimitComponents<Collider>(root, Cap("colliders", 200, 20, 2000), "colliders");

                if (LimitRigidbodiesEnabled)
                    rigidbodiesRemoved = LimitComponents<Rigidbody>(root, Cap("rigidbodies", 30, 5, 200), "rigidbodies");

                if (LimitJointsEnabled)
                    jointsRemoved = LimitComponents<Joint>(root, Cap("joints", 30, 5, 200), "joints");

                if (LimitTrailsEnabled)
                {
                    trailsRemoved = LimitComponents<LineRenderer>(root, Cap("trails", 60, 10, 500), "lines");
                    trailsRemoved += LimitComponents<TrailRenderer>(root, Cap("trails", 60, 10, 500), "trails");
                }

                if (StripCamerasEnabled)
                    camerasRemoved = LimitComponents<Camera>(root, 0, "avatar cameras");

                int clothRemoved = 0;
                int videosRemoved = 0;
                int blendRemoved = 0;
                int contactsRemoved = 0;

                if (LimitVideosEnabled)
                    videosRemoved = LimitComponents<ABI.CCK.Components.CVRVideoPlayer>(root, MaxVideos(), "video players");

                if (LimitBlendshapesEnabled)
                    blendRemoved = LimitBlendshapes(root);

                if (LimitContactsEnabled)
                    contactsRemoved = LimitComponents<NAK.Contacts.ContactBase>(root, MaxContacts(), "contacts");
                if (LimitClothEnabled)
                    clothRemoved = LimitCloth(root);

                if (AutoHideLaggyEnabled)
                    AutoHideCheck(entity, root);

                API.HeavenlyAPI.Log(
                    $"Scan{(delayed ? " (auto)" : "")}: {entity.Username}: " +
                    $"{lightsRemoved} light(s), {shadersFixed} shader(s), " +
                    $"{renderersRemoved} renderer(s), {polyRemoved} poly mesh(es), " +
                    $"{particlesRemoved} particle(s), {audioRemoved} audio, " +
                    $"{constraintsRemoved} constraint(s), {collidersRemoved} collider(s), " +
                    $"{rigidbodiesRemoved} rigid(s), {jointsRemoved} joint(s), " +
                    $"{trailsRemoved} trail(s), {camerasRemoved} camera(s), " +
                    $"{clothRemoved} cloth, {videosRemoved} video(s), " +
                    $"{blendRemoved} blendshape mesh(es), {contactsRemoved} contact(s)."
                );
                return true;
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Scan player failed: {ex.Message}");
        }
        return false;
    }

    private static void AutoHideCheck(CVRPlayerEntity entity, GameObject root)
    {
        try
        {
            if (SkipFriends && IsFriend(entity.Uuid))
                return;

            int lights = 0;
            int maxVerts = 0;
            long totalVerts = 0;
            try { lights = root.GetComponentsInChildren<Light>().Length; } catch { }
            try
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                {
                    if (renderer == null)
                        continue;
                    try
                    {
                        Mesh? mesh = null;
                        if (renderer is SkinnedMeshRenderer skinned)
                            mesh = skinned.sharedMesh;
                        else if (renderer is MeshRenderer)
                        {
                            MeshFilter? filter = renderer.GetComponent<MeshFilter>();
                            if (filter != null)
                                mesh = filter.sharedMesh;
                        }
                        if (mesh != null)
                        {
                            totalVerts += mesh.vertexCount;
                            if (mesh.vertexCount > maxVerts)
                                maxVerts = mesh.vertexCount;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            if (lights > MaxLights() * 2 || maxVerts > MaxVerts() * 2 || totalVerts > (long)MaxVerts() * 10L)
            {
                try { entity.PuppetMaster?.SetAvatarVisibility(false); } catch { }
                API.HeavenlyAPI.Log(
                    $"Auto-hide: {entity.Username} hidden ({lights} lights, {maxVerts} maxVerts, {totalVerts} totalVerts)."
                );
                API.HeavenlyAPI.Toast($"[Heavenly] Hid laggy avatar: {entity.Username}.");
            }
        }
        catch { }
    }

    private static int MaxCloth()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxCloth;
            if (v < 2) return 2;
            if (v > 200) return 200;
            return v;
        }
        catch
        {
            return 12;
        }
    }

    private static int MaxVideos()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxVideos;
            if (v < 0) return 0;
            if (v > 16) return 16;
            return v;
        }
        catch
        {
            return 2;
        }
    }

    private static int MaxBlendshapes()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxBlendshapes;
            if (v < 100) return 100;
            if (v > 20000) return 20000;
            return v;
        }
        catch
        {
            return 2000;
        }
    }

    private static int MaxContacts()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxContacts;
            if (v < 4) return 4;
            if (v > 1024) return 1024;
            return v;
        }
        catch
        {
            return 64;
        }
    }

    private static int LimitBlendshapes(GameObject root)
    {
        int removed = 0;
        try
        {
            int cap = MaxBlendshapes();
            int total = 0;
            List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
            try
            {
                foreach (SkinnedMeshRenderer r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (r == null)
                        continue;
                    renderers.Add(r);
                    try
                    {
                        if (r.sharedMesh != null)
                            total += r.sharedMesh.blendShapeCount;
                    }
                    catch { }
                }
            }
            catch { }

            if (total <= cap)
                return 0;

            // Over budget: drop whole renderers (most blendshapes first).
            renderers.Sort((a, b) =>
            {
                int av = 0, bv = 0;
                try { av = a.sharedMesh != null ? a.sharedMesh.blendShapeCount : 0; } catch { }
                try { bv = b.sharedMesh != null ? b.sharedMesh.blendShapeCount : 0; } catch { }
                return bv.CompareTo(av);
            });

            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (total <= cap)
                    break;
                try
                {
                    int count = 0;
                    try { count = r.sharedMesh != null ? r.sharedMesh.blendShapeCount : 0; } catch { }
                    if (count <= 0)
                        continue;
                    API.HeavenlyAPI.Log($"Blendshape limiter: {count} shapes on {root.name}, removing renderer.");
                    UnityEngine.Object.Destroy(r);
                    total -= count;
                    removed++;
                }
                catch { }
            }
        }
        catch { }
        return removed;
    }

    private static int LimitCloth(GameObject root)
    {
        int removed = 0;
        try
        {
            int cap = MaxCloth();
            List<Component> all = new List<Component>();
            try { all.AddRange(root.GetComponentsInChildren<MagicaCloth.BaseComponent>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<MagicaCloth2.ClothBehaviour>()); } catch { }

            for (int i = cap; i < all.Count; i++)
            {
                try
                {
                    if (all[i] == null)
                        continue;
                    UnityEngine.Object.Destroy(all[i]);
                    removed++;
                }
                catch { }
            }

            if (removed > 0)
                API.HeavenlyAPI.Log($"Limiter: removed {removed} cloth sim(s) on {root.name}.");
        }
        catch { }
        return removed;
    }

    private static int LimitConstraints(GameObject root)
    {
        int removed = 0;
        try
        {
            List<Component> all = new List<Component>();
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.AimConstraint>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.LookAtConstraint>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.PositionConstraint>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.RotationConstraint>()); } catch { }
            try { all.AddRange(root.GetComponentsInChildren<UnityEngine.Animations.ScaleConstraint>()); } catch { }

            int cap = Cap("constraints", 100, 10, 1000);
            for (int i = cap; i < all.Count; i++)
            {
                try
                {
                    if (all[i] == null)
                        continue;
                    UnityEngine.Object.Destroy(all[i]);
                    removed++;
                }
                catch { }
            }

            if (removed > 0)
                API.HeavenlyAPI.Log($"Limiter: removed {removed} constraint(s) on {root.name}.");
        }
        catch { }
        return removed;
    }

    private static int MaxParticles()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxParticles;
            if (v < 5) return 5;
            if (v > 1000) return 1000;
            return v;
        }
        catch
        {
            return 100;
        }
    }

    private static int MaxAudio()
    {
        try
        {
            int v = Config.HeavenlyConfig.ProtMaxAudio;
            if (v < 2) return 2;
            if (v > 128) return 128;
            return v;
        }
        catch
        {
            return 16;
        }
    }

    private static bool AnyLimiterOn()
    {
        try
        {
            return SafeShadersEnabled || LimitLightsEnabled || LimitMaterialsEnabled
                || LimitPolyEnabled || LimitParticlesEnabled || LimitAudioEnabled
                || MuteAvatarAudioEnabled || LimitConstraintsEnabled || LimitCollidersEnabled
                || LimitRigidbodiesEnabled || LimitJointsEnabled || LimitTrailsEnabled
                || StripCamerasEnabled || LimitClothEnabled || LimitVideosEnabled
                || LimitBlendshapesEnabled || LimitContactsEnabled;
        }
        catch
        {
            return false;
        }
    }

    private static int Cap(string which, int fallback, int min, int max)
    {
        try
        {
            int v = fallback;
            switch (which)
            {
                case "constraints": v = Config.HeavenlyConfig.ProtMaxConstraints; break;
                case "colliders": v = Config.HeavenlyConfig.ProtMaxColliders; break;
                case "rigidbodies": v = Config.HeavenlyConfig.ProtMaxRigidbodies; break;
                case "joints": v = Config.HeavenlyConfig.ProtMaxJoints; break;
                case "trails": v = Config.HeavenlyConfig.ProtMaxTrails; break;
            }
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
        catch
        {
            return fallback;
        }
    }

    private static int LimitComponents<T>(GameObject root, int cap, string label) where T : Component
    {
        int removed = 0;
        try
        {
            T[] found = root.GetComponentsInChildren<T>();
            for (int i = cap; i < found.Length; i++)
            {
                try
                {
                    if (found[i] == null)
                        continue;
                    UnityEngine.Object.Destroy(found[i]);
                    removed++;
                }
                catch { }
            }

            if (removed > 0)
                API.HeavenlyAPI.Log($"Limiter: removed {removed} {label} on {root.name}.");
        }
        catch { }
        return removed;
    }

    private static int LimitLights(GameObject root)
    {
        int removed = 0;
        try
        {
            Light[] lights = root.GetComponentsInChildren<Light>();
            for (int i = MaxLights(); i < lights.Length; i++)
            {
                try
                {
                    UnityEngine.Object.Destroy(lights[i]);
                    removed++;
                }
                catch { }
            }

            if (removed > 0)
                API.HeavenlyAPI.Log($"Light limiter: removed {removed} light(s) on {root.name}.");
        }
        catch { }
        return removed;
    }

    private static int DowngradeShaders(GameObject root)
    {
        int fixedCount = 0;
        Shader? standard = null;
        try { standard = Shader.Find("Standard"); } catch { }
        if (standard == null)
            return 0;

        try
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null)
                    continue;

                Material[] materials;
                try { materials = renderer.materials; }
                catch { continue; }

                foreach (Material material in materials)
                {
                    if (material == null || material.shader == null)
                        continue;
                    try
                    {
                        if (material.shader.name != "Standard")
                        {
                            material.shader = standard;
                            fixedCount++;
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
        return fixedCount;
    }

    private static bool IsFriend(string userId)
    {
        try
        {
            return ABI_RC.Core.Networking.IO.Social.Friends.FriendsWith(userId);
        }
        catch
        {
            return false;
        }
    }
}
