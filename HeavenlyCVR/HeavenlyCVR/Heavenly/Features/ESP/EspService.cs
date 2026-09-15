using System;
using System.Collections.Generic;
using ABI_RC.Core.Player;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.ESP;

public static class EspService
{
    public static bool MasterEnabled { get; private set; }
    public static bool PlayerEsp { get; private set; } = true;
    public static bool PickupEsp { get; private set; }
    public static bool SpawnableEsp { get; private set; }

    /// <summary>Render remote nameplates through walls (overlay).</summary>
    public static bool NameplateEsp { get; private set; }

    public static bool HideNameplatesEnabled { get; private set; }

    private static readonly Dictionary<string, HighlightPlus.HighlightEffect> OutlineHolders = new();
    private static readonly List<GameObject> PropMarkers = new();

    private static float _nextPlayerRefresh;
    private static float _nextPropRefresh;
    private static float _nextNameplateRefresh;
    private static int _playerTick;
    private const float FallbackPlayerRefresh = 3f;
    private const float FallbackPropRefresh = 4f;
    private const float FallbackNameplateRefresh = 5f;
    private const int FallbackPropMax = 50;
    private const float FallbackOutlineWidth = 2f;

    private static float OutlineWidth()
    {
        try
        {
            float scale = Config.HeavenlyConfig.EspPlayerScale;
            float width = FallbackOutlineWidth * scale;
            if (width < 0.2f) return 0.2f;
            if (width > 12f) return 12f;
            return width;
        }
        catch
        {
            return FallbackOutlineWidth;
        }
    }
    private const float FallbackPropSize = 0.25f;

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            PlayerEsp = Config.HeavenlyConfig.EspPlayers;
            if (Config.HeavenlyConfig.EspHideNameplates)
                SetHideNameplates(true);
            PickupEsp = Config.HeavenlyConfig.EspPickups;
            SpawnableEsp = Config.HeavenlyConfig.EspSpawnables;
            NameplateEsp = Config.HeavenlyConfig.EspNameplates;

