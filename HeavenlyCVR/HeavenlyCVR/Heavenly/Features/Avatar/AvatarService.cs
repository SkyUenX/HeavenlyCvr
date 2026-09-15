using System;
using System.Collections.Generic;
using ABI_RC.API;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.UI.UILib;
using Newtonsoft.Json;

namespace HeavenlyCVR.Heavenly.Features.Avatar;

public sealed class AvatarEntry
{
    public string Id { get; set; } = "";
    public string At { get; set; } = "";

    public AvatarEntry() { }
}

public static class AvatarService
{
    private const int MaxHistory = 20;
    private static readonly List<AvatarEntry> History = new();
    private static readonly List<AvatarEntry> Favorites = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        LoadList(Config.HeavenlyConfig.AvatarHistoryJson, History, MaxHistory);
        LoadList(Config.HeavenlyConfig.AvatarFavoritesJson, Favorites, 100);

        try
        {
            string currentId = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid ?? "";
            if (currentId.Length > 0)
                RecordHistory(currentId);
        }
        catch { }
    }

    private static void LoadList(string json, List<AvatarEntry> list, int cap)
    {
        try
        {
            List<AvatarEntry>? loaded = JsonConvert.DeserializeObject<List<AvatarEntry>>(json);
            if (loaded == null)
                return;

            list.Clear();
            foreach (AvatarEntry e in loaded)
            {
                if (e == null || string.IsNullOrEmpty(e.Id))
                    continue;
                e.At ??= "";
                list.Add(e);
            }

            while (list.Count > cap)
                list.RemoveAt(0);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Avatar list load failed: {ex.Message}");
        }
    }

    private static void RecordHistory(string avatarId)
    {
        try
        {
            if (History.Count > 0 && History[History.Count - 1].Id == avatarId)
                return;

            History.RemoveAll(e => e.Id == avatarId);
            History.Add(new AvatarEntry { Id = avatarId, At = DateTime.Now.ToString("yyyy-MM-dd HH:mm") });

            while (History.Count > MaxHistory)
                History.RemoveAt(0);

            Config.HeavenlyConfig.AvatarHistoryJson = JsonConvert.SerializeObject(History);
        }
        catch { }
    }

    public static void LogHistory()
    {
        LogList("Recent avatars", History);
    }

    public static void WearHistoryIndexKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard("", text =>
            {
                if (int.TryParse((text ?? "").Trim(), out int n))
                    WearFromList(History, n, "recent");
            });
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Recent keyboard failed: {ex.Message}");
        }
    }

    public static void FavoriteCurrent()
    {
        try
        {
            string currentId = "";
            try { currentId = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid ?? ""; } catch { }
            if (currentId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current avatar ID unknown.");
                return;
            }

            if (Favorites.Exists(e => e.Id == currentId))
            {
                API.HeavenlyAPI.Toast("[Heavenly] Already favorited.");
                return;
            }

            FavoriteById(currentId);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Favorite failed: {ex.Message}");
        }
    }

    public static void FavoriteByIdKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    string id = (text ?? "").Trim();
                    if (id.Length == 0)
                        return;
                    FavoriteById(id);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Favorite keyboard failed: {ex.Message}");
        }
    }

    public static void FavoriteById(string avatarId)
    {
        try
        {
            if (Favorites.Exists(e => e.Id == avatarId))
            {
                API.HeavenlyAPI.Toast("[Heavenly] Already favorited.");
                return;
            }

            Favorites.Add(new AvatarEntry { Id = avatarId, At = DateTime.Now.ToString("yyyy-MM-dd HH:mm") });
            Config.HeavenlyConfig.AvatarFavoritesJson = JsonConvert.SerializeObject(Favorites);
            API.HeavenlyAPI.Toast("[Heavenly] Favorited avatar.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Favorite failed: {ex.Message}");
        }
    }

    public static void WearRandomFavorite()
    {
        try
        {
            if (Favorites.Count == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No favorites yet.");
                return;
            }

            int idx = new Random().Next(Favorites.Count);
            SwitchTo(Favorites[idx].Id);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Random favorite failed: {ex.Message}");
        }
    }

    public static void LogFavorites()
    {
        LogList("Favorite avatars", Favorites);
    }

    public static void WearFavoriteIndexKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard("", text =>
            {
                if (int.TryParse((text ?? "").Trim(), out int n))
                    WearFromList(Favorites, n, "favorite");
            });
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Favorite keyboard failed: {ex.Message}");
        }
    }

    public static void UnfavoriteIndexKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard("", text =>
            {
                if (!int.TryParse((text ?? "").Trim(), out int n))
                    return;

                int idx = Favorites.Count - n;
                if (n < 1 || idx < 0 || idx >= Favorites.Count)
                {
                    API.HeavenlyAPI.Toast("[Heavenly] Favorite number out of range.");
                    return;
                }

                Favorites.RemoveAt(idx);
                Config.HeavenlyConfig.AvatarFavoritesJson = JsonConvert.SerializeObject(Favorites);
                API.HeavenlyAPI.Toast($"[Heavenly] Removed favorite #{n}.");
            });
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Unfavorite keyboard failed: {ex.Message}");
        }
    }

    private static void LogList(string title, List<AvatarEntry> list)
    {
        try
        {
            if (list.Count == 0)
            {
                API.HeavenlyAPI.Toast($"[Heavenly] No {title.ToLower()} yet.");
                return;
            }

            API.HeavenlyAPI.Log($"--- {title} ({list.Count}) ---");
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int n = list.Count - i;
                API.HeavenlyAPI.Log($"{n}. {list[i].Id} @ {list[i].At}");
            }

            API.HeavenlyAPI.Toast($"[Heavenly] {title} logged. Wear #N from the menu.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar list failed: {ex.Message}");
        }
    }

    private static void WearFromList(List<AvatarEntry> list, int n, string kind)
    {
        try
        {
            int idx = list.Count - n;
            if (n < 1 || idx < 0 || idx >= list.Count)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Number out of range.");
                return;
            }

            SwitchTo(list[idx].Id);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Wear {kind} failed: {ex.Message}");
        }
    }
    public static void SwitchByIdKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                id =>
                {
                    if (string.IsNullOrWhiteSpace(id))
                        return;
                    SwitchTo(id.Trim());
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar keyboard failed: {ex.Message}");
        }
    }

    public static void SwitchTo(string avatarId)
    {
        try
        {
            Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }
            if (local == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player not ready.");
                return;
            }

            bool? result = null;
            try { result = local.SwitchAvatar(avatarId); } catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Switch avatar failed: {ex.Message}");
                return;
            }

            if (result == false)
            {
                API.HeavenlyAPI.Toast($"[Heavenly] Switch rejected for '{avatarId}'.");
            }
            else
            {
                RecordHistory(avatarId);
                API.HeavenlyAPI.Toast($"[Heavenly] Switching avatar to {avatarId}...");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Switch avatar failed: {ex.Message}");
        }
    }

    public static void ReloadCurrent()
    {
        try
        {
            string? current = null;
            try { current = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid; } catch { }
            if (current == null || current.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current avatar ID unknown.");
                return;
            }

            SwitchTo(current);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Reload avatar failed: {ex.Message}");
        }
    }

    public static void LogCurrentId()
    {
        try
        {
            string currentId = "";
            try { currentId = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid ?? ""; } catch { }
            if (currentId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current avatar ID unknown.");
                return;
            }

            API.HeavenlyAPI.Log($"Current avatar ID: {currentId}");
            API.HeavenlyAPI.Toast("[Heavenly] Avatar ID logged to console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar ID failed: {ex.Message}");
        }
    }

    public static void OpenCurrentInHub()
    {
        try
        {
            string currentId = "";
            try { currentId = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid ?? ""; } catch { }
            if (currentId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current avatar ID unknown.");
                return;
            }

            try
            {
                ViewManager.Instance?.OpenAvatarInHub(currentId);
                API.HeavenlyAPI.Toast("[Heavenly] Opened avatar in hub.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Avatar hub failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar hub failed: {ex.Message}");
        }
    }

    public static void OpenCurrentInDetails()
    {
        try
        {
            string? current = null;
            try { current = ABI_RC.Core.Savior.MetaPort.Instance?.currentAvatarGuid; } catch { }
            if (current == null || current.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current avatar ID unknown.");
                return;
            }

            try
            {
                ViewManager.Instance?.RequestAvatarDetailsPage(current);
                API.HeavenlyAPI.Toast("[Heavenly] Opened avatar details (favorite from there).");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Open avatar details failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Open avatar details failed: {ex.Message}");
        }
    }

    public static void SearchKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                query =>
                {
                    if (string.IsNullOrWhiteSpace(query))
                        return;
                    Search(query.Trim());
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar search keyboard failed: {ex.Message}");
        }
    }

    public static void SetParamKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                name =>
                {
                    name = (name ?? "").Trim();
                    if (name.Length == 0)
                        return;

                    QuickMenuAPI.OpenKeyboard(
                        "0",
                        valueText =>
                        {
                            if (!float.TryParse((valueText ?? "").Trim(), out float value))
                            {
                                API.HeavenlyAPI.Toast("[Heavenly] Not a number.");
                                return;
                            }
                            SetParam(name, value);
                        }
                    );
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Param keyboard failed: {ex.Message}");
        }
    }

    public static void FireTriggerKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                name =>
                {
                    name = (name ?? "").Trim();
                    if (name.Length == 0)
                        return;
                    FireTrigger(name);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Trigger keyboard failed: {ex.Message}");
        }
    }

    public static void FireTrigger(string name)
    {
        try
        {
            UnityEngine.Animator? animator = LocalAnimator();
            if (animator == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local avatar animator not ready.");
                return;
            }

            try
            {
                animator.SetTrigger(name);
                API.HeavenlyAPI.Toast($"[Heavenly] Fired trigger '{name}'.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Fire trigger failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Fire trigger failed: {ex.Message}");
        }
    }

    public static void SetParam(string name, float value)
    {
        try
        {
            ABI_RC.API.Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }
            if (local?.Avatar == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local avatar not ready.");
                return;
            }

            try
            {
                local.Avatar.SetParameter(name, value);
                API.HeavenlyAPI.Toast($"[Heavenly] Param '{name}' -> {value:0.###}.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Set param failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Set param failed: {ex.Message}");
        }
    }

    public static void LogCoreParams()
    {
        try
        {
            ABI_RC.API.Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }
            if (local?.AvatarAnimatorManager == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            var mgr = local.AvatarAnimatorManager;
            try
            {
                API.HeavenlyAPI.Log(
                    "--- Avatar core params ---\n" +
                    $"Move=({mgr.MovementX:0.00},{mgr.MovementY:0.00}) " +
                    $"Grounded={mgr.Grounded} Crouch={mgr.Crouching} Prone={mgr.Prone} " +
                    $"Fly={mgr.Flying} Sit={mgr.Sitting}\n" +
                    $"GestureL={mgr.GestureLeft:0.00} GestureR={mgr.GestureRight:0.00} " +
                    $"Toggle={mgr.Toggle} Emote={mgr.Emote} Viseme={mgr.VisemeIdx}"
                );
                API.HeavenlyAPI.Toast("[Heavenly] Core params logged.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Core params failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Core params failed: {ex.Message}");
        }
    }

    private static ABI_RC.Core.Util.AnimatorManager.AvatarAnimatorManager? AnimatorManager()
    {
        try
        {
            ABI_RC.API.Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }
            return local?.AvatarAnimatorManager;
        }
        catch
        {
            return null;
        }
    }

    public static void SetGestureLeft(float value)
    {
        try
        {
            var mgr = AnimatorManager();
            if (mgr == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            try
            {
                mgr.GestureLeft = value;
                API.HeavenlyAPI.Log($"Gesture left -> {value:0.00}");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Gesture failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Gesture failed: {ex.Message}");
        }
    }

    public static void SetGestureRight(float value)
    {
        try
        {
            var mgr = AnimatorManager();
            if (mgr == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            try
            {
                mgr.GestureRight = value;
                API.HeavenlyAPI.Log($"Gesture right -> {value:0.00}");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Gesture failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Gesture failed: {ex.Message}");
        }
    }

    public static void StopEmote()
    {
        try
        {
            var mgr = AnimatorManager();
            if (mgr == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            try
            {
                mgr.CancelEmote = true;
                API.HeavenlyAPI.Toast("[Heavenly] Emote stopped.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Stop emote failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Stop emote failed: {ex.Message}");
        }
    }

    private static UnityEngine.Animator? LocalAnimator()
    {
        try
        {
            var setup = ABI_RC.Core.Player.PlayerSetup.Instance;
            if (setup?.PlayerAvatarParent == null)
                return null;

            try { return setup.PlayerAvatarParent.GetComponentInChildren<UnityEngine.Animator>(); }
            catch { return null; }
        }
        catch
        {
            return null;
        }
    }

    public static void LogAnimatorParams()
    {
        try
        {
            UnityEngine.Animator? animator = LocalAnimator();
            if (animator == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local avatar animator not ready.");
                return;
            }

            UnityEngine.AnimatorControllerParameter[]? parameters = null;
            try { parameters = animator.parameters; }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Param list failed: {ex.Message}");
                return;
            }

            if (parameters == null || parameters.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar has no animator params.");
                return;
            }

            API.HeavenlyAPI.Log($"--- Animator params ({parameters.Length}) ---");
            foreach (UnityEngine.AnimatorControllerParameter param in parameters)
            {
                if (param == null)
                    continue;
                string value = "?";
                try
                {
                    switch (param.type)
                    {
                        case UnityEngine.AnimatorControllerParameterType.Float:
                            value = animator.GetFloat(param.nameHash).ToString("0.###");
                            break;
                        case UnityEngine.AnimatorControllerParameterType.Int:
                            value = animator.GetInteger(param.nameHash).ToString();
                            break;
                        case UnityEngine.AnimatorControllerParameterType.Bool:
                            value = animator.GetBool(param.nameHash).ToString();
                            break;
                        default:
                            value = "(trigger)";
                            break;
                    }
                }
                catch { }
                API.HeavenlyAPI.Log($"{param.name} [{param.type}] = {value}");
            }

            API.HeavenlyAPI.Toast($"[Heavenly] {parameters.Length} params logged. Set any by name.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Param list failed: {ex.Message}");
        }
    }

    public static void SetViseme(float index)
    {
        try
        {
            var mgr = AnimatorManager();
            if (mgr == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            try
            {
                mgr.VisemeIdx = (int)index;
                API.HeavenlyAPI.Log($"Viseme -> {(int)index}");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Viseme failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Viseme failed: {ex.Message}");
        }
    }

    public static void CycleOutfitToggle()
    {
        try
        {
            var mgr = AnimatorManager();
            if (mgr == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Avatar animator not ready.");
                return;
            }

            try
            {
                int next = mgr.Toggle + 1;
                if (next > 8)
                    next = 0;
                mgr.Toggle = next;
                API.HeavenlyAPI.Toast($"[Heavenly] Outfit toggle -> {next} (avatar-defined).");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Outfit toggle failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Outfit toggle failed: {ex.Message}");
        }
    }

    public static void Search(string query)
    {
        try
        {
            ViewManager.Instance?.GetFilteredAvatarsPaged(query, false, 0);
            API.HeavenlyAPI.Toast($"[Heavenly] Searching avatars for '{query}' (see avatar list).");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar search failed: {ex.Message}");
        }
    }
}
