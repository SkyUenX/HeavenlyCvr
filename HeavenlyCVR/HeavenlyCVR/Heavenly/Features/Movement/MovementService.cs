using System;
using ABI_RC.API;
using ABI_RC.Core.Player;
using ABI_RC.Systems.Movement;
using ABI_RC.Systems.UI.UILib;
using UnityEngine;

namespace HeavenlyCVR.Heavenly.Features.Movement;

public static class MovementService
{
    public static bool IsFlying { get; private set; }

    public static bool IsNoClip { get; private set; }

    public static float FlySpeedMultiplier { get; private set; } = 3f;

    public static bool SittingOnPlayer { get; private set; }

    public static bool IsCrouched { get; private set; }
    public static bool IsProne { get; private set; }

    public static HumanBodyBones SitBone
    {
        get
        {
            try { return (HumanBodyBones)Config.HeavenlyConfig.SitBone; }
            catch { return HumanBodyBones.Head; }
        }
        set
        {
            try { Config.HeavenlyConfig.SitBone = (int)value; } catch { }
        }
    }

    private static string? _sitTargetUserId;
    private static float _originalFlightMultiplier = -1f;
    private static int _sitMissFrames;
    private static int _reconcileFrames;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        try
        {
            FlySpeedMultiplier = Config.HeavenlyConfig.FlySpeed;
        }
        catch { }

