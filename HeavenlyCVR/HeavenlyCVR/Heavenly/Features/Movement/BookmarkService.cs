using System;
using System.Collections.Generic;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using Newtonsoft.Json;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Movement;

public sealed class BookmarkEntry
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public string WorldId { get; set; } = "";
    public string At { get; set; } = "";

    public BookmarkEntry() { }
}

/// <summary>
/// Three persisted position bookmarks per world. Teleports are always
/// same-world; recalling elsewhere just warns.
/// </summary>
public static class BookmarkService
{
    public const int SlotCount = 3;

    private static readonly List<BookmarkEntry> Slots = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;
        Load();
    }

    public static void SaveSlot(int slot)
    {
        try
        {
            Vector3? pos = LocalPosition();
            if (pos == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player not ready.");
                return;
            }

            while (Slots.Count <= slot)
                Slots.Add(new BookmarkEntry());

            string worldId = "";
            try { worldId = ABI_RC.Core.Networking.IO.Instancing.Instances.CurrentWorldId ?? ""; } catch { }

            Slots[slot] = new BookmarkEntry
            {
                X = pos.Value.x,
                Y = pos.Value.y,
                Z = pos.Value.z,
                WorldId = worldId,
                At = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            Persist();
            API.HeavenlyAPI.Toast($"[Heavenly] Spot {slot + 1} saved.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Save spot failed: {ex.Message}");
        }
    }

    public static void GoSlot(int slot)
    {
        try
        {
            if (slot < 0 || slot >= Slots.Count || Slots[slot] == null)
            {
                API.HeavenlyAPI.Toast($"[Heavenly] Spot {slot + 1} is empty.");
                return;
            }

            BookmarkEntry mark = Slots[slot];

            string worldId = "";
            try { worldId = ABI_RC.Core.Networking.IO.Instancing.Instances.CurrentWorldId ?? ""; } catch { }
            if (!string.IsNullOrEmpty(mark.WorldId) && mark.WorldId != worldId)
            {
                API.HeavenlyAPI.Toast("[Heavenly] That spot is in another world.");
                return;
            }

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            try
            {
                controller.TeleportPlayerTo(
                    new Vector3(mark.X, mark.Y, mark.Z),
                    false,
                    true,
                    false,
                    null
                );
                API.HeavenlyAPI.Toast($"[Heavenly] Went to spot {slot + 1}.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Spot teleport failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Go to spot failed: {ex.Message}");
        }
    }

    private static Vector3? LocalPosition()
    {
        try
        {
            var setup = PlayerSetup.Instance;
            if (setup != null)
                return setup.GetPlayerPosition();
        }
        catch { }

        try
        {
            var controller = BetterBetterCharacterController.Instance;
            if (controller != null)
                return controller.transform.position;
        }
        catch { }

        return null;
    }

    private static void Load()
    {
        try
        {
            List<BookmarkEntry>? loaded =
                JsonConvert.DeserializeObject<List<BookmarkEntry>>(Config.HeavenlyConfig.BookmarkJson);
            if (loaded == null)
                return;

            Slots.Clear();
            foreach (BookmarkEntry e in loaded)
            {
                if (e == null)
                    continue;
                e.WorldId ??= "";
                e.At ??= "";
                Slots.Add(e);
            }

            while (Slots.Count > SlotCount)
                Slots.RemoveAt(0);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Bookmark load failed: {ex.Message}");
        }
    }

    private static void Persist()
    {
        try
        {
            Config.HeavenlyConfig.BookmarkJson = JsonConvert.SerializeObject(Slots);
        }
        catch { }
    }
}
