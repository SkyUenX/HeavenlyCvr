# HeavenlyCVR

A ChilloutVR client mod inspired by HeavenlyVRC's layout (MelonLoader).
QoL-focused: native CVR APIs only. No force-clone of private avatars,
no serialize/desync abuse, no earrape, no remote kill-switch.

## Menu model (preserved Heavenly-style API)

- `HeavenlyAPI` (log/toast helpers)
- `HeavenlyPage` (buttons, toggles, sliders, labels, sub-pages)
- `HeavenlyButton` / `HeavenlyToggle` / `HeavenlySlider` / `HeavenlyLabel`
- `HeavenlyUI` (page registry)
- `HeavenlyMenu` (menu state model)

Rendering is separate: `HeavenlyUILibBridge` renders into ChilloutVR's
built-in UILib (Cohtml QuickMenu) — the same backend BTKUILib uses, so the
Heavenly tab works with or without the BTKUILib mod. `HeavenlyStyle`
injects the Heavenly look (dark glass, crimson accent, hexagonal
buttons/toggles/tabs) as Cohtml CSS. `HeavenlyUIXBridge`
mirrors buttons/toggles/labels into a UI Expansion Kit settings category
(sliders live in the QuickMenu tab only — UIX has no slider control).

## Modules (all live, no placeholders)

- Main: navigation hub (page buttons really open pages)
- World: rejoin current/last/history/by-ID, reload, go home, set home
  here, copy instance/world IDs, world facts, world search, instance
  details, persisted instance history
- Avatar: switch by ID, reload, open details/hub, search, persisted
  favorites (incl. by-ID, wear-by-number, random), worn-avatar history,
  float param setter, trigger firing, core animator readout, full
  animator param list, viseme test, outfit toggle cycling, gesture
  sliders, emote stop
- Voice: master/avatar/world volumes, mic mute, deafen (no earrape)
- Movement: fly, fly noclip, fly/walk/sprint speed, jump + multi-jump,
  bunny hop, camera FOV, sit on selected player (bone choice + offset),
  stand up, crouch/prone, respawn, go-to-spawn, stop motion, nudge up,
  position logger, blink/dash teleports, position bookmarks, emotes,
  headlamp flashlight (angle/range/intensity), personal mirror
- Keys: rebindable Ctrl+ shortcuts (fly, rejoin, noclip, stand, panic)
- ESP: rank-colored x-ray outlines on avatars (HighlightPlus, nothing
  added to foreign objects), through-wall/hidden nameplates, pickup /
  spawnable markers, ESP diagnostics
- Social: teleport to selected player, face target, full target info
  readout, copy-to-clipboard, invite/request invite, friend request,
  unfriend, pending request accept/deny, block/unblock,
  hide/show avatar, wear target avatar, target avatar details + ID, user ID logging, local
  player notes (shown in join feed), chatbox sender, persistent
  per-player mute/volume/avatar-hide/props-hide/chatbox-hide/
  voice-normalize
- Protection: hide avatars, block portals+props+pickups, remove
  portals, safe shaders, light / material / poly / particle / audio /
  constraint / collider / rigidbody / joint / trail / cloth / video /
  blendshape / contact limiters, avatar-audio mute, avatar camera
  strip, avatar ID blocklist, avatar safety scans + read-only audits
  (avatar + world), auto-hide laggy avatars, auto rescan, distance
  culling, void-fall safety net, protection status log, panic button
- Debug: join/leave feed (console + live in-menu) + optional/friend
  toasts, instance status, roster with ranks, unique-player count,
  FPS cap, FPS/ping/uptime readout, clipboard status snapshot,
  menu reload, clean quit
- Tags: 3 custom nameplate slots (text, color, target, height,
  spacing); self-tags shared with other Heavenly users over
  mod-network (only self-tags broadcast, 64-char cap, toggleable),
  plate diagnostics included
- Config: every tunable (ESP timings/sizes/colors, light cap, scan delay,
  teleport offsets, sit timeout, feed size), plus reset to defaults

Settings persist in `ChilloutVR/UserData/MelonPreferences.cfg`
(category `HeavenlyCVR`) — editable in-game via the Config page or by
hand while the game is closed.

## Icons (your own logo/art)

Drop 256x256 PNGs into `ChilloutVR/UserData/HeavenlyCVR/Icons/`
(created on first run, no rebuild needed):

- `heavenly.png` — the Heavenly QuickMenu tab icon (your logo here)
- `main.png`, `world.png`, `avatar.png`, ... — per-page icons
  (lowercase page name + `.png`)

Missing files fall back to CVR defaults; the console log tells you
exactly which art was found. Button icons stay hidden on purpose so
pages without art don't show placeholder glyphs.

## Keybinds

Default: Ctrl+F fly, Ctrl+R rejoin, Ctrl+N noclip, Ctrl+T stand up,
Ctrl+P panic (respawn/reload-avatar/ESP/flashlight unbound). All
rebindable from the Keys page (letters only; LeftControl stays the
modifier).


## Version

0.2.0

## Build

Targets `net48` to match ChilloutVR/Unity assemblies.

```bash
dotnet clean
rm -rf bin obj
dotnet restore
dotnet build
```

Game/reference DLLs are resolved from a local ChilloutVR install via
`HintPath` entries in `HeavenlyCVR.csproj`; adjust the paths if your
Steam library lives elsewhere.

## Install

Copy:

`bin/Debug/net48/HeavenlyCVR.dll`

to:

`ChilloutVR/Mods/HeavenlyCVR.dll`

Do not copy dependency DLLs into `Mods`.
