using System;
using ABI_RC.API;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using ABI_RC.Systems.UI.UILib;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Social;

public static class SocialService
{
    public static string? SelectedUserId()
    {
        return PlayerTarget.CurrentId();
    }

    public static string? SelectedUserName()
    {
        return PlayerTarget.CurrentName();
    }

    private static bool RequireSelected(out string userId, out string userName)
    {
        userId = SelectedUserId() ?? "";
        userName = SelectedUserName() ?? "";

        if (userId.Length == 0)
        {
            API.HeavenlyAPI.Toast("[Heavenly] No target: use Choose Player or select one in the QuickMenu.");
            return false;
        }

        if (string.IsNullOrEmpty(userName))
            userName = userId;

        return true;
    }

    public static void TeleportToSelectedPlayer()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            Vector3? target = SelectedPlayerPosition(userId);
            if (target == null)
            {
                API.HeavenlyAPI.Toast($"[Heavenly] Could not locate {userName}.");
                return;
            }

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            Vector3 dest = target.Value + new Vector3(TeleportOffsetRight(), TeleportOffsetUp(), 0f);
            try
            {
                controller.TeleportPlayerTo(dest, false, true, false, null);
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Teleport failed: {ex.Message}");
                return;
            }

            API.HeavenlyAPI.Toast($"[Heavenly] Teleported to {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Teleport to player failed: {ex.Message}");
        }
    }

    public static void InviteSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            ViewManager.Instance?.InvitePlayer(userId);
            API.HeavenlyAPI.Toast($"[Heavenly] Invited {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Invite failed: {ex.Message}");
        }
    }

    public static void RequestInviteSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            ViewManager.Instance?.RequestInvite(userId);
            API.HeavenlyAPI.Toast($"[Heavenly] Invite requested from {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Request invite failed: {ex.Message}");
        }
    }

    public static void SendFriendRequestSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            ViewManager.Instance?.SendFriendRequest(userId);
            API.HeavenlyAPI.Toast($"[Heavenly] Friend request sent to {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Friend request failed: {ex.Message}");
        }
    }

    public static void BlockSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            ViewManager.Instance?.BlockUser(userId, true);
            API.HeavenlyAPI.Toast($"[Heavenly] Blocked {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Block failed: {ex.Message}");
        }
    }

    public static void UnblockSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            ViewManager.Instance?.UnblockUser(userId);
            API.HeavenlyAPI.Toast($"[Heavenly] Unblocked {userName}.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Unblock failed: {ex.Message}");
        }
    }

    public static string? SelectedAvatarId()
    {
        try
        {
            if (!RequireSelected(out string userId, out _))
                return null;

            try
            {
                var selected = QuickMenuAPI.SelectedPlayerEntity;
                if (selected != null && selected.Uuid == userId && !string.IsNullOrEmpty(selected.AvatarID))
                    return selected.AvatarID;
            }
            catch { }

            try
            {
                foreach (Player remote in PlayerAPI.RemotePlayersInternal)
                {
                    if (remote == null || remote.UserID != userId)
                        continue;
                    try
                    {
                        string? id = remote.Avatar?.AvatarID;
                        if (!string.IsNullOrEmpty(id))
                            return id;
                    }
                    catch { }
                }
            }
            catch { }
        }
        catch { }

        return null;
    }

    public static void LogSelectedAvatarId()
    {
        try
        {
            string? avatarId = SelectedAvatarId();
            if (string.IsNullOrEmpty(avatarId))
            {
                API.HeavenlyAPI.Toast("[Heavenly] Target avatar ID unknown.");
                return;
            }

            API.HeavenlyAPI.Log($"Target avatar ID: {avatarId}");
            API.HeavenlyAPI.Toast("[Heavenly] Avatar ID logged to console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar ID failed: {ex.Message}");
        }
    }

    public static void OpenSelectedAvatarDetails()
    {
        try
        {
            string? avatarId = SelectedAvatarId();
            if (string.IsNullOrEmpty(avatarId))
            {
                API.HeavenlyAPI.Toast("[Heavenly] Target avatar ID unknown.");
                return;
            }

            try
            {
                ViewManager.Instance?.RequestAvatarDetailsPage(avatarId);
                API.HeavenlyAPI.Toast("[Heavenly] Opened target avatar details.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Avatar details failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar details failed: {ex.Message}");
        }
    }

    public static void LogSelectedUserId()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            API.HeavenlyAPI.Log($"Target user: {userName} [{userId}]");
            API.HeavenlyAPI.Toast("[Heavenly] User ID logged to console.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"User ID failed: {ex.Message}");
        }
    }

    public static void ChatBoxKeyboard()
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    text = (text ?? "").Trim();
                    if (text.Length == 0)
                        return;
                    SendChatBox(text);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Chatbox keyboard failed: {ex.Message}");
        }
    }

    public static void SendChatBox(string text)
    {
        try
        {
            ABI_RC.Systems.ChatBox.ChatBoxAPI.SendMessage(text, true, true, true);
            API.HeavenlyAPI.Log($"Chatbox sent: {text}");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Chatbox send failed: {ex.Message}");
        }
    }

    private static ABI_RC.Core.Savior.CVRSelfModerationManager? Moderation()
    {
        try { return ABI_RC.Core.Savior.MetaPort.Instance?.SelfModerationManager; }
        catch { return null; }
    }

    public static void SetTargetMuted(bool muted)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                mod.SetPlayerMute(userId, muted);
                API.HeavenlyAPI.Toast(muted
                    ? $"[Heavenly] Muted {userName} (saved)."
                    : $"[Heavenly] Unmuted {userName}.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Mute failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Mute failed: {ex.Message}");
        }
    }

    public static void SetTargetVolume(float volume)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Warning("Moderation manager not ready.");
                return;
            }

            if (volume < 0f) volume = 0f;
            if (volume > 2f) volume = 2f;

            try
            {
                mod.SetPlayerVolume(userId, volume);
                API.HeavenlyAPI.Log($"Target volume for {userName} -> {volume:0.00} (saved).");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Target volume failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Target volume failed: {ex.Message}");
        }
    }

    public static void SetTargetAvatarHidden(bool hidden)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                mod.SetPlayerAvatarVisibility(userId, !hidden);
                API.HeavenlyAPI.Toast(hidden
                    ? $"[Heavenly] Hid {userName}'s avatar (saved)."
                    : $"[Heavenly] Showing {userName}'s avatar.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Avatar hide failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Avatar hide failed: {ex.Message}");
        }
    }

    public static void FaceTarget()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            Vector3? target = SelectedPlayerPosition(userId);
            if (target == null)
            {
                API.HeavenlyAPI.Toast($"[Heavenly] Could not locate {userName}.");
                return;
            }

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            Vector3 self;
            try { self = controller.transform.position; }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Face failed: {ex.Message}");
                return;
            }

            Vector3 flat = target.Value - self;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Already on top of target.");
                return;
            }

            try
            {
                Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
                controller.TeleportPlayerTo(self, false, true, false, look);
                API.HeavenlyAPI.Toast($"[Heavenly] Facing {userName}.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Face failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Face target failed: {ex.Message}");
        }
    }

    public static void LogTargetInfo()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            API.HeavenlyAPI.Log(TargetInfoText(userId, userName));
            API.HeavenlyAPI.Toast("[Heavenly] Target info logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Target info failed: {ex.Message}");
        }
    }

    private static string TargetInfoText(string userId, string userName)
    {
        try
        {
            Vector3? pos = SelectedPlayerPosition(userId);
            string? avatarId = SelectedAvatarId();

            string dist = "?";
            try
            {
                var controller = BetterBetterCharacterController.Instance;
                if (controller != null && pos != null)
                    dist = Vector3.Distance(controller.transform.position, pos.Value).ToString("0.0") + "m";
            }
            catch { }

            string rank = "?";
            bool friend = false;
            try
            {
                var manager = CVRPlayerManager.Instance;
                if (manager?.NetworkPlayers != null)
                {
                    foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                    {
                        if (entity != null && entity.Uuid == userId)
                        {
                            try { rank = entity.ApiUserRank ?? "?"; } catch { }
                            break;
                        }
                    }
                }
            }
            catch { }
            try { friend = ABI_RC.Core.Networking.IO.Social.Friends.FriendsWith(userId); } catch { }

            string? note = null;
            try { note = PlayerNotes.GetNote(userId); } catch { }

            return
                $"--- Target: {userName} [{userId}] ---\n" +
                $"Rank: {rank} | Friend: {(friend ? "yes" : "no")} | Distance: {dist}\n" +
                $"Avatar: {avatarId ?? "(unknown)"}\n" +
                $"Note: {note ?? "(none)"}";
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Target info failed: {ex.Message}");
            return "";
        }
    }

    private static readonly System.Collections.Generic.List<ABI_RC.Core.Networking.IO.Social.FriendRequest_t> CachedRequests = new();

    public static void UnfriendSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            try
            {
                ViewManager.Instance?.Unfriend(userId);
                API.HeavenlyAPI.Toast($"[Heavenly] Unfriended {userName}.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Unfriend failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Unfriend failed: {ex.Message}");
        }
    }

    public static void LogFriendRequests()
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

            API.HeavenlyAPI.Toast("[Heavenly] Fetching friend requests...");

            System.Threading.Tasks.Task<System.Collections.Generic.List<ABI_RC.Core.Networking.IO.Social.FriendRequest_t>> task;
            try
            {
                task = vm.GetFriendRequestsTask();
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Friend requests failed: {ex.Message}");
                return;
            }

            // Never block the game thread: handle completion in the background
            // and only touch the console from there (no Unity calls off-thread).
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted || t.Result == null)
                    {
                        API.HeavenlyAPI.Warning("Friend requests fetch failed.");
                        return;
                    }

                    lock (CachedRequests)
                    {
                        CachedRequests.Clear();
                        foreach (var req in t.Result)
                        {
                            if (req != null && !string.IsNullOrEmpty(req.UserId))
                                CachedRequests.Add(req);
                        }
                    }

                    if (CachedRequests.Count == 0)
                    {
                        API.HeavenlyAPI.Log("No pending friend requests.");
                        return;
                    }

                    API.HeavenlyAPI.Log($"--- Friend requests ({CachedRequests.Count}) ---");
                    for (int i = CachedRequests.Count - 1; i >= 0; i--)
                    {
                        int n = CachedRequests.Count - i;
                        API.HeavenlyAPI.Log($"{n}. {CachedRequests[i].UserName} [{CachedRequests[i].UserId}]");
                    }
                    API.HeavenlyAPI.Log("Accept/Deny #N from the Social page.");
                }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"Friend requests failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Friend requests failed: {ex.Message}");
        }
    }

    public static void AcceptRequestIndexKeyboard()
    {
        RequestIndexKeyboard(true);
    }

    public static void DenyRequestIndexKeyboard()
    {
        RequestIndexKeyboard(false);
    }

    private static void RequestIndexKeyboard(bool accept)
    {
        try
        {
            QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    try
                    {
                        if (!int.TryParse((text ?? "").Trim(), out int n))
                            return;

                        ABI_RC.Core.Networking.IO.Social.FriendRequest_t? req = null;
                        lock (CachedRequests)
                        {
                            int idx = CachedRequests.Count - n;
                            if (n >= 1 && idx >= 0 && idx < CachedRequests.Count)
                                req = CachedRequests[idx];
                        }

                        if (req == null)
                        {
                            API.HeavenlyAPI.Toast("[Heavenly] Number out of range (log requests first).");
                            return;
                        }

                        try
                        {
                            if (accept)
                                ViewManager.Instance?.AcceptFriendRequest(req.UserId);
                            else
                                ViewManager.Instance?.DenyFriendRequest(req.UserId);
                            API.HeavenlyAPI.Toast($"[Heavenly] {(accept ? "Accepted" : "Denied")} {req.UserName}.");
                        }
                        catch (Exception ex)
                        {
                            API.HeavenlyAPI.Error($"Friend request update failed: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        API.HeavenlyAPI.Error($"Friend request update failed: {ex.Message}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Request keyboard failed: {ex.Message}");
        }
    }

    public static void CopyTargetInfo()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            string text = TargetInfoText(userId, userName);
            if (string.IsNullOrEmpty(text))
            {
                API.HeavenlyAPI.Toast("[Heavenly] Nothing to copy.");
                return;
            }

            try
            {
                UnityEngine.GUIUtility.systemCopyBuffer = text;
                API.HeavenlyAPI.Toast("[Heavenly] Target info copied.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"Clipboard failed: {ex.Message}");
            }

            API.HeavenlyAPI.Log(text);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Copy target info failed: {ex.Message}");
        }
    }

    public static void SetTargetPropsHidden(bool hidden)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                mod.SetPlayerPropVisibility(userId, !hidden);
                API.HeavenlyAPI.Toast(hidden
                    ? $"[Heavenly] Hid {userName}'s props (saved)."
                    : $"[Heavenly] Showing {userName}'s props.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Prop visibility failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Prop visibility failed: {ex.Message}");
        }
    }

    public static void SetTargetNormalized(bool normalized)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                mod.SetPlayerNormalization(userId, normalized);
                API.HeavenlyAPI.Toast(normalized
                    ? $"[Heavenly] Normalized {userName}'s voice (saved)."
                    : $"[Heavenly] Unnormalized {userName}'s voice.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Normalization failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Normalization failed: {ex.Message}");
        }
    }

    public static void SetTargetChatBoxHidden(bool hidden)
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;

            var mod = Moderation();
            if (mod == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Moderation manager not ready.");
                return;
            }

            try
            {
                // None = hide everything; UseGlobalSettings = restore default.
                mod.SetChatBoxVisibility(userId, hidden ? 4 : 1);
                API.HeavenlyAPI.Toast(hidden
                    ? $"[Heavenly] Hid {userName}'s chatbox (saved)."
                    : $"[Heavenly] Showing {userName}'s chatbox.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Chatbox visibility failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Chatbox visibility failed: {ex.Message}");
        }
    }

    public static void WearTargetAvatar()
    {
        try
        {
            string targetAvatar = SelectedAvatarId() ?? "";
            if (targetAvatar.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Target avatar ID unknown.");
                return;
            }

            // CVR enforces avatar permissions server-side: public avatars
            // switch, private ones are rejected (reported back via toast).
            Avatar.AvatarService.SwitchTo(targetAvatar);
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Wear avatar failed: {ex.Message}");
        }
    }

    public static void HideAvatarSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            if (SetAvatarVisibility(userId, false))
                API.HeavenlyAPI.Toast($"[Heavenly] Hid {userName}'s avatar.");
            else
                API.HeavenlyAPI.Toast($"[Heavenly] Could not hide {userName}'s avatar.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Hide avatar failed: {ex.Message}");
        }
    }

    public static void ShowAvatarSelected()
    {
        try
        {
            if (!RequireSelected(out string userId, out string userName))
                return;
            if (SetAvatarVisibility(userId, true))
                API.HeavenlyAPI.Toast($"[Heavenly] Showed {userName}'s avatar.");
            else
                API.HeavenlyAPI.Toast($"[Heavenly] Could not show {userName}'s avatar.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Show avatar failed: {ex.Message}");
        }
    }

    private static bool SetAvatarVisibility(string userId, bool visible)
    {
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity == null || entity.Uuid != userId)
                        continue;
                    try
                    {
                        entity.PuppetMaster?.SetAvatarVisibility(visible);
                        return true;
                    }
                    catch { return false; }
                }
            }
        }
        catch { }
        return false;
    }

    private static float TeleportOffsetRight()
    {
        try
        {
            float v = Config.HeavenlyConfig.TeleportRight;
            if (v < 0f) return 0f;
            if (v > 10f) return 10f;
            return v;
        }
        catch
        {
            return 1.2f;
        }
    }

    private static float TeleportOffsetUp()
    {
        try
        {
            float v = Config.HeavenlyConfig.TeleportUp;
            if (v < 0f) return 0f;
            if (v > 10f) return 10f;
            return v;
        }
        catch
        {
            return 0.3f;
        }
    }

    private static Vector3? SelectedPlayerPosition(string userId)
    {
        // 1. QuickMenu selected object has the live GameObject.
        try
        {
            var selected = QuickMenuAPI.SelectedPlayerEntity;
            if (selected != null && selected.Uuid == userId)
            {
                GameObject? go = selected.PlayerGameObject;
                if (go != null)
                    return go.transform.position;
            }
        }
        catch { }

        // 2. Network player list.
        try
        {
            var manager = CVRPlayerManager.Instance;
            if (manager?.NetworkPlayers != null)
            {
                foreach (CVRPlayerEntity entity in manager.NetworkPlayers)
                {
                    if (entity == null || entity.Uuid != userId)
                        continue;
                    try
                    {
                        if (entity.PlayerObject != null)
                            return entity.PlayerObject.transform.position;
                    }
                    catch { }
                }
            }
        }
        catch { }

        // 3. PlayerAPI remote list.
        try
        {
            foreach (Player remote in PlayerAPI.RemotePlayersInternal)
            {
                if (remote == null || remote.UserID != userId)
                    continue;
                try { return remote.GetPosition(); } catch { }
            }
        }
        catch { }

        return null;
    }
}
