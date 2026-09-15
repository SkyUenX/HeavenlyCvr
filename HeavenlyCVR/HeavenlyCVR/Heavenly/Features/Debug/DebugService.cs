using System;
using System.Collections.Generic;
using ABI_RC.API;

namespace HeavenlyCVR.Heavenly.Features.Debug;

public static class DebugService
{
    private static readonly List<string> Feed = new();

    private static API.HeavenlyLabel? _feedLabel;
    private static bool _feedLabelDirty;

    /// <summary>Called once by the menu builder with the Debug page label.</summary>
    public static void SetFeedLabel(API.HeavenlyLabel label)
    {
        _feedLabel = label;
        _feedLabelDirty = true;
    }

    private static int MaxFeed()
    {
        try
        {
            int v = Config.HeavenlyConfig.FeedLines;
            if (v < 5) return 5;
            if (v > 200) return 200;
            return v;
        }
        catch
        {
            return 30;
        }
    }

    private static readonly Dictionary<string, string> KnownNames = new();
    private static readonly HashSet<string> UniqueSeen = new();

    public static int JoinCount { get; private set; }
    public static int LeaveCount { get; private set; }

    private static DateTime _startTime = DateTime.Now;

    public static string Uptime()
    {
        try
        {
            TimeSpan up = DateTime.Now - _startTime;
            return $"{(int)up.TotalHours:0}:{up.Minutes:00}:{up.Seconds:00}";
        }
        catch
        {
            return "?";
        }
    }

    public static bool JoinToasts { get; private set; }

