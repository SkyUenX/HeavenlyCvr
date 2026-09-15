using System;
using System.Collections.Generic;
using ABI_RC.Core.Player;
using ABI_RC.Systems.UI.UILib;
using Newtonsoft.Json;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Tags;

public sealed class TagRecord
{
    public bool Enabled { get; set; }
    public string Text { get; set; } = "";
    public int R { get; set; } = 255;
    public int G { get; set; } = 255;
    public int B { get; set; } = 255;
    public string TargetId { get; set; } = "";
    public string TargetName { get; set; } = "";

    public TagRecord() { }
}

/// <summary>
/// Own custom nameplate tags (FewTags-style, self-hosted: no downloads).
/// Each slot clones the target's nameplate content into a "HeavenlyTagN"
/// plate showing your text in your color. Local-only, re-applied on a
/// timer so joins and rebuilt plates are covered without Harmony patches.
/// </summary>
public static class TagService
{
    public const int SlotCount = 3;
    public const string SelfTarget = "self";

    private const string NetId = "Heavenly.Tags";
    private const int NetMaxText = 64;
    private const string RemotePlateName = "HeavenlyNet";

    public static bool ShowRemoteTags { get; private set; } = true;

    private sealed class RemoteTag
    {
        public string Text = "";
        public int R = 255;
        public int G = 255;
        public int B = 255;
    }

    private static readonly Dictionary<string, RemoteTag> RemoteTags = new();
    private static float _nextHeartbeat;
    private static float _pendingBroadcastAt = -1f;

    private static readonly List<TagRecord> Slots = new();
    private static readonly Dictionary<int, GameObject> LastRoots = new();
    private static bool _initialized;
    private static float _nextTick;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        for (int i = 0; i < SlotCount; i++)
            Slots.Add(new TagRecord());

        Load();

        try
        {
            ABI_RC.Systems.ModNetwork.ModNetworkManager.Subscribe(NetId, OnNetMessage);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Tags: net subscribe failed: {ex.Message}");
        }

        try
        {
            ABI_RC.API.PlayerAPI.OnPlayerJoined += _ => QueueBroadcast(15f);
            ABI_RC.API.PlayerAPI.OnPlayerLeft += OnPlayerLeft;
        }
        catch { }