        try
        {
            ABI_RC.Core.IO.CVRObjectLoader.OnWorldLoadedAfterEnable += OnWorldLoaded;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Movement: world-load hook failed: {ex.Message}");
        }
    }

    private static void OnWorldLoaded(string _)
    {
        // World transitions drop flight/sit state; reflect that in the model
        // so toggles don't lie.
        IsFlying = false;
        IsNoClip = false;
        IsCrouched = false;
        IsProne = false;
        SittingOnPlayer = false;
        _sitTargetUserId = null;
        _sitMissFrames = 0;

        try { FlashlightService.OnWorldLoaded(); } catch { }

        try
        {
            if (_mirror != null)
                UnityEngine.Object.Destroy(_mirror);
        }
        catch { }
        _mirror = null;
        if (MirrorEnabled)
        {
            MirrorEnabled = false;
            API.HeavenlyAPI.Log("Mirror cleared by world transition (re-enable to restore).");
        }

        // Worlds reset controller tuning; re-apply ours.
        try { ApplySavedLocomotion(); } catch { }
    }

    public static void SetWalkSpeed(float mult)
    {
        mult = ClampMult(mult);
        try { Config.HeavenlyConfig.WalkSpeed = mult; } catch { }
        ApplyLocomotion();
        API.HeavenlyAPI.Log($"Walk speed -> {mult:0.00}x");
    }

    public static void SetSprintMult(float mult)
    {
        mult = ClampMult(mult);
        try { Config.HeavenlyConfig.SprintMult = mult; } catch { }
        ApplyLocomotion();
        API.HeavenlyAPI.Log($"Sprint -> {mult:0.00}x");
    }

    private static float ClampMult(float v)
    {
        if (v < 0.25f) return 0.25f;
        if (v > 3f) return 3f;
        return v;
    }

    private static float _origWalkSpeed = -1f;
    private static float _origSprintMult = -1f;
    private static float _origJumpImpulse = -1f;
    private static int _origJumpCount = -1;

    public static bool BunnyHop { get; private set; }

    public static bool MirrorEnabled { get; private set; }

    private static GameObject? _mirror;

    private static void ApplySavedLocomotion()
    {
        ApplyLocomotion();
    }

    private static void ApplyLocomotion()
    {
        BetterBetterCharacterController? controller = null;
        try { controller = BetterBetterCharacterController.Instance; } catch { }
        if (controller == null)
        {
            try { controller = PlayerSetup.Instance.CharacterController; } catch { }
        }
        if (controller == null)
            return;

        float walk = 1f, sprint = 1f, jump = 1f;
        int count = 1;
        try { walk = Config.HeavenlyConfig.WalkSpeed; } catch { }
        try { sprint = Config.HeavenlyConfig.SprintMult; } catch { }
        try { jump = Config.HeavenlyConfig.JumpMult; } catch { }
        try { count = Config.HeavenlyConfig.JumpCount; } catch { }
        walk = ClampMult(walk);
        sprint = ClampMult(sprint);
        jump = ClampMult(jump);
        if (count < 1) count = 1;
        if (count > 5) count = 5;

        try
        {
            if (_origWalkSpeed < 0f)
                _origWalkSpeed = controller.BaseMovementSpeed;
            controller.BaseMovementSpeed = _origWalkSpeed * walk;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Walk speed failed: {ex.Message}");
        }

        try
        {
            if (_origSprintMult < 0f)
                _origSprintMult = controller.worldSprintMultiplier;
            controller.worldSprintMultiplier = _origSprintMult * sprint;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Sprint failed: {ex.Message}");
        }

        try
        {
            if (_origJumpImpulse < 0f)
                _origJumpImpulse = controller.jumpImpulse;
            controller.jumpImpulse = _origJumpImpulse * jump;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Jump impulse failed: {ex.Message}");
        }

        try
        {
            if (_origJumpCount < 0)
                _origJumpCount = controller.jumpMaxCount;
            controller.jumpMaxCount = count;
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Jump count failed: {ex.Message}");
        }

    }

    public static void SetFlying(bool enabled)
    {
        try
        {
            // Preferred high-level API.
            Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }

            if (local != null)
            {
                try
                {
                    local.SetFlight(enabled, IsNoClip);
                    IsFlying = enabled;
                    API.HeavenlyAPI.Toast(enabled ? "[Heavenly] Fly ON" : "[Heavenly] Fly OFF");
                    if (enabled)
                        ApplyFlySpeed(FlySpeedMultiplier);
                    else
                        RestoreFlySpeed();
                    return;
                }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"Player.SetFlight failed, falling back to controller: {ex.Message}");
                }
            }

            // Fallback: drive the character controller directly (also bypasses world rules).
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                try { controller = PlayerSetup.Instance.CharacterController; } catch { }
            }

            if (controller == null)
            {
                API.HeavenlyAPI.Warning("Fly toggle failed: no local player controller yet (not in world?).");
                return;
            }

            try { controller.FlightAllowedInWorld = true; } catch { }

            try
            {
                var world = ABI.CCK.Components.CVRWorld.Instance;
                if (world != null)
                {
                    try { world.allowFlying = true; } catch { }
                }
            }
            catch { }

            controller.ChangeFlight(enabled, true);
            IsFlying = enabled;

            if (enabled)
                ApplyFlySpeed(FlySpeedMultiplier);
            else
                RestoreFlySpeed();

            if (IsNoClip)
            {
                try
                {
                    if (!controller.IsFlyingNoClipEnabled())
                        controller.ToggleFlightNoClip();
                }
                catch { }
            }

            API.HeavenlyAPI.Toast(enabled ? "[Heavenly] Fly ON" : "[Heavenly] Fly OFF");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"SetFlying failed: {ex}");
        }
    }

    public static void ToggleFlying() => SetFlying(!IsFlying);

    internal static void SyncExternalFly(bool flying)
    {
        IsFlying = flying;
    }

    internal static void SyncExternalNoClip(bool noclip)
    {
        IsNoClip = noclip;
    }

    public static void SetNoClip(bool enabled)
    {
        try
        {
            Player? local = null;
            try { local = PlayerAPI.LocalPlayerInternal; } catch { }

            if (local != null)
            {
                try
                {
                    local.SetFlight(IsFlying, enabled);
                    IsNoClip = enabled;
                    API.HeavenlyAPI.Toast(enabled ? "[Heavenly] NoClip ON" : "[Heavenly] NoClip OFF");
                    return;
                }
                catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"Player.SetFlight (noclip) failed, falling back: {ex.Message}");
                }
            }

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                try { controller = PlayerSetup.Instance.CharacterController; } catch { }
            }

            if (controller == null)
            {
                API.HeavenlyAPI.Warning("NoClip toggle failed: no local player controller yet.");
                return;
            }

            try
            {
                if (controller.IsFlyingNoClipEnabled() != enabled)
                    controller.ToggleFlightNoClip();
                IsNoClip = enabled;
                API.HeavenlyAPI.Toast(enabled ? "[Heavenly] NoClip ON" : "[Heavenly] NoClip OFF");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Warning($"NoClip toggle failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"SetNoClip failed: {ex}");
        }
    }

    public static void ApplyFlySpeed(float multiplier)
    {
        FlySpeedMultiplier = Math.Max(0.1f, multiplier);
        try { Config.HeavenlyConfig.FlySpeed = FlySpeedMultiplier; } catch { }

        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }

            if (controller != null)
            {
                if (_originalFlightMultiplier < 0f)
                {
                    try { _originalFlightMultiplier = controller.worldFlightSpeedMultiplier; } catch { }
                }

                try { controller.worldFlightSpeedMultiplier = FlySpeedMultiplier; } catch (Exception ex)
                {
                    API.HeavenlyAPI.Warning($"Could not set flight speed multiplier: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"ApplyFlySpeed (controller) failed: {ex.Message}");
        }

        try
        {
            var world = ABI.CCK.Components.CVRWorld.Instance;
            if (world != null)
            {
                try { world.SetFlyMultiplier(FlySpeedMultiplier); } catch { }
            }
        }
        catch { }

        API.HeavenlyAPI.Log($"Fly speed multiplier -> {FlySpeedMultiplier}");
    }

    private static void RestoreFlySpeed()
    {
        if (_originalFlightMultiplier < 0f)
            return;

        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller != null)
            {
                try { controller.worldFlightSpeedMultiplier = _originalFlightMultiplier; } catch { }
            }
        }
        catch { }

        API.HeavenlyAPI.Log($"Fly speed multiplier restored -> {_originalFlightMultiplier}");
    }

    public static void Respawn()
    {
        try
        {
            try
            {
                Player? local = PlayerAPI.LocalPlayerInternal;
                if (local != null)
                {
                    local.Respawn();
                    API.HeavenlyAPI.Toast("[Heavenly] Respawned.");
                    return;
                }
            }
            catch { }

            try
            {
                CVR.LocalPlayer.Respawn();
                API.HeavenlyAPI.Toast("[Heavenly] Respawned.");
                return;
            }
            catch { }

            ABI_RC.Core.RootLogic.Instance?.Respawn();
            API.HeavenlyAPI.Toast("[Heavenly] Respawned.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Respawn failed: {ex.Message}");
        }
    }

    public static void TeleportToSpawn()
    {
        try
        {
            var world = ABI.CCK.Components.CVRWorld.Instance;
            if (world?.spawns == null || world.spawns.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No spawn points in this world.");
                return;
            }

            int idx = world.currentSpawnIndex % world.spawns.Length;
            if (idx < 0) idx = 0;
            GameObject? spawn = world.spawns[idx];
            if (spawn == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Spawn point missing.");
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
                    spawn.transform.position + new Vector3(0f, 0.5f, 0f),
                    false,
                    true,
                    false,
                    null
                );
                API.HeavenlyAPI.Toast("[Heavenly] Teleported to spawn.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Spawn teleport failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Spawn teleport failed: {ex.Message}");
        }
    }

    public static void PlayEmoteKeyboard()
    {
        try
        {
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OpenKeyboard(
                "",
                text =>
                {
                    if (!float.TryParse((text ?? "").Trim(), out float emote))
                        return;
                    PlayEmote(emote);
                }
            );
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Emote keyboard failed: {ex.Message}");
        }
    }

    public static void PlayEmote(float emote)
    {
        try
        {
            if (emote < 0f) emote = 0f;
            if (emote > 20f) emote = 20f;

            var setup = PlayerSetup.Instance;
            if (setup == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player not ready.");
                return;
            }

            try
            {
                setup.TriggerEmote(emote);
                API.HeavenlyAPI.Toast($"[Heavenly] Emote {emote:0} played.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Emote failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Emote failed: {ex.Message}");
        }
    }

    public static void SetJumpMult(float mult)
    {
        mult = ClampMult(mult);
        try { Config.HeavenlyConfig.JumpMult = mult; } catch { }
        ApplyLocomotion();
        API.HeavenlyAPI.Log($"Jump impulse -> {mult:0.00}x");
    }

    public static void SetJumpCount(float count)
    {
        int c = (int)count;
        if (c < 1) c = 1;
        if (c > 5) c = 5;
        try { Config.HeavenlyConfig.JumpCount = c; } catch { }
        ApplyLocomotion();
        API.HeavenlyAPI.Toast(c > 1
            ? $"[Heavenly] Multi-jump x{c} ON."
            : "[Heavenly] Multi-jump OFF (single jump).");
    }

    public static void SetBunnyHop(bool enabled)
    {
        BunnyHop = enabled;
        API.HeavenlyAPI.Toast(enabled
            ? "[Heavenly] Bunny hop ON (hold jump)."
            : "[Heavenly] Bunny hop OFF.");
    }

    public static void SetCrouch(bool crouch)
    {
        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            try
            {
                controller.ChangeCrouch(crouch);
                IsCrouched = crouch;
                if (crouch) IsProne = false;
                API.HeavenlyAPI.Toast(crouch ? "[Heavenly] Crouched." : "[Heavenly] Stood up.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Crouch failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Crouch failed: {ex.Message}");
        }
    }

    public static void SetProne(bool prone)
    {
        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            try
            {
                controller.ChangeProne(prone);
                IsProne = prone;
                if (prone) IsCrouched = false;
                API.HeavenlyAPI.Toast(prone ? "[Heavenly] Prone." : "[Heavenly] Stood up.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Prone failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Prone failed: {ex.Message}");
        }
    }

    public static void StopMotion()
    {
        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            try
            {
                controller.ResetAllForces();
                controller.ClearAccumulatedForces();
                API.HeavenlyAPI.Toast("[Heavenly] Motion stopped.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Stop motion failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Stop motion failed: {ex.Message}");
        }
    }

    public static void Blink(float meters = 5f)
    {
        try
        {
            if (meters < 1f) meters = 1f;
            if (meters > 50f) meters = 50f;

            var setup = PlayerSetup.Instance;
            if (setup == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player not ready.");
                return;
            }

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            Vector3 dest;
            try { dest = setup.GetPlayerPosition() + setup.GetPlayerForward() * meters; }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Blink failed: {ex.Message}");
                return;
            }

            try
            {
                controller.TeleportPlayerTo(dest, false, true, false, null);
                API.HeavenlyAPI.Toast($"[Heavenly] Blinked {meters:0}m.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Blink failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Blink failed: {ex.Message}");
        }
    }

    public static void LogPosition()
    {
        try
        {
            var setup = PlayerSetup.Instance;
            if (setup == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player not ready.");
                return;
            }

            Vector3 pos;
            try { pos = setup.GetPlayerPosition(); }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Position failed: {ex.Message}");
                return;
            }

            API.HeavenlyAPI.Log($"My position: {pos.x:0.00}, {pos.y:0.00}, {pos.z:0.00}");
            API.HeavenlyAPI.Toast("[Heavenly] Position logged.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Position failed: {ex.Message}");
        }
    }

    public static void NudgeUp()
    {
        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            Vector3 pos;
            try { pos = controller.transform.position; }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Nudge failed: {ex.Message}");
                return;
            }

            try
            {
                controller.TeleportPlayerTo(pos + new Vector3(0f, 1f, 0f), false, true, false, null);
                API.HeavenlyAPI.Toast("[Heavenly] Nudged up 1m.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Nudge failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Nudge failed: {ex.Message}");
        }
    }

    public static void SetMirror(bool enabled)
    {
        MirrorEnabled = enabled;

        if (!enabled)
        {
            DestroyMirror();
            API.HeavenlyAPI.Toast("[Heavenly] Mirror OFF.");
            return;
        }

        try
        {
            DestroyMirror();

            GameObject? prefab = null;
            try { prefab = ABI_RC.Core.Savior.MetaPort.Instance?.personalMirrorPrefab; } catch { }
            if (prefab == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No mirror prefab available.");
                MirrorEnabled = false;
                return;
            }

            Camera? cam = null;
            try { cam = Camera.main; } catch { }
            if (cam == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No camera for mirror placement.");
                MirrorEnabled = false;
                return;
            }

            _mirror = UnityEngine.Object.Instantiate(prefab);
            _mirror.name = "HeavenlyMirror";

            Vector3 pos = cam.transform.position + cam.transform.forward * 2f;
            _mirror.transform.position = pos;
            try
            {
                Vector3 toCam = (cam.transform.position - pos).normalized;
                if (toCam.sqrMagnitude > 0.0001f)
                    _mirror.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
            }
            catch { }

            API.HeavenlyAPI.Toast("[Heavenly] Mirror ON (2m ahead).");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Mirror failed: {ex.Message}");
            MirrorEnabled = false;
            DestroyMirror();
        }
    }

    private static void DestroyMirror()
    {
        try
        {
            if (_mirror != null)
                UnityEngine.Object.Destroy(_mirror);
        }
        catch { }
        _mirror = null;
    }

    public static void SitOnSelectedPlayer()
    {
        try
        {
            string? userId = Social.PlayerTarget.CurrentId();
            string? userName = Social.PlayerTarget.CurrentName();

            if (userId == null || userId.Length == 0)
            {
                API.HeavenlyAPI.Toast("[Heavenly] No target: use Choose Player or select one in the QuickMenu.");
                return;
            }

            _sitTargetUserId = userId;
            _sitMissFrames = 0;
            SittingOnPlayer = true;

            API.HeavenlyAPI.Toast($"[Heavenly] Sitting on {userName ?? userId} ({SitBone}). Press Stand Up to stop.");
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Sit failed: {ex.Message}");
        }
    }

    public static void StandUp()
    {
        SittingOnPlayer = false;
        _sitTargetUserId = null;
        API.HeavenlyAPI.Toast("[Heavenly] Stood up.");
    }

    private static int _enforceFrames;

    /// <summary>Called every frame from HeavenlyPlugin.OnUpdate.</summary>
    public static void OnUpdate()
    {
        ReconcileFlyState();
        EnforceJumpSettings();
        UpdateBunnyHop();
        UpdateSitting();
    }

    /// <summary>
    /// The controller reverts jump tuning (world loads, avatar swaps, state
    /// changes), so re-assert our values on a throttle. Silent by design.
    /// </summary>
    private static void EnforceJumpSettings()
    {
        try
        {
            if (++_enforceFrames < 30)
                return;
            _enforceFrames = 0;

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
                return;

            int wantCount = 1;
            float wantMult = 1f;
            try { wantCount = Config.HeavenlyConfig.JumpCount; } catch { }
            try { wantMult = Config.HeavenlyConfig.JumpMult; } catch { }
            if (wantCount < 1) wantCount = 1;
            if (wantCount > 5) wantCount = 5;
            wantMult = ClampMult(wantMult);

            try
            {
                if (controller.jumpMaxCount != wantCount)
                    controller.jumpMaxCount = wantCount;
            }
            catch { }

            try
            {
                if (_origJumpImpulse < 0f)
                    _origJumpImpulse = controller.jumpImpulse;
                float wantImpulse = _origJumpImpulse * wantMult;
                if (Math.Abs(controller.jumpImpulse - wantImpulse) > 0.01f)
                    controller.jumpImpulse = wantImpulse;
            }
            catch { }
        }
        catch { }
    }

    private static bool _wasGrounded = true;
    private static int _bhopFires;
    private static float _nextBhopLog;

    private static void UpdateBunnyHop()
    {
        if (!BunnyHop || IsFlying || SittingOnPlayer)
        {
            _wasGrounded = true;
            return;
        }

        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
                return;

            bool grounded = false;
            try
            {
                var movement = controller.CharacterMovement;
                if (movement != null)
                    grounded = movement.isGrounded;
            }
            catch { }

            // Edge pulse: holding jump=true every frame wedges the jump
            // state machine (freeze in place). Fire once per landing only.
            bool landed = grounded && !_wasGrounded;
            _wasGrounded = grounded;

            if (!landed)
                return;

            try
            {
                var input = ABI_RC.Systems.InputManagement.CVRInputManager.Instance;
                if (input == null)
                    return;
                input.jump = true;
                _bhopFires++;

                if (Time.time >= _nextBhopLog)
                {
                    _nextBhopLog = Time.time + 5f;
                    API.HeavenlyAPI.Log($"Bhop fired x{_bhopFires} (landing jumps).");
                }
            }
            catch { }
        }
        catch { }
    }

    public static void Dash(float meters = 6f)
    {
        try
        {
            if (meters < 1f) meters = 1f;
            if (meters > 30f) meters = 30f;

            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
            {
                API.HeavenlyAPI.Toast("[Heavenly] Local player controller not ready.");
                return;
            }

            Vector3 dir;
            try
            {
                Camera? cam = Camera.main;
                if (cam == null)
                {
                    API.HeavenlyAPI.Toast("[Heavenly] No camera for dash direction.");
                    return;
                }
                dir = cam.transform.forward;
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Dash failed: {ex.Message}");
                return;
            }

            try
            {
                controller.TeleportPlayerTo(
                    controller.transform.position + dir * meters,
                    false,
                    false,
                    true,
                    null
                );
                API.HeavenlyAPI.Toast($"[Heavenly] Dashed {meters:0}m.");
            }
            catch (Exception ex)
            {
                API.HeavenlyAPI.Error($"Dash failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Error($"Dash failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Picks up flight changes made outside Heavenly (CVR's own menu, world
    /// transitions) so the model and keybinds don't lie. Throttled to ~1/s.
    /// Model-only: UILib toggles are one-way and can't be re-pushed.
    /// </summary>
    private static void ReconcileFlyState()
    {
        try
        {
            if (++_reconcileFrames < 60)
                return;
            _reconcileFrames = 0;

            bool? actual = null;
            try
            {
                Player? local = PlayerAPI.LocalPlayerInternal;
                if (local != null)
                    actual = local.IsFlying;
            }
            catch { }

            // NOTE: no controller fallback here on purpose. The only cheap
            // controller reading (IsFlyingWithNoClip) can't see normal flight,
            // so using it could flip the model OFF while actually flying.
            if (actual != null && actual.Value != IsFlying)
            {
                IsFlying = actual.Value;
                API.HeavenlyAPI.Log($"Fly state reconciled -> {(IsFlying ? "ON" : "OFF")} (external change).");
            }
        }
        catch { }
    }

    private static void UpdateSitting()
    {
        if (!SittingOnPlayer)
            return;

        string? targetUserId = _sitTargetUserId;
        if (targetUserId == null || targetUserId.Length == 0)
            return;

        try
        {
            BetterBetterCharacterController? controller = null;
            try { controller = BetterBetterCharacterController.Instance; } catch { }
            if (controller == null)
                return;

            Vector3? targetPos = GetSitTargetPosition(targetUserId);
            if (targetPos == null)
            {
                // Target left or not resolvable: give up after a grace period.
                int limit = SitMissLimit();
                _sitMissFrames++;
                if (_sitMissFrames >= limit)
                {
                    SittingOnPlayer = false;
                    _sitTargetUserId = null;
                    _sitMissFrames = 0;
                    API.HeavenlyAPI.Toast("[Heavenly] Sit target lost. Stood up.");
                }
                return;
            }

            _sitMissFrames = 0;

            // Snap local player to the target bone. Teleport without interpolation
            // so the ECM2 controller does not fight us with smoothing.
            try
            {
                controller.TeleportPlayerTo(
                    targetPos.Value,
                    false,
                    false,
                    false,
                    null
                );
            }
            catch
            {
                try { controller.transform.position = targetPos.Value; } catch { }
            }
        }
        catch (Exception ex)
        {
            API.HeavenlyAPI.Warning($"Sit follow failed: {ex.Message}");
            SittingOnPlayer = false;
        }
    }

    private static float SitExtraOffset()
    {
        try
        {
            float v = Config.HeavenlyConfig.SitOffset;
            if (v < 0f) return 0f;
            if (v > 2f) return 2f;
            return v;
        }
        catch
        {
            return 0f;
        }
    }

    public static void SetSitOffset(float meters)
    {
        if (meters < 0f) meters = 0f;
        if (meters > 2f) meters = 2f;
        try { Config.HeavenlyConfig.SitOffset = meters; } catch { }
        API.HeavenlyAPI.Log($"Sit offset -> {meters:0.00}m");
    }

    private static int SitMissLimit()
    {
        try
        {
            float sec = Config.HeavenlyConfig.SitTimeoutSec;
            if (sec < 1f) sec = 1f;
            if (sec > 30f) sec = 30f;
            return (int)(sec * 60f);
        }
        catch
        {
            return 300;
        }
    }

    private static Vector3? GetSitTargetPosition(string userId)
    {
        try
        {
            // 1. Try the currently selected QuickMenu player object (has Animator directly).
            try
            {
                var selected = QuickMenuAPI.SelectedPlayerEntity;
                if (selected != null && selected.Uuid == userId)
                {
                    Vector3? p = BonePosition(selected.AvatarAnimator, selected.PlayerGameObject);
                    if (p != null) return p;
                }
            }
            catch { }

            // 2. Fall back to scanning network players.
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
                            GameObject? root = entity.PlayerObject;
                            if (root == null) continue;
                            Animator? animator = root.GetComponentInChildren<Animator>();
                            Vector3? p = BonePosition(animator, root);
                            if (p != null) return p;
                            return root.transform.position + Vector3.up * 1.6f;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 3. Last resort: PlayerAPI remote list position.
            try
            {
                foreach (Player remote in PlayerAPI.RemotePlayersInternal)
                {
                    if (remote == null || remote.UserID != userId)
                        continue;
                    try { return remote.GetPosition() + Vector3.up * 1.6f; } catch { }
                }
            }
            catch { }
        }
        catch { }

        return null;
    }

    private static Vector3? BonePosition(Animator? animator, GameObject? fallbackRoot)
    {
        try
        {
            if (animator != null)
            {
                Transform? bone = animator.GetBoneTransform(SitBone);
                if (bone != null)
                {
                    Vector3 pos = bone.position;
                    if (SitBone == HumanBodyBones.Head)
                        pos += Vector3.up * 0.35f;
                    pos += Vector3.up * SitExtraOffset();
                    return pos;
                }

                // Animator exists but bone missing - sit above head.
                if (fallbackRoot != null)
                    return fallbackRoot.transform.position + Vector3.up * 1.9f;
            }
        }
        catch { }

        if (fallbackRoot != null)
        {
            try { return fallbackRoot.transform.position + Vector3.up * 1.6f; } catch { }
        }

        return null;
    }
}
