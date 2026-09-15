using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HeavenlyCVR.Heavenly.Features.Social;

/// <summary>
/// Local-only player notes (remember who people are). Keyed by user ID,
/// persisted, and shown in the join feed.
/// </summary>
public static class PlayerNotes
{
    private static readonly Dictionary<string, string> Notes = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            Dictionary<string, string>? loaded =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(Config.HeavenlyConfig.PlayerNotesJson);
            if (loaded == null)
                return;

            Notes.Clear();
            foreach (var kv in loaded)
            {
                if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value))
                    continue;
                Notes[kv.Key] = kv.Value;
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Notes load failed: {ex.Message}");
        }
    }

    public static string? GetNote(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                return null;
            lock (Notes)
            {
                if (Notes.TryGetValue(userId, out string? note) && !string.IsNullOrEmpty(note))
                    return note;
            }
        }
        catch { }
        return null;
    }

    public static void NoteSelected()
    {
        try
        {
            string userId = PlayerTarget.CurrentId() ?? "";
            string userName = PlayerTarget.CurrentName() ?? "";
            if (userId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No target: use Choose Player or select one in the QuickMenu.");
                return;
            }

            string existing;
            lock (Notes)
            {
                Notes.TryGetValue(userId, out string? found);
                existing = found ?? "";
            }

            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OpenKeyboard(
                existing,
                text =>
                {
                    try
                    {
                        text = (text ?? "").Trim();
                        lock (Notes)
                        {
                            if (text.Length == 0)
                                Notes.Remove(userId);
                            else
                                Notes[userId] = text;
                        }
                        Persist();
                        API.HeavenlyAPI.Toast(text.Length == 0
                            ? $"[Heavenly] Note cleared for {userName}."
                            : $"[Heavenly] Note saved for {userName}.");
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Warning($"Note save failed: {ex.Message}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Note keyboard failed: {ex.Message}");
        }
    }

    public static void ShowSelectedNote()
    {
        try
        {
            string userId = PlayerTarget.CurrentId() ?? "";
            string userName = PlayerTarget.CurrentName() ?? "";
            if (userId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No target: use Choose Player or select one in the QuickMenu.");
                return;
            }

            string? note = GetNote(userId);
            if (string.IsNullOrEmpty(note))
                API.HeavenlyAPI.Toast($"[Heavenly] No note for {userName}.");
            else
                API.HeavenlyAPI.Toast($"[Heavenly] {userName}: {note}");
            API.HeavenlyAPI.Log($"Note for {userName} [{userId}]: {note ?? "(none)"}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Show note failed: {ex.Message}");
        }
    }

    private static void Persist()
    {
        try
        {
            string json;
            lock (Notes) { json = JsonConvert.SerializeObject(Notes); }
            Config.HeavenlyConfig.PlayerNotesJson = json;
        }
        catch { }
    }
}