        try
        {
            ABI_RC.Core.IO.CVRObjectLoader.OnWorldLoadedAfterEnable += _ =>
            {
                try { lock (RemoteTags) { RemoteTags.Clear(); } } catch { }
            };
        }
        catch { }
    }

    private static void OnPlayerLeft(ABI_RC.API.Player player)
    {
        try
        {
            string leaveId = "";
            try { leaveId = player?.UserID ?? ""; } catch { }
            if (leaveId.Length == 0)
                return;
            lock (RemoteTags) { RemoteTags.Remove(leaveId); }
        }
        catch { }
    }

    private static void QueueBroadcast(float delaySec)
    {
        try { _pendingBroadcastAt = Time.time + delaySec; } catch { }
    }

    public static TagRecord Slot(int index)
    {
        while (Slots.Count <= index)
            Slots.Add(new TagRecord());
        return Slots[index];
    }

    public static void SetShowRemoteTags(bool enabled)
    {
        ShowRemoteTags = enabled;
        if (!enabled)
            RemoveAllRemotePlates();
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Players' tags ON."
            : "[Heavenly] Players' tags OFF.");
    }

    public static void SetEnabled(int slot, bool enabled)
    {
        TagRecord tag = Slots[slot];
        tag.Enabled = enabled;
        Save();
        QueueBroadcast(2f);
        if (!enabled)
            RemoveSlotClones(slot);
        API.HeavenlyAPI.Toast(enabled
            ? $"[Heavenly] Tag {slot + 1} ON."
            : $"[Heavenly] Tag {slot + 1} OFF.");
    }

    public static void EditText(int slot)
    {
        int captured = slot;
        try
        {
            QuickMenuAPI.OpenKeyboard(
                Slots[captured].Text,
                text =>
                {
                    Slots[captured].Text = text ?? "";
                    Save();
                    QueueBroadcast(2f);
                    RemoveSlotClones(captured);
                    API.HeavenlyAPI.Toast($"[Heavenly] Tag {captured + 1} text set.");
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Tag text keyboard failed: {ex.Message}");
        }
    }

    public static void SetColor(int slot, int r, int g, int b)
    {
        TagRecord tag = Slots[slot];
        tag.R = ClampByte(r);
        tag.G = ClampByte(g);
        tag.B = ClampByte(b);
        Save();
        QueueBroadcast(2f);
    }

    public static void TargetSelf(int slot)
    {
        TagRecord tag = Slots[slot];
        tag.TargetId = SelfTarget;
        tag.TargetName = "Self";
        Save();
        QueueBroadcast(2f);
        RemoveSlotClones(slot);
        API.HeavenlyAPI.Toast($"[Heavenly] Tag {slot + 1} target: Self.");
    }

    public static void TargetChoose(int slot)
    {
        int captured = slot;
        try
        {
            QuickMenuAPI.OpenPlayerSelector(
                $"Heavenly tag {captured + 1} target",
                obj =>
                {
                    try
                    {
                        if (obj == null || string.IsNullOrEmpty(obj.Uuid))
                            return;

                        Slots[captured].TargetId = obj.Uuid;
                        Slots[captured].TargetName = obj.Username ?? obj.Uuid;
                        Save();
                        QueueBroadcast(2f);
                        RemoveSlotClones(captured);
                        API.HeavenlyAPI.Toast($"[Heavenly] Tag {captured + 1} target: {Slots[captured].TargetName}.");
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Warning($"Tag target failed: {ex.Message}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Tag player selector failed: {ex.Message}");
        }
    }

    public static void TargetById(int slot)
    {
        int captured = slot;
        try
        {
            QuickMenuAPI.OpenKeyboard(
                Slots[captured].TargetId == SelfTarget ? "" : Slots[captured].TargetId,
                id =>
                {
                    id = (id ?? "").Trim();
                    if (id.Length == 0)
                        return;

                    Slots[captured].TargetId = id;
                    Slots[captured].TargetName = id;
                    Save();
                    QueueBroadcast(2f);
                    RemoveSlotClones(captured);
                    API.HeavenlyAPI.Toast($"[Heavenly] Tag {captured + 1} target set.");
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Tag ID keyboard failed: {ex.Message}");
        }
    }

    public static void ClearSlot(int slot)
    {
        Slots[slot] = new TagRecord();
        Save();
        QueueBroadcast(2f);
        RemoveSlotClones(slot);
        API.HeavenlyAPI.Toast($"[Heavenly] Tag {slot + 1} cleared.");
    }

    public static void ClearAll()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Slots[i] = new TagRecord();
            RemoveSlotClones(i);
        }
        Save();
        API.HeavenlyAPI.Toast("[Heavenly] All tags cleared.");
    }

    /// <summary>Called from HeavenlyPlugin.OnUpdate.</summary>
    public static void OnUpdate()
    {
        if (!_initialized)
            return;

        try
        {
            if (_pendingBroadcastAt > 0f && Time.time >= _pendingBroadcastAt)
            {
                _pendingBroadcastAt = -1f;
                BroadcastOwn();
            }

            if (Time.time >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.time + 60f;
                BroadcastOwn();
            }
        }
        catch { }

        if (Time.time < _nextTick)
            return;
        _nextTick = Time.time + 4f;

        try
        {
            for (int i = 0; i < SlotCount; i++)
                ApplySlot(i);

            if (ShowRemoteTags)
                ApplyRemoteTags();
        }
        catch { }
    }

    private static void BroadcastOwn()
    {
        try
        {
            TagRecord? self = null;
            for (int i = 0; i < SlotCount && i < Slots.Count; i++)
            {
                TagRecord t = Slots[i];
                if (t != null && t.Enabled && t.Text.Length > 0 && t.TargetId == SelfTarget)
                {
                    self = t;
                    break;
                }
            }

            if (self == null)
                return;

            string text = self.Text.Length > NetMaxText ? self.Text.Substring(0, NetMaxText) : self.Text;
            string payload = JsonConvert.SerializeObject(new TagRecord
            {
                Enabled = true,
                Text = text,
                R = self.R,
                G = self.G,
                B = self.B,
                TargetId = SelfTarget,
                TargetName = ""
            });

            var msg = new ABI_RC.Systems.ModNetwork.ModNetworkMessage(NetId);
            try
            {
                msg.Write(payload);
                msg.Send();
            }
            finally
            {
                try { msg.Dispose(); } catch { }
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Tags: broadcast failed: {ex.Message}");
        }
    }

    private static void OnNetMessage(ABI_RC.Systems.ModNetwork.ModNetworkMessage msg)
    {
        try
        {
            if (msg == null || msg.ID != NetId)
                return;

            string sender = "";
            try { sender = msg.Sender ?? ""; } catch { }
            if (sender.Length == 0)
                return;

            try
            {
                if (sender == PlayerSetup.PlayerLocalId)
                    return;
            }
            catch { }

            string payload = "";
            try { msg.Read(out payload); } catch { }
            if (string.IsNullOrEmpty(payload) || payload.Length > 1024)
                return;

            TagRecord? record = null;
            try { record = JsonConvert.DeserializeObject<TagRecord>(payload); } catch { }
            if (record == null || record.Text.Length == 0)
                return;

            string text = record.Text.Length > NetMaxText ? record.Text.Substring(0, NetMaxText) : record.Text;

            lock (RemoteTags)
            {
                RemoteTags[sender] = new RemoteTag { Text = text, R = record.R, G = record.G, B = record.B };
            }
        }
        catch { }
    }

    private static void ApplyRemoteTags()
    {
        List<KeyValuePair<string, RemoteTag>> copy;
        lock (RemoteTags) { copy = new List<KeyValuePair<string, RemoteTag>>(RemoteTags); }

        foreach (var kv in copy)
        {
            try
            {
                if (!TryGetCanvasForUser(kv.Key, out Transform? canvas) || canvas == null)
                    continue;

                GameObject? plate = null;
                try
                {
                    Transform? found = canvas.Find(RemotePlateName);
                    if (found != null)
                        plate = found.gameObject;
                }
                catch { }

                if (plate == null)
                    plate = ClonePlate(canvas, RemotePlateName, SlotCount);

                if (plate == null)
                    continue;

                try
                {
                    Vector3 wantPos = PlatePosition(SlotCount);
                    if ((plate.transform.localPosition - wantPos).sqrMagnitude > 0.000001f)
                        plate.transform.localPosition = wantPos;
                }
                catch { }

                try
                {
                    TMPro.TMP_Text? tmp = FindTagText(plate);
                    if (tmp == null)
                        continue;

                    if (tmp.text != kv.Value.Text)
                        tmp.text = kv.Value.Text;
                    Color32 want = new Color32((byte)kv.Value.R, (byte)kv.Value.G, (byte)kv.Value.B, 255);
                    if (!ColorsEqual(tmp.color, want))
                        tmp.color = want;
                }
                catch { }
            }
            catch { }
        }
    }

    private static bool TryGetCanvasForUser(string userId, out Transform? canvas)
    {
        canvas = null;
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return false;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null || entity.Uuid != userId)
                    continue;

                try
                {
                    if (entity.PlayerNameplate != null)
                    {
                        canvas = CanvasFromPlate(entity.PlayerNameplate.transform);
                        if (canvas != null)
                            return true;
                    }
                }
                catch { }

                try
                {
                    if (entity.PlayerObject != null)
                    {
                        canvas = CanvasFromRoot(entity.PlayerObject);
                        if (canvas != null)
                            return true;
                    }
                }
                catch { }

                return false;
            }
        }
        catch { }
        return false;
    }

    private static void RemoveAllRemotePlates()
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
                return;

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity?.PlayerObject == null)
                    continue;
                try
                {
                    foreach (Transform canvas in AllCanvasesFor(entity.PlayerObject))
                    {
                        try
                        {
                            Transform? found = canvas.Find(RemotePlateName);
                            if (found != null)
                                UnityEngine.Object.Destroy(found.gameObject);
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static List<Transform> AllCanvasesFor(GameObject root)
    {
        List<Transform> result = new();
        try
        {
            foreach (PlayerNameplate plate in root.GetComponentsInChildren<PlayerNameplate>())
            {
                if (plate == null)
                    continue;
                try
                {
                    Transform? canvas = CanvasFromPlate(plate.transform);
                    if (canvas != null && !result.Contains(canvas))
                        result.Add(canvas);
                }
                catch { }
            }

            Transform? fallback = CanvasFromRoot(root);
            if (fallback != null && !result.Contains(fallback))
                result.Add(fallback);
        }
        catch { }
        return result;
    }

    private static void ApplySlot(int slot)
    {
        TagRecord tag = Slots[slot];
        if (!tag.Enabled || tag.Text.Length == 0 || tag.TargetId.Length == 0)
            return;

        if (!TryGetCanvas(tag, out Transform? canvas, out string why) || canvas == null)
        {
            WarnThrottled(slot, $"no nameplate canvas ({why})");
            return;
        }

        if (LastRoots.TryGetValue(slot, out GameObject? last)
            && last != null && last != canvas.gameObject)
            RemoveSlotClones(slot);
        LastRoots[slot] = canvas.gameObject;

        string plateName = $"HeavenlyTag{slot}";
        GameObject? plate = null;
        try
        {
            Transform? found = canvas.Find(plateName);
            if (found != null)
                plate = found.gameObject;
        }
        catch { }

        if (plate == null)
            plate = ClonePlate(canvas, plateName, slot);

        if (plate == null)
        {
            WarnThrottled(slot, "plate clone failed (see warnings above)");
            return;
        }

        // Live-sync text + color so RGB sliders apply instantly.
        try
        {
            try
            {
                Vector3 wantPos = PlatePosition(slot);
                if ((plate.transform.localPosition - wantPos).sqrMagnitude > 0.000001f)
                    plate.transform.localPosition = wantPos;
            }
            catch { }

            TMPro.TMP_Text? tmp = FindTagText(plate);
            if (tmp == null)
            {
                WarnThrottled(slot, "plate has no text component");
                return;
            }

            if (tmp.text != tag.Text)
                tmp.text = tag.Text;
            Color32 want = new Color32((byte)tag.R, (byte)tag.G, (byte)tag.B, 255);
            if (!ColorsEqual(tmp.color, want))
                tmp.color = want;
        }
        catch { }
    }

    private static readonly Dictionary<int, float> NextWarn = new();

    private static void WarnThrottled(int slot, string reason)
    {
        try
        {
            float now = Time.time;
            lock (NextWarn)
            {
                if (NextWarn.TryGetValue(slot, out float at) && now < at)
                    return;
                NextWarn[slot] = now + 30f;
            }

            API.HeavenlyAPI.Warning($"Tags: slot {slot + 1} idle: {reason}. Run Tags > Diagnose Nameplates.");
        }
        catch { }
    }

    /// <summary>
    /// Finds the nameplate canvas for a target without assuming one fixed
    /// hierarchy: tries the entity's PlayerNameplate component first, then
    /// the legacy [NamePlate]/Canvas path, then a shallow name scan.
    /// </summary>
    private static bool TryGetCanvas(TagRecord tag, out Transform? canvas, out string why)
    {
        canvas = null;
        why = "unknown";

        try
        {
            if (tag.TargetId == SelfTarget)
                return TryLocalCanvas(out canvas, out why);

            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
            {
                why = "player list not ready";
                return false;
            }

            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null || entity.Uuid != tag.TargetId)
                    continue;

                try
                {
                    if (entity.PlayerNameplate != null)
                    {
                        canvas = CanvasFromPlate(entity.PlayerNameplate.transform);
                        if (canvas != null)
                        {
                            why = "";
                            return true;
                        }
                    }
                }
                catch { }

                try
                {
                    if (entity.PlayerObject != null)
                    {
                        canvas = CanvasFromRoot(entity.PlayerObject);
                        if (canvas != null)
                        {
                            why = "";
                            return true;
                        }
                    }
                }
                catch { }

                why = "target has no nameplate canvas (see Diagnose)";
                return false;
            }

            // Not among remotes: the target may be the local user, picked via
            // Choose instead of Self (own UUID never appears in that list).
            if (IsLocalUser(tag.TargetId, tag.TargetName))
                return TryLocalCanvas(out canvas, out why);

            why = "target not in instance";
            return false;
        }
        catch (Exception ex)
        {
            why = ex.Message;
            return false;
        }
    }

    private static Transform? CanvasFromPlate(Transform plate)
    {
        try
        {
            Transform? canvas = plate.Find("Canvas");
            if (canvas != null)
                return canvas;
        }
        catch { }

        try
        {
            // Plate transform itself may hold Content already.
            if (plate.Find("Content") != null)
                return plate;
        }
        catch { }

        // Current builds nest the canvas deeper (e.g. under [Overhead]).
        try
        {
            Transform? deep = DeepFind(plate, "Canvas");
            if (deep != null)
                return deep;
        }
        catch { }

        return null;
    }

    private static Transform? DeepFind(Transform root, string name)
    {
        try
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == name)
                    return t;
            }
        }
        catch { }
        return null;
    }

    private static Transform? CanvasFromRoot(GameObject root)
    {
        try
        {
            Transform? legacy = root.transform.Find("[NamePlate]/Canvas");
            if (legacy != null)
                return legacy;
        }
        catch { }

        try
        {
            foreach (PlayerNameplate plate in root.GetComponentsInChildren<PlayerNameplate>())
            {
                if (plate == null)
                    continue;
                Transform? canvas = CanvasFromPlate(plate.transform);
                if (canvas != null)
                    return canvas;
            }
        }
        catch { }

        // Current builds nest the plate under an [Overhead] subtree.
        try
        {
            Transform? overhead = root.transform.Find("[Overhead]");
            if (overhead == null)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t.name == "[Overhead]")
                    {
                        overhead = t;
                        break;
                    }
                }
            }

            if (overhead != null)
            {
                Transform? deep = DeepFind(overhead, "Canvas");
                if (deep != null)
                    return deep;
            }
        }
        catch { }

        // Last resort: any Canvas anywhere under the root.
        try
        {
            Transform? deep = DeepFind(root.transform, "Canvas");
            if (deep != null)
                return deep;
        }
        catch { }

        return null;
    }

    public static PlayerNameplate? LocalNameplate()
    {
        try
        {
            var controller = ABI_RC.Systems.Movement.BetterBetterCharacterController.Instance;
            if (controller != null)
            {
                PlayerNameplate? plate = controller.GetComponentInChildren<PlayerNameplate>();
                if (plate != null)
                    return plate;
            }
        }
        catch { }

        try
        {
            GameObject? named = GameObject.Find("_PLAYERLOCAL");
            if (named != null)
            {
                PlayerNameplate? plate = named.GetComponentInChildren<PlayerNameplate>();
                if (plate != null)
                    return plate;
            }
        }
        catch { }

        return null;
    }

    private static bool TryLocalCanvas(out Transform? canvas, out string why)
    {
        canvas = null;
        why = "local nameplate not found";

        try
        {
            PlayerNameplate? local = LocalNameplate();
            if (local != null)
            {
                canvas = CanvasFromPlate(local.transform);
                if (canvas != null)
                {
                    why = "";
                    return true;
                }
            }

            foreach (GameObject root in LocalRoots())
            {
                if (root == null)
                    continue;
                canvas = CanvasFromRoot(root);
                if (canvas != null)
                {
                    why = "";
                    return true;
                }
            }

            why = "local plate has no canvas (see Diagnose)";
            return false;
        }
        catch (Exception ex)
        {
            why = ex.Message;
            return false;
        }
    }

    private static List<GameObject> LocalRoots()
    {
        List<GameObject> roots = new();

        try
        {
            var controller = ABI_RC.Systems.Movement.BetterBetterCharacterController.Instance;
            if (controller != null)
                roots.Add(controller.gameObject);
        }
        catch { }

        try
        {
            GameObject? named = GameObject.Find("_PLAYERLOCAL");
            if (named != null && !roots.Contains(named))
                roots.Add(named);
        }
        catch { }

        try
        {
            if (PlayerSetup.Instance != null && !roots.Contains(PlayerSetup.Instance.gameObject))
                roots.Add(PlayerSetup.Instance.gameObject);
        }
        catch { }

        return roots;
    }

    private static bool IsLocalUser(string targetId, string targetName)
    {
        try
        {
            var setup = PlayerSetup.Instance;
            if (setup == null)
                return false;

            try
            {
                if (!string.IsNullOrEmpty(targetId) && targetId == PlayerSetup.PlayerLocalId)
                    return true;
            }
            catch { }

            try
            {
                string? localName = setup.PlayerUsername;
                if (!string.IsNullOrEmpty(targetName) && targetName == localName)
                    return true;
            }
            catch { }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Logs the real nameplate hierarchy (local + remotes) so paths can be
    /// fixed to the current CVR version. Run from Tags > Diagnose Nameplates.
    /// </summary>
    public static void Diagnose()
    {
        try
        {
            API.HeavenlyAPI.Log("--- Tags diagnose: local ---");
            PlayerNameplate? local = LocalNameplate();
            if (local == null)
            {
                API.HeavenlyAPI.Log("local nameplate: NOT FOUND (controller/_PLAYERLOCAL scan failed)");
                try
                {
                    var controller = ABI_RC.Systems.Movement.BetterBetterCharacterController.Instance;
                    API.HeavenlyAPI.Log($"controller: {(controller != null ? controller.gameObject.name : "null")}");
                }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Log($"controller lookup threw: {ex.Message}");
                }
            }
            else
            {
                DumpPlate("local", local.transform);

                try
                {
                    if (TryLocalCanvas(out Transform? dbgCanvas, out _) && dbgCanvas != null)
                        DumpPlates(dbgCanvas);
                }
                catch { }
            }

            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers == null)
            {
                API.HeavenlyAPI.Log("remote list: not ready");
                return;
            }

            int shown = 0;
            foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
            {
                if (entity == null)
                    continue;
                if (shown >= 3)
                {
                    API.HeavenlyAPI.Log($"... and more (total remote: {manager.NetworkPlayers.Count})");
                    break;
                }
                shown++;

                string userName = "?";
                try { userName = entity.Username ?? "?"; } catch { }
                API.HeavenlyAPI.Log($"--- Tags diagnose: {userName} [{entity.Uuid}] ---");
                try
                {
                    API.HeavenlyAPI.Log($"PlayerObject: {(entity.PlayerObject != null ? entity.PlayerObject.name : "null")}");
                }
                catch { }

                PlayerNameplate? plate = null;
                try { plate = entity.PlayerNameplate; } catch { }
                API.HeavenlyAPI.Log($"entity.PlayerNameplate: {(plate != null ? plate.gameObject.name : "null")}");

                if (plate != null)
                    DumpPlate(userName, plate.transform);
                else if (entity.PlayerObject != null)
                    DumpChildren(entity.PlayerObject.transform, 0);

                try
                {
                    if (TryGetCanvasForUser(entity.Uuid, out Transform? dbgCanvas) && dbgCanvas != null)
                        DumpPlates(dbgCanvas);
                }
                catch { }
            }

            API.HeavenlyAPI.Toast("[Heavenly] Diagnose done. See console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Tags diagnose failed: {ex.Message}");
        }
    }

    private static void DumpPlates(Transform canvas)
    {
        try
        {
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform? child = null;
                try { child = canvas.GetChild(i); } catch { continue; }
                if (child == null || !child.name.StartsWith("Heavenly"))
                    continue;

                bool active = false;
                try { active = child.gameObject.activeInHierarchy; } catch { }
                string text = "";
                try
                {
                    TMPro.TMP_Text? tmp = FindTagText(child.gameObject);
                    if (tmp != null)
                        text = tmp.text;
                }
                catch { }

                API.HeavenlyAPI.Log(
                    $"plate '{child.name}': pos={child.localPosition} scale={child.localScale} " +
                    $"active={active} text='{text}'"
                );
            }
        }
        catch { }
    }

    private static void DumpPlate(string who, Transform plate)
    {
        try
        {
            API.HeavenlyAPI.Log($"{who} plate: {FullPath(plate)}");
            DumpChildren(plate, 0);

            // Childless plate: the interesting objects are siblings/parent.
            if (plate.childCount == 0 && plate.parent != null)
            {
                API.HeavenlyAPI.Log($"{who} plate parent: {FullPath(plate.parent)}");
                DumpChildren(plate.parent, 0);
            }
        }
        catch { }
    }

    private static void DumpChildren(Transform t, int depth)
    {
        if (depth > 1)
            return;
        try
        {
            for (int i = 0; i < t.childCount && i < 25; i++)
            {
                Transform? child = null;
                try { child = t.GetChild(i); } catch { continue; }
                if (child == null)
                    continue;
                string extra = "";
                try
                {
                    if (child.GetComponent<TMPro.TMP_Text>() != null)
                        extra = " [HAS TMP_TEXT]";
                }
                catch { }
                API.HeavenlyAPI.Log($"{new string(' ', depth * 2)}- {child.name}{extra}");
                DumpChildren(child, depth + 1);
            }
        }
        catch { }
    }

    private static string FullPath(Transform t)
    {
        try
        {
            string path = t.name;
            Transform? p = t.parent;
            int hops = 0;
            while (p != null && hops < 6)
            {
                path = p.name + "/" + path;
                p = p.parent;
                hops++;
            }
            return path;
        }
        catch
        {
            return "?";
        }
    }

    private static Vector3 PlatePosition(int slot)
    {
        float baseY = -0.12f, gap = 0.07f;
        try { baseY = Config.HeavenlyConfig.TagBaseY; } catch { }
        try { gap = Config.HeavenlyConfig.TagGap; } catch { }
        if (baseY < -1f) baseY = -1f;
        if (baseY > 0.5f) baseY = 0.5f;
        if (gap < 0.02f) gap = 0.02f;
        if (gap > 0.5f) gap = 0.5f;
        return new Vector3(0f, baseY - slot * gap, 0f);
    }

    private static GameObject? ClonePlate(Transform canvas, string plateName, int slot)
    {
        try
        {
            Transform? content = FindContent(canvas);
            if (content == null)
            {
                API.HeavenlyAPI.Warning($"Tags: nameplate has no Content to clone (slot {slot + 1}). Run Diagnose Nameplates.");
                return null;
            }

            GameObject plate = UnityEngine.Object.Instantiate(content.gameObject, canvas);
            plate.name = plateName;
            plate.transform.localPosition = PlatePosition(slot);
            plate.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            try
            {
                Transform? disableWithMenu = plate.transform.Find("Disable with Menu");
                if (disableWithMenu != null)
                    UnityEngine.Object.Destroy(disableWithMenu.gameObject);
            }
            catch { }

            try
            {
                Transform? image = plate.transform.Find("Image");
                if (image != null)
                    UnityEngine.Object.Destroy(image.gameObject);
            }
            catch { }

            return plate;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Tags: clone failed (slot {slot + 1}): {ex.Message}");
            return null;
        }
    }

    private static Transform? FindContent(Transform canvas)
    {
        try
        {
            Transform? named = canvas.Find("Content");
            if (named != null)
                return named;
        }
        catch { }

        // Fallback: first direct child that holds any TMP text.
        try
        {
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform? child = null;
                try { child = canvas.GetChild(i); } catch { continue; }
                if (child == null || child.name.StartsWith("HeavenlyTag"))
                    continue;
                try
                {
                    if (child.GetComponentsInChildren<TMPro.TMP_Text>().Length > 0)
                        return child;
                }
                catch { }
            }
        }
        catch { }

        return null;
    }

    private static TMPro.TMP_Text? FindTagText(GameObject plate)
    {
        try
        {
            Transform? named = plate.transform.Find("TMP:Username");
            if (named != null)
            {
                TMPro.TMP_Text? tmp = named.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                    return tmp;
            }
        }
        catch { }

        try
        {
            TMPro.TMP_Text[] all = plate.GetComponentsInChildren<TMPro.TMP_Text>();
            if (all.Length > 0)
                return all[0];
        }
        catch { }

        return null;
    }

    private static void RemoveSlotClones(int slot)
    {
        string plateName = $"HeavenlyTag{slot}";
        try
        {
            foreach (Transform canvas in AllCanvases())
            {
                if (canvas == null)
                    continue;
                try
                {
                    Transform? found = canvas.Find(plateName);
                    if (found != null)
                        UnityEngine.Object.Destroy(found.gameObject);
                }
                catch { }
            }
        }
        catch { }

        LastRoots.Remove(slot);
    }

    private static List<Transform> AllCanvases()
    {
        List<Transform> canvases = new();

        try
        {
            PlayerNameplate? local = LocalNameplate();
            if (local != null)
            {
                Transform? canvas = CanvasFromPlate(local.transform);
                if (canvas != null)
                    canvases.Add(canvas);
            }
        }
        catch { }

        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity == null)
                        continue;
                    try
                    {
                        Transform? canvas = null;
                        if (entity.PlayerNameplate != null)
                            canvas = CanvasFromPlate(entity.PlayerNameplate.transform);
                        if (canvas == null && entity.PlayerObject != null)
                            canvas = CanvasFromRoot(entity.PlayerObject);
                        if (canvas != null)
                            canvases.Add(canvas);
                    }
                    catch { }
                }
            }
        }
        catch { }

        return canvases;
    }

    private static bool ColorsEqual(Color a, Color32 b)
    {
        return Math.Abs(a.r - b.r / 255f) < 0.004f
            && Math.Abs(a.g - b.g / 255f) < 0.004f
            && Math.Abs(a.b - b.b / 255f) < 0.004f;
    }

    private static int ClampByte(int v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return v;
    }

    private static void Load()
    {
        try
        {
            string json = Config.HeavenlyConfig.TagsJson;
            List<TagRecord>? loaded = JsonConvert.DeserializeObject<List<TagRecord>>(json);
            if (loaded == null)
                return;

            for (int i = 0; i < SlotCount && i < loaded.Count; i++)
            {
                if (loaded[i] == null)
                    continue;
                loaded[i].Text ??= "";
                loaded[i].TargetId ??= "";
                loaded[i].TargetName ??= "";
                loaded[i].R = ClampByte(loaded[i].R);
                loaded[i].G = ClampByte(loaded[i].G);
                loaded[i].B = ClampByte(loaded[i].B);
                Slots[i] = loaded[i];
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Tags: load failed: {ex.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            Config.HeavenlyConfig.TagsJson = JsonConvert.SerializeObject(Slots);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Tags: save failed: {ex.Message}");
        }
    }
}
