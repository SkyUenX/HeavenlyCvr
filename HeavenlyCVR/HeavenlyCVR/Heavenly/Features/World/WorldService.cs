using System;
using System.Collections.Generic;
using ABI_RC.API;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Systems.UI.UILib;
using Newtonsoft.Json;

namespace HeavenlyCVR.Heavenly.Features.World;

public sealed class WorldHistoryEntry
{
    public string InstanceId { get; set; } = "";
    public string WorldId { get; set; } = "";
    public string WorldName { get; set; } = "";
    public string At { get; set; } = "";

    public WorldHistoryEntry() { }
}

public static class WorldService
{
    private const int MaxHistory = 10;
    private static readonly List<WorldHistoryEntry> History = new();
    public static string CurrentInstanceId { get; private set; } = "NULL";
    public static string LastInstanceId { get; private set; } = "NULL";
    public static string CurrentWorldId { get; private set; } = "NULL";

    private static bool _initialized;
    private static string _lastPolledId = "NULL";

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            RefreshIds();
            _lastPolledId = CurrentInstanceId;

            try
            {
                InstancesAPI.OnInstanceConnected += OnConnected;
                InstancesAPI.OnInstanceDisconnected += OnDisconnected;
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"WorldService: could not subscribe to instance events: {ex.Message}");
            }

            LoadHistory();
            RecordHistory();

            API.HeavenlyAPI.Log($"WorldService initialized. Instance: {CurrentInstanceId}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"WorldService init failed: {ex.Message}");
        }
    }

    private static void OnConnected()
    {
        try
        {
            string fresh = SafeCurrentInstanceId();
            if (!string.IsNullOrEmpty(fresh) && fresh != "NULL" && fresh != CurrentInstanceId)
            {
                LastInstanceId = CurrentInstanceId;
                CurrentInstanceId = fresh;
                CurrentWorldId = SafeCurrentWorldId();
                RecordHistory();
                API.HeavenlyAPI.Log($"Instance connected: {CurrentInstanceId} (last: {LastInstanceId})");
            }
        }
        catch { }
    }

    private static void OnDisconnected()
    {
        try { API.HeavenlyAPI.Log("Instance disconnected."); } catch { }
    }

    /// <summary>Poll for instance changes (called from plugin OnUpdate).</summary>
    public static void OnUpdate()
    {
        try
        {
            string fresh = SafeCurrentInstanceId();
            if (!string.IsNullOrEmpty(fresh) && fresh != "NULL" && fresh != _lastPolledId)
            {
                if (_lastPolledId != "NULL" && _lastPolledId != CurrentInstanceId)
                    LastInstanceId = CurrentInstanceId != "NULL" ? CurrentInstanceId : _lastPolledId;
                else if (CurrentInstanceId != "NULL" && CurrentInstanceId != fresh)
                    LastInstanceId = CurrentInstanceId;

                CurrentInstanceId = fresh;
                CurrentWorldId = SafeCurrentWorldId();
                _lastPolledId = fresh;
                RecordHistory();
                API.HeavenlyAPI.Log($"Instance changed: {CurrentInstanceId} (last: {LastInstanceId})");
            }
        }
        catch { }
    }

    public static void RejoinCurrent()
    {
        try
        {
            RefreshIds();
            if (string.IsNullOrEmpty(CurrentInstanceId) || CurrentInstanceId == "NULL")
            {
                API.HeavenlyAPI.Toast("[Heavenly] No current instance to rejoin.");
                return;
            }

            JoinInstance(CurrentInstanceId);
            API.HeavenlyAPI.Toast("[Heavenly] Rejoining current instance...");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Rejoin current failed: {ex.Message}");
        }
    }

    public static void RejoinLast()
    {
        try
        {
            RefreshIds();
            if (string.IsNullOrEmpty(LastInstanceId) || LastInstanceId == "NULL")
            {
                API.HeavenlyAPI.Toast("[Heavenly] No last instance recorded yet.");
                return;
            }

            JoinInstance(LastInstanceId);
            API.HeavenlyAPI.Toast("[Heavenly] Rejoining last instance...");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Rejoin last failed: {ex.Message}");
        }
    }

    public static void ReloadWorld() => RejoinCurrent();

    public static void SearchWorldsKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                query =>
                {
                    query = (query ?? "").Trim();
                    if (query.Length == 0)
                        return;

                    try
                    {
                        ViewManager? vm = ViewManager.Instance;
                        if (vm == null)
                        {
                            API.HeavenlyAPI.Toast("[Heavenly] ViewManager not ready.");
                            return;
                        }

                        vm.GetFilteredWorldsPaged(query, false, 0);
                        API.HeavenlyAPI.Toast($"[Heavenly] Searching worlds for '{query}' (see world list).");
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Error($"World search failed: {ex.Message}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"World search keyboard failed: {ex.Message}");
        }
    }

    public static void OpenInstanceDetails()
    {
        try
        {
            ViewManager? vm = null;
            try { vm = ViewManager.Instance; } catch { }
            if (vm == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] ViewManager not ready.");
                return;
            }

            vm.OpenCurrentInstanceDetails();
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Instance details failed: {ex.Message}");
        }
    }

    public static string WorldName()
    {
        try
        {
            var meta = ABI_RC.Core.Savior.MetaPort.Instance;
            if (meta != null && !string.IsNullOrEmpty(meta.CurrentWorldName))
                return meta.CurrentWorldName;
        }
        catch { }
        return "?";
    }

    public static void LogWorldInfo()
    {
        try
        {
            var world = ABI.CCK.Components.CVRWorld.Instance;
            if (world == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No world loaded yet.");
                return;
            }

            float gravity = 0f, jump = 0f, fly = 0f, respawn = 0f;
            bool portals = false, spawnables = false, flying = false;
            int spawns = 0;
            try { gravity = world.gravity; } catch { }
            try { jump = world.jumpHeight; } catch { }
            try { fly = world.flyMultiplier; } catch { }
            try { respawn = world.GetRespawnHeight(); } catch { }
            try { portals = world.allowPortals; } catch { }
            try { spawnables = world.allowSpawnables; } catch { }
            try { flying = world.allowFlying; } catch { }
            try { spawns = world.spawns != null ? world.spawns.Length : 0; } catch { }

            API.HeavenlyAPI.Log(
                $"--- World info: {WorldName()} ---\n" +
                $"Gravity: {gravity:0.00} | Jump: {jump:0.00} | FlyMult: {fly:0.00} | " +
                $"Portals: {(portals ? "yes" : "no")} | Spawnables: {(spawnables ? "yes" : "no")} | " +
                $"RespawnY: {respawn:0.0} | " +
                $"Flying: {(flying ? "yes" : "no")} | Spawns: {spawns}"
            );
            API.HeavenlyAPI.Toast("[Heavenly] World info logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"World info failed: {ex.Message}");
        }
    }

    public static void JoinInstanceIdKeyboard()
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
                    JoinInstance(id);
                    API.HeavenlyAPI.Toast("[Heavenly] Joining instance...");
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Join keyboard failed: {ex.Message}");
        }
    }

    public static void SetHomeHere()
    {
        try
        {
            RefreshIds();
            if (string.IsNullOrEmpty(CurrentWorldId) || CurrentWorldId == "NULL")
            {
                API.HeavenlyAPI.Toast("[Heavenly] Current world unknown.");
                return;
            }

            ViewManager? vm = null;
            try { vm = ViewManager.Instance; } catch { }
            if (vm == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] ViewManager not ready.");
                return;
            }

            vm.SetHomeWorld(CurrentWorldId);
            API.HeavenlyAPI.Toast("[Heavenly] Home world set to this world.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Set home failed: {ex.Message}");
        }
    }

    public static void LogHistory()
    {
        try
        {
            if (History.Count == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No instance history yet.");
                return;
            }

            API.HeavenlyAPI.Log($"--- Instance history ({History.Count}) ---");
            for (int i = History.Count - 1; i >= 0; i--)
            {
                int n = History.Count - i;
                WorldHistoryEntry e = History[i];
                API.HeavenlyAPI.Log($"{n}. {(e.WorldName.Length > 0 ? e.WorldName : e.WorldId)} [{e.InstanceId}] @ {e.At}");
            }

            API.HeavenlyAPI.Toast("[Heavenly] History logged. Rejoin #N to go back.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"History log failed: {ex.Message}");
        }
    }

    public static void RejoinIndexKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    if (!int.TryParse((text ?? "").Trim(), out int n))
                        return;
                    RejoinIndex(n);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"History keyboard failed: {ex.Message}");
        }
    }

    public static void RejoinIndex(int n)
    {
        try
        {
            int idx = History.Count - n;
            if (n < 1 || idx < 0 || idx >= History.Count)
            {
                API.HeavenlyAPI.Toast("[Heavenly] History number out of range.");
                return;
            }

            JoinInstance(History[idx].InstanceId);
            API.HeavenlyAPI.Toast($"[Heavenly] Rejoining history #{n}...");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Rejoin history failed: {ex.Message}");
        }
    }

    private static void RecordHistory()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentInstanceId) || CurrentInstanceId == "NULL")
                return;

            if (History.Count > 0 && History[History.Count - 1].InstanceId == CurrentInstanceId)
                return;

            string worldName = "";
            try
            {
                var meta = ABI_RC.Core.Savior.MetaPort.Instance;
                if (meta != null)
                    worldName = meta.CurrentWorldName ?? "";
            }
            catch { }

            History.Add(new WorldHistoryEntry
            {
                InstanceId = CurrentInstanceId,
                WorldId = CurrentWorldId,
                WorldName = worldName,
                At = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            while (History.Count > MaxHistory)
                History.RemoveAt(0);

            SaveHistory();
        }
        catch { }
    }

    private static void LoadHistory()
    {
        try
        {
            List<WorldHistoryEntry>? loaded =
                JsonConvert.DeserializeObject<List<WorldHistoryEntry>>(Config.HeavenlyConfig.WorldHistoryJson);
            if (loaded == null)
                return;

            History.Clear();
            foreach (WorldHistoryEntry e in loaded)
            {
                if (e == null || string.IsNullOrEmpty(e.InstanceId))
                    continue;
                e.WorldId ??= "";
                e.WorldName ??= "";
                e.At ??= "";
                History.Add(e);
            }

            while (History.Count > MaxHistory)
                History.RemoveAt(0);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Instance history load failed: {ex.Message}");
        }
    }

    private static void SaveHistory()
    {
        try
        {
            Config.HeavenlyConfig.WorldHistoryJson = JsonConvert.SerializeObject(History);
        }
        catch { }
    }

    public static void GoHome()
    {
        try
        {
            ViewManager? vm = null;
            try { vm = ViewManager.Instance; } catch { }
            if (vm == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] ViewManager not ready.");
                return;
            }

            vm.GoHome();
            API.HeavenlyAPI.Toast("[Heavenly] Going home...");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Go home failed: {ex.Message}");
        }
    }

    public static void CopyInstanceId()
    {
        try
        {
            RefreshIds();
            if (string.IsNullOrEmpty(CurrentInstanceId) || CurrentInstanceId == "NULL")
            {
                API.HeavenlyAPI.Toast("[Heavenly] No instance ID yet.");
                return;
            }

            try
            {
                ViewManager? vm = ViewManager.Instance;
                vm?.CopyInstanceId(CurrentInstanceId);
            }
            catch { }

            API.HeavenlyAPI.Toast($"[Heavenly] Instance: {CurrentInstanceId}");
            API.HeavenlyAPI.Log($"Instance ID: {CurrentInstanceId} | World: {CurrentWorldId}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Copy instance ID failed: {ex.Message}");
        }
    }

    public static void CopyWorldId()
    {
        try
        {
            RefreshIds();
            if (string.IsNullOrEmpty(CurrentWorldId) || CurrentWorldId == "NULL")
            {
                API.HeavenlyAPI.Toast("[Heavenly] No world ID yet.");
                return;
            }

            API.HeavenlyAPI.Log($"World ID: {CurrentWorldId}");
            API.HeavenlyAPI.Toast("[Heavenly] World ID logged to console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Copy world ID failed: {ex.Message}");
        }
    }

    public static string StatusLine()
    {
        try
        {
            RefreshIds();
            return $"World: {WorldName()} [{CurrentWorldId}] | Instance: {CurrentInstanceId} ({Privacy()}) | Last: {LastInstanceId}";
        }
        catch
        {
            return "World status unavailable.";
        }
    }

    private static string Privacy()
    {
        try
        {
            return Instances.CurrentInstancePrivacyType.ToString();
        }
        catch
        {
            return "?";
        }
    }

    private static void JoinInstance(string instanceId)
    {
        // Exceptions propagate to the caller, which logs once.
        Instances.TryJoinInstance(
            instanceId,
            Instances.JoinInstanceSource.Mod,
            false,
            null
        );
    }

    private static void RefreshIds()
    {
        try
        {
            string freshInstance = SafeCurrentInstanceId();
            if (!string.IsNullOrEmpty(freshInstance) && freshInstance != "NULL")
            {
                if (CurrentInstanceId != "NULL" && CurrentInstanceId != freshInstance)
                    LastInstanceId = CurrentInstanceId;
                CurrentInstanceId = freshInstance;
            }

            string freshWorld = SafeCurrentWorldId();
            if (!string.IsNullOrEmpty(freshWorld))
                CurrentWorldId = freshWorld;
        }
        catch { }
    }

    private static string SafeCurrentInstanceId()
    {
        try { return Instances.CurrentInstanceId ?? "NULL"; } catch { return "NULL"; }
    }

    private static string SafeCurrentWorldId()
    {
        try { return Instances.CurrentWorldId ?? "NULL"; } catch { return "NULL"; }
    }
}