            if (Config.HeavenlyConfig.EspMaster)
                SetMaster(true);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"ESP init failed: {ex.Message}");
        }
    }
    // Rank colors default to the Nocturnal-style palette; all user-tunable.
    private static Color DefaultColor => CfgColor(() => Config.HeavenlyConfig.EspColorDefault, new Color32(255, 8, 90, 255));
    private static Color FriendsColor => CfgColor(() => Config.HeavenlyConfig.EspColorFriend, new Color32(255, 251, 0, 255));
    private static Color LegendColor => CfgColor(() => Config.HeavenlyConfig.EspColorLegend, new Color32(227, 129, 0, 255));
    private static Color GuideColor => CfgColor(() => Config.HeavenlyConfig.EspColorGuide, new Color32(0, 199, 7, 255));
    private static Color ModColor => CfgColor(() => Config.HeavenlyConfig.EspColorMod, new Color32(158, 0, 29, 255));
    private static Color DevColor => CfgColor(() => Config.HeavenlyConfig.EspColorDev, new Color32(77, 0, 14, 255));

    private static float CfgFloat(float fallback, Func<float> read)
    {
        try { return read(); } catch { return fallback; }
    }

    private static int CfgInt(int fallback, Func<int> read)
    {
        try { return read(); } catch { return fallback; }
    }

    private static Color32 CfgColor(Func<Color32> read, Color32 fallback)
    {
        try { return read(); } catch { return fallback; }
    }

    public static void SetMaster(bool enabled)
    {
        MasterEnabled = enabled;
        try { Config.HeavenlyConfig.EspMaster = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled ? "[Heavenly] ESP ON" : "[Heavenly] ESP OFF");
        if (!enabled)
        {
            ClearAllMarkers();
            ApplyNameplateOverlay(false);
        }
    }

    public static void SetPlayers(bool enabled)
    {
        PlayerEsp = enabled;
        try { Config.HeavenlyConfig.EspPlayers = enabled; } catch { }
        API.HeavenlyAPI.Log($"Player ESP -> {(enabled ? "ON" : "OFF")}");
        if (!enabled || !MasterEnabled)
            ClearPlayerMarkers();
    }

    public static void SetPickups(bool enabled)
    {
        PickupEsp = enabled;
        try { Config.HeavenlyConfig.EspPickups = enabled; } catch { }
        API.HeavenlyAPI.Log($"Pickup ESP -> {(enabled ? "ON" : "OFF")}");
        if (!enabled || !MasterEnabled)
            ClearPropMarkers();
    }

    public static void SetHideNameplates(bool enabled)
    {
        HideNameplatesEnabled = enabled;
        try { Config.HeavenlyConfig.EspHideNameplates = enabled; } catch { }
        try
        {
            ABI_RC.Core.Player.PlayerNameplate.ToggleRemotePlayersNameplates();
            API.HeavenlyAPI.Toast(enabled
                ? "[Heavenly] Remote nameplates hidden."
                : "[Heavenly] Remote nameplates shown.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Nameplate toggle failed: {ex.Message}");
            HideNameplatesEnabled = false;
        }
    }

    public static void SetSpawnables(bool enabled)
    {
        SpawnableEsp = enabled;
        try { Config.HeavenlyConfig.EspSpawnables = enabled; } catch { }
        API.HeavenlyAPI.Log($"Spawnable ESP -> {(enabled ? "ON" : "OFF")}");
        if (!enabled || !MasterEnabled)
            ClearPropMarkers();
    }

    public static void SetNameplates(bool enabled)
    {
        NameplateEsp = enabled;
        try { Config.HeavenlyConfig.EspNameplates = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled ? "[Heavenly] Nameplate ESP ON" : "[Heavenly] Nameplate ESP OFF");
        if (enabled && MasterEnabled)
            ApplyNameplateOverlay(true);
        else
            ApplyNameplateOverlay(false);
    }

    public static void OnUpdate()
    {
        if (!MasterEnabled)
            return;

        try
        {
            if (PlayerEsp && Time.time >= _nextPlayerRefresh)
            {
                _nextPlayerRefresh = Time.time + CfgFloat(FallbackPlayerRefresh, () => Config.HeavenlyConfig.EspPlayerRefresh);
                _playerTick++;
                RefreshPlayerMarkers();
            }

            if (NameplateEsp && Time.time >= _nextNameplateRefresh)
            {
                _nextNameplateRefresh = Time.time + CfgFloat(FallbackNameplateRefresh, () => Config.HeavenlyConfig.EspNameplateRefresh);
                ApplyNameplateOverlay(true);
            }

            if ((PickupEsp || SpawnableEsp) && Time.time >= _nextPropRefresh)
            {
                _nextPropRefresh = Time.time + CfgFloat(FallbackPropRefresh, () => Config.HeavenlyConfig.EspPropRefresh);
                RefreshPropMarkers();
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"ESP update failed: {ex.Message}");
        }
    }

    public static void ClearAllMarkers()
    {
        ClearPlayerMarkers();
        ClearPropMarkers();
    }

    /// <summary>
    /// Rank-colored x-ray outlines via HighlightPlus. The effect component
    /// rides on each avatar holder (removed on toggle-off); only the
    /// nearest N players get outlines for performance.
    /// </summary>
    private static void RefreshPlayerMarkers()
    {
        HashSet<string> seen = new();

        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                Vector3 selfPos = Vector3.zero;
                bool haveSelf = false;
                try
                {
                    var setup = PlayerSetup.Instance;
                    if (setup != null)
                    {
                        selfPos = setup.GetPlayerPosition();
                        haveSelf = true;
                    }
                }
                catch { }

                List<(CVRPlayerEntity entity, float dist)> candidates = new();
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity == null || string.IsNullOrEmpty(entity.Uuid))
                        continue;
                    if (entity.PlayerObject == null)
                        continue;

                    float dist = 0f;
                    if (haveSelf)
                    {
                        try { dist = Vector3.Distance(selfPos, entity.PlayerObject.transform.position); }
                        catch { }
                    }
                    candidates.Add((entity, dist));
                }

                candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

                int max = CfgInt(8, () => Config.HeavenlyConfig.EspOutlineMax);
                if (max < 1) max = 1;

                int taken = 0;
                foreach ((CVRPlayerEntity entity, float _) in candidates)
                {
                    if (taken >= max)
                        break;
                    taken++;
                    seen.Add(entity.Uuid);

                    Color color = RankColor(entity);

                    if (!OutlineHolders.TryGetValue(entity.Uuid, out HighlightPlus.HighlightEffect? fx) || fx == null)
                    {
                        fx = CreateOutline(entity, color);
                        if (fx == null)
                            continue;
                        OutlineHolders[entity.Uuid] = fx;
                    }
                    else
                    {
                        try
                        {
                            fx.overlayColor = color;
                            fx.outlineColor = color;
                            fx.outlineWidth = OutlineWidth();
                            if (!fx.highlighted)
                                fx.SetHighlighted(true);

                            // Refresh() rebuilds target lists and is the expensive
                            // call here: only run it every 10th cycle (~30s) to
                            // catch avatar swaps. Color/width sets are cheap.
                            if (_playerTick % 10 == 0)
                            {
                                try { fx.Refresh(); } catch { }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        catch { }

        try
        {
            List<string> stale = new();
            foreach (var kv in OutlineHolders)
            {
                if (!seen.Contains(kv.Key) || kv.Value == null)
                    stale.Add(kv.Key);
            }

            foreach (string id in stale)
            {
                try
                {
                    if (OutlineHolders[id] != null)
                        UnityEngine.Object.Destroy(OutlineHolders[id]);
                }
                catch { }
                OutlineHolders.Remove(id);
            }
        }
        catch { }
    }

    /// <summary>
    /// Attaches the outline to the avatar holder itself with a Children
    /// group: the asset's designed usage, covering the avatar but not the
    /// nameplate. Reuses a surviving component instead of stacking.
    /// </summary>
    private static HighlightPlus.HighlightEffect? CreateOutline(CVRPlayerEntity entity, Color color)
    {
        try
        {
            GameObject? avatarRoot = null;
            try { avatarRoot = entity.AvatarHolder; } catch { }
            if (avatarRoot == null)
            {
                try { avatarRoot = entity.PlayerObject; } catch { }
            }
            if (avatarRoot == null)
                return null;

            HighlightPlus.HighlightEffect? fx = null;
            try { fx = avatarRoot.GetComponent<HighlightPlus.HighlightEffect>(); } catch { }

            if (fx == null)
            {
                try { fx = avatarRoot.AddComponent<HighlightPlus.HighlightEffect>(); }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"ESP: outline create failed: {ex.Message}");
                    return null;
                }
            }

            if (fx == null)
                return null;

            try
            {
                fx.effectGroup = HighlightPlus.TargetOptions.Children;
                fx.overlay = 1f;
                fx.overlayColor = color;
                fx.outline = 1f;
                fx.outlineColor = color;
                fx.outlineWidth = OutlineWidth();
                fx.outlineVisibility = HighlightPlus.Visibility.AlwaysOnTop;
                fx.glow = 0f;
                fx.fadeInDuration = 0f;
                fx.fadeOutDuration = 0f;
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"ESP: outline setup failed: {ex.Message}");
            }

            try
            {
                if (!fx.highlighted)
                    fx.SetHighlighted(true);
                fx.Refresh();
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"ESP: outline enable failed: {ex.Message}");
            }

            return fx;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"ESP: outline create failed: {ex.Message}");
            return null;
        }
    }

    public static void SetCustomColorEnabled(bool enabled)
    {
        try { Config.HeavenlyConfig.EspUseCustom = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Custom ESP color ON (overrides ranks)."
            : "[Heavenly] Rank ESP colors restored.");
    }

    public static void SetCustomColor(int r, int g, int b)
    {
        try { Config.HeavenlyConfig.SetEspColorCustom(r, g, b); } catch { }
    }

    public static void SetOutlineWidth(float mult)
    {
        if (mult < 0.1f) mult = 0.1f;
        if (mult > 2f) mult = 2f;
        try { Config.HeavenlyConfig.EspPlayerScale = mult; } catch { }
        API.HeavenlyAPI.Log($"Outline width -> {mult:0.00}x");
    }

    private static Color RankColor(CVRPlayerEntity entity)
    {
        try
        {
            try
            {
                if (Config.HeavenlyConfig.EspUseCustom)
                    return Config.HeavenlyConfig.EspColorCustom;
            }
            catch { }

            try
            {
                if (ABI_RC.Core.Networking.IO.Social.Friends.FriendsWith(entity.Uuid))
                    return FriendsColor;
            }
            catch { }

            string rank = "";
            try { rank = entity.ApiUserRank ?? ""; } catch { }
            if (rank.Length == 0)
            {
#pragma warning disable CS0618 // Legacy rank fallback (primary is ApiUserRank).
                try { rank = entity.PlayerDescriptor?.userRank ?? ""; } catch { }
#pragma warning restore CS0618
            }

            switch (rank)
            {
                case "Legend": return LegendColor;
                case "Community Guide": return GuideColor;
                case "Moderator": return ModColor;
                case "Developer": return DevColor;
            }
        }
        catch { }
        return DefaultColor;
    }

    /// <summary>
    /// Through-wall nameplates: sets overlay on every text of each remote
    /// nameplate. Re-applied periodically so joiners and reloaded plates
    /// are covered without Harmony patches.
    /// </summary>
    private static void ApplyNameplateOverlay(bool overlay)
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity?.PlayerNameplate == null)
                    continue;

                try
                {
                    TMPro.TMP_Text[] texts =
                        entity.PlayerNameplate.GetComponentsInChildren<TMPro.TMP_Text>();
                    foreach (TMPro.TMP_Text text in texts)
                    {
                        if (text == null)
                            continue;
                        try { text.isOverlay = overlay; } catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void RefreshPropMarkers()
    {
        ClearPropMarkers();

        try
        {
            if (PickupEsp)
            {
                ABI.CCK.Components.CVRPickupObject[]? pickups = null;
                try { pickups = UnityEngine.Object.FindObjectsOfType<ABI.CCK.Components.CVRPickupObject>(); } catch { }

                if (pickups != null)
                {
                    int count = 0;
                    foreach (var pickup in pickups)
                    {
                        if (pickup == null || count >= CfgInt(FallbackPropMax, () => Config.HeavenlyConfig.EspPropMax)) break;
                        try
                        {
                            GameObject marker = CreateMarker(Color.yellow, CfgFloat(FallbackPropSize, () => Config.HeavenlyConfig.EspPropSize));
                            marker.transform.position = pickup.transform.position + new Vector3(0f, 0.3f, 0f);
                            PropMarkers.Add(marker);
                            count++;
                        }
                        catch { }
                    }
                }
            }

            if (SpawnableEsp)
            {
                ABI.CCK.Components.CVRSpawnable[]? spawnables = null;
                try { spawnables = UnityEngine.Object.FindObjectsOfType<ABI.CCK.Components.CVRSpawnable>(); } catch { }

                if (spawnables != null)
                {
                    int count = PropMarkers.Count;
                    foreach (var spawnable in spawnables)
                    {
                        if (spawnable == null || count >= CfgInt(FallbackPropMax, () => Config.HeavenlyConfig.EspPropMax)) break;
                        try
                        {
                            GameObject marker = CreateMarker(Color.cyan, CfgFloat(FallbackPropSize, () => Config.HeavenlyConfig.EspPropSize));
                            marker.transform.position = spawnable.transform.position + new Vector3(0f, 0.3f, 0f);
                            PropMarkers.Add(marker);
                            count++;
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Prop ESP refresh failed: {ex.Message}");
        }
    }

    private static GameObject CreateMarker(Color color, float size)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        }
        catch
        {
            try { UnityEngine.Object.Destroy(marker.GetComponent<Collider>()); } catch { }
        }

        try
        {
            marker.transform.localScale = new Vector3(size, size, size);
            Renderer? renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                try
                {
                    Shader? unlit = Shader.Find("Unlit/Color");
                    if (unlit != null)
                        renderer.material = new Material(unlit);
                }
                catch { }

                try { renderer.material.color = color; } catch { }
            }
        }
        catch { }

        return marker;
    }

    private static void ClearPlayerMarkers()
    {
        try
        {
            foreach (var kv in OutlineHolders)
            {
                try
                {
                    if (kv.Value != null)
                        UnityEngine.Object.Destroy(kv.Value);
                }
                catch { }
            }
        }
        catch { }
        OutlineHolders.Clear();
    }

    /// <summary>
    /// Logs remote players, their raw ranks, and outline state for debugging.
    /// Run from ESP page > Diagnose ESP.
    /// </summary>
    public static void Diagnose()
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
            {
                API.HeavenlyAPI.Log("ESP diagnose: player list not ready.");
                return;
            }

            API.HeavenlyAPI.Log($"--- ESP diagnose: {manager.NetworkPlayers.Count} remote, {OutlineHolders.Count} outlines ---");
            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null)
                    continue;

                string name = "?";
                try { name = entity.Username ?? "?"; } catch { }
                string rank = "?";
                try { rank = entity.ApiUserRank ?? "?"; } catch { }
                bool friend = false;
                try { friend = ABI_RC.Core.Networking.IO.Social.Friends.FriendsWith(entity.Uuid); } catch { }
                bool hasRoot = false;
                try { hasRoot = entity.PlayerObject != null; } catch { }
                bool hasOutline = false;
                bool highlighted = false;
                try
                {
                    if (OutlineHolders.ContainsKey(entity.Uuid) && OutlineHolders[entity.Uuid] != null)
                    {
                        hasOutline = true;
                        highlighted = OutlineHolders[entity.Uuid].highlighted;
                    }
                }
                catch { }

                API.HeavenlyAPI.Log($"{name}: rank='{rank}' friend={friend} root={hasRoot} outline={hasOutline} on={highlighted}");
            }

            API.HeavenlyAPI.Toast("[Heavenly] ESP diagnose logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"ESP diagnose failed: {ex.Message}");
        }
    }

    private static void ClearPropMarkers()
    {
        try
        {
            foreach (GameObject marker in PropMarkers)
            {
                try
                {
                    if (marker != null)
                        UnityEngine.Object.Destroy(marker);
                }
                catch { }
            }
        }
        catch { }
        PropMarkers.Clear();
    }
}