    public static void SetJoinToasts(bool enabled)
    {
        JoinToasts = enabled;
        try { Config.HeavenlyConfig.DbgJoinToasts = enabled; } catch { }
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Join/leave toasts ON."
            : "[Heavenly] Join/leave toasts OFF.");
    }

    private static float _fpsAccum;
    private static int _fpsFrames;
    private static float _fpsValue;
    private static float _fpsNextCalc;
    private static int _appliedFpsCap = int.MinValue;
    private static float _nextFpsCapApply;

    public static int Fps => (int)_fpsValue;

    /// <summary>Called every frame from HeavenlyPlugin.OnUpdate.</summary>
    public static void OnUpdate()
    {
        try
        {
            if (_feedLabelDirty)
            {
                _feedLabelDirty = false;
                FlushFeedLabel();
            }
        }
        catch { }

        try
        {
            float dt = UnityEngine.Time.deltaTime;
            if (dt > 0f && dt < 1f)
            {
                _fpsAccum += dt;
                _fpsFrames++;
            }

            if (UnityEngine.Time.time >= _fpsNextCalc && _fpsFrames > 0)
            {
                _fpsNextCalc = UnityEngine.Time.time + 1f;
                _fpsValue = _fpsFrames / _fpsAccum;
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }

            if (UnityEngine.Time.time >= _nextFpsCapApply)
            {
                _nextFpsCapApply = UnityEngine.Time.time + 5f;
                ApplyFpsCap();
            }
        }
        catch { }
    }

    public static void SetFpsCap(float cap)
    {
        int v = (int)cap;
        if (v < 0) v = 0;
        if (v > 480) v = 480;
        try { Config.HeavenlyConfig.FpsCap = v; } catch { }
        ApplyFpsCap();
        API.HeavenlyAPI.Toast(v <= 0 ? "[Heavenly] FPS cap off." : $"[Heavenly] FPS capped at {v}.");
    }

    private static void ApplyFpsCap()
    {
        try
        {
            int want = Config.HeavenlyConfig.FpsCap;
            if (want == _appliedFpsCap)
                return;
            _appliedFpsCap = want;
            UnityEngine.Application.targetFrameRate = want <= 0 ? -1 : want;
            API.HeavenlyAPI.Log($"FPS cap -> {(want <= 0 ? "off" : want.ToString())}");
        }
        catch { }
    }

    public static int Ping()
    {
        try
        {
            var nm = ABI_RC.Core.Networking.NetworkManager.Instance;
            if (nm != null)
                return nm.GameNetworkPing;
        }
        catch { }
        return -1;
    }

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            try
            {
                JoinToasts = Config.HeavenlyConfig.DbgJoinToasts;
            }
            catch { }

            try
            {
                PlayerAPI.OnPlayerJoined += OnJoined;
                PlayerAPI.OnPlayerLeft += OnLeft;
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"Debug: player event hook failed: {ex.Message}");
            }

            try
            {
                InstancesAPI.OnInstanceConnected += () => Push("Connected to instance.");
                InstancesAPI.OnInstanceDisconnected += () => Push("Disconnected from instance.");
            }
            catch { }

            Push($"HeavenlyCVR v{API.HeavenlyAPI.Version} debug feed started.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Debug init failed: {ex.Message}");
        }
    }

    private static void OnJoined(Player player)
    {
        try
        {
            string? id = null;
            string? name = null;
            try { id = player?.UserID; } catch { }
            try { name = player?.Username; } catch { }

            string joinId = id ?? "";
            string joinName = name ?? "";

            if (joinId.Length > 0)
            {
                lock (KnownNames) { KnownNames[joinId] = joinName.Length > 0 ? joinName : joinId; }
                lock (UniqueSeen) { UniqueSeen.Add(joinId); }
            }

            string display = joinName.Length > 0 ? joinName : (joinId.Length > 0 ? joinId : "???");
            JoinCount++;

            string note = "";
            try
            {
                string? found = joinId.Length > 0 ? Social.PlayerNotes.GetNote(joinId) : null;
                if (!string.IsNullOrEmpty(found))
                    note = $" (note: {found})";
            }
            catch { }

            Push($"{display} joined{note} (total joins: {JoinCount})");
            API.HeavenlyAPI.Log($"[Join] {display}{note}");

            if (IsFriend(joinId))
                API.HeavenlyAPI.Toast($"[Heavenly] Friend joined: {display}.");
            if (JoinToasts)
                API.HeavenlyAPI.Toast($"[Heavenly] {display} joined.");
        }
        catch { }
    }

    private static void OnLeft(Player player)
    {
        try
        {
            string? id = null;
            string? name = null;
            try { id = player?.UserID; } catch { }
            try { name = player?.Username; } catch { }

            string leaveId = id ?? "";
            string leaveName = name ?? "";

            // Leave payloads are often nulled: fall back to the cached join name.
            if (leaveName.Length == 0 && leaveId.Length > 0)
            {
                lock (KnownNames)
                {
                    if (KnownNames.TryGetValue(leaveId, out string? cached))
                        leaveName = cached ?? "";
                }
            }

            string display = leaveName.Length > 0 ? leaveName : (leaveId.Length > 0 ? leaveId : "???");
            LeaveCount++;
            Push($"{display} left (total leaves: {LeaveCount})");
            API.HeavenlyAPI.Log($"[Leave] {display}");
            if (JoinToasts)
                API.HeavenlyAPI.Toast($"[Heavenly] {display} left.");

            if (leaveId.Length > 0)
            {
                lock (KnownNames) { KnownNames.Remove(leaveId); }
            }
        }
        catch { }
    }

    private static void Push(string line)
    {
        try
        {
            lock (Feed)
            {
                Feed.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
                while (Feed.Count > MaxFeed())
                    Feed.RemoveAt(0);
            }
            _feedLabelDirty = true;
        }
        catch { }
    }

    private static void FlushFeedLabel()
    {
        try
        {
            if (_feedLabel == null)
                return;

            List<string> copy;
            lock (Feed) { copy = new List<string>(Feed); }

            int start = Math.Max(0, copy.Count - 6);
            _feedLabel.SetText(string.Join("\n", copy.GetRange(start, copy.Count - start)));
        }
        catch { }
    }

    public static void LogStatus()
    {
        try
        {
            int unique = 0;
            try { lock (UniqueSeen) { unique = UniqueSeen.Count; } } catch { }
            API.HeavenlyAPI.Log($"Debug: joins={JoinCount} leaves={LeaveCount} unique={unique} players={SafePlayerCount()} fps={Fps} ping={Ping()}ms uptime={Uptime()} clock={DateTime.Now:HH:mm}");
            API.HeavenlyAPI.Log($"Self: {SelfName()} [{SelfId()}]");
            API.HeavenlyAPI.Log(World.WorldService.StatusLine());

            List<string> copy;
            lock (Feed) { copy = new List<string>(Feed); }
            int start = Math.Max(0, copy.Count - 8);
            for (int i = start; i < copy.Count; i++)
                API.HeavenlyAPI.Log(copy[i]);

            API.HeavenlyAPI.Toast($"[Heavenly] {Fps}fps {Ping()}ms | Joins: {JoinCount} Leaves: {LeaveCount}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Debug status failed: {ex.Message}");
        }
    }

    public static void CopyStatus()
    {
        try
        {
            string text =
                $"HeavenlyCVR v{API.HeavenlyAPI.Version} | {DateTime.Now:yyyy-MM-dd HH:mm} | " +
                $"joins={JoinCount} leaves={LeaveCount} players={SafePlayerCount()} " +
                $"fps={Fps} ping={Ping()}ms uptime={Uptime()} | " +
                $"{World.WorldService.StatusLine()} | Self: {SelfName()} [{SelfId()}]";

            try
            {
                UnityEngine.GUIUtility.systemCopyBuffer = text;
                API.HeavenlyAPI.Toast("[Heavenly] Status copied to clipboard.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"Clipboard failed: {ex.Message}");
            }

            API.HeavenlyAPI.Log(text);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Copy status failed: {ex.Message}");
        }
    }

    public static void ReloadMenus()
    {
        try
        {
            ABI_RC.Core.RootLogic.OnMenusReloadButton?.Invoke();
            API.HeavenlyAPI.Toast("[Heavenly] Reloading menus...");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Menu reload failed: {ex.Message}");
        }
    }

    public static void QuitGame()
    {
        try
        {
            API.HeavenlyAPI.Log("Quitting game by user request.");
            ABI_RC.Core.RootLogic.Instance?.QuitApplication();
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Quit failed: {ex.Message}");
        }
    }

    public static void LogRoster()
    {
        try
        {
            var all = PlayerAPI.AllPlayersInternal;
            if (all == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Player list not ready.");
                return;
            }

            API.HeavenlyAPI.Log($"--- Roster ({all.Count}) ---");
            foreach (Player player in all)
            {
                if (player == null)
                    continue;
                string name = "???", id = "???", extra = "";
                try { name = player.Username ?? "???"; } catch { }
                try { id = player.UserID ?? "???"; } catch { }
                try { extra = player.IsLocal ? " (you)" : ""; } catch { }
                API.HeavenlyAPI.Log($"{name}{extra} [{id}] rank={RankOf(id)} friend={(IsFriend(id) ? "yes" : "no")}");
            }

            API.HeavenlyAPI.Toast($"[Heavenly] Roster: {all.Count} player(s). See console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Roster failed: {ex.Message}");
        }
    }

    private static string SelfId()
    {
        try { return ABI_RC.Core.Player.PlayerSetup.PlayerLocalId ?? "?"; } catch { return "?"; }
    }

    private static string SelfName()
    {
        try
        {
            var setup = ABI_RC.Core.Player.PlayerSetup.Instance;
            if (setup != null)
                return setup.PlayerUsername ?? "?";
        }
        catch { }
        return "?";
    }

    private static bool IsFriend(string userId)
    {
        try
        {
            if (!string.IsNullOrEmpty(userId))
                return ABI_RC.Core.Networking.IO.Social.Friends.FriendsWith(userId);
        }
        catch { }
        return false;
    }

    private static string RankOf(string userId)
    {
        try
        {
            var manager = ABI_RC.Core.Player.CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (ABI_RC.Core.Player.CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity != null && entity.Uuid == userId)
                        return entity.ApiUserRank ?? "?";
                }
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(userId) && userId == ABI_RC.Core.Player.PlayerSetup.PlayerLocalId)
                return "local";
        }
        catch { }

        return "?";
    }

    private static int SafePlayerCount()
    {
        try { return PlayerAPI.AllPlayersInternal?.Count ?? -1; } catch { return -1; }
    }
}
