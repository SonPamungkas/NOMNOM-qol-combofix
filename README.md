![1000120182](https://github.com/user-attachments/assets/c035f0df-7035-427c-8b8f-61a2904a8e6c)

## Features

### Gun Control Fix
Corrects gun ballistics against the vanilla trajectory simulation: overrides `Bullet.TrajectoryTrace`'s timestep to use `Time.fixedDeltaTime`, and replaces `Kinematics.TrajectorySim` with a fixed-timestep simulation (proper gravity/drag integration, early-exit on divergence) so AI gun-lead calculations are accurate.

### IRplus IR Overhaul
- **High off-boresight launch** — raises the HUD/weapon lock-on angle for IR missiles up to a configurable angle (default 90°).
- **Enhanced turning** — raises IR missile turn rate and torque for tighter tracking performance.
- **Lock-On After Launch (LOAL)** — missiles fired without a lock search a cone ahead of them for a valid IR target after launch, with optional player-view slaving while scanning and per-target flare-evasion memory so decoyed missiles require a stronger signature to relock.
- **PAB-350LR dispenser fix** — removes the inherited `SubmunitionDispenser` component so PAB-350LR behaves as a single-warhead bunker-buster instead of a cluster glide bomb.

### Railgun Fixes
- **Priority fix** — railguns (155mm-class) never target missiles or aircraft; all other targeting priority logic is reimplemented to match QoL's original behavior.
- **Range fix** — boosts railgun `maxRange` and multiplies bullet self-destruct timer so shells don't despawn before reaching extended-range targets.

### Navex Ship Fixes
Recenters ship wake VFX onto the hull centerline (fixing starboard-offset and heading-independent drift), with per-ship exclusions and special-cased centering for certain hulls. Also tunes the Andromeda Class Cruiser's and Devotion Class Light Carrier's propulsion thrust and hull drag to reach their intended top speeds.
- **Hovercraft Climb Fix** — LandingCraft1 (hovercraft) propulsion is marked underwater-only by default, which hard-cuts thrust to zero the instant it climbs onto a beach. Removes that cutoff and applies a configurable thrust multiplier (default 1.15x) so it can climb ashore.

### AI Short Takeoff Fix
AI-piloted swivel-duct aircraft (default: SmallFighter1 / Vortex) are hard-locked out of the automatic duct-vectoring mode selection that human pilots get, so they never use the reduced duct angle needed for a short takeoff roll — causing AI to fail launching off short carrier decks (e.g. SmallCarrier1 / Cursor). This lets matched AI aircraft use the same automatic Forward/ShortTakeoff/Hover/ShortLanding mode selection as a human pilot.

### QoL Integration
- **Restore Event Content** — removes the "Event Content Only" restriction QoL 1.1.8.1 added to the APM-71, ATB-10, and 40mm Railcannon.
- **Hide P2082 Mod Tag** — hides the QoL version banner on the main menu.
- **Suppress QoL Debug Log** — drops QoL's Debug-level log spam (e.g. `[LoadoutTrace]` dumps fired on every AI aircraft spawn) before it's written to the console/log file. Mass AI spawns during mission load can emit hundreds of these synchronously, causing multi-second frame stalls; this is enabled by default since it's purely cosmetic logging.
- **Disable Aircraft Livery Randomization** — unpatches QoL's per-spawn random-livery assignment. QoL's own author flags this as a performance risk with many liveries installed; it's the most likely cause of multi-second frame stalls during mass AI spawn bursts, so this is disabled (unpatched) by default.
- **Disable Aircraft Name Randomization** — unpatches QoL's per-spawn random pilot-name assignment. Cheaper than the livery randomization; left enabled (not unpatched) by default, but available to toggle off if spawn-time stutter persists.

### Integrations
- **Airbase Radius Clamp** — clamps a carrier's `Airbase.GetRadius()` to the ship's actual hull length, preventing oversized/overlapping carrier airspace that confuses ATC.
- **QoL Preclusion Override** — replaces QoL's own AI weapon-preclusion patch (which uses per-spawn reflection-by-name) with an equivalent cached-accessor implementation for lower overhead.

### Misc Fixes
Suppresses a recurring vanilla `Refueler.Start()` null-reference exception.

---

## Requirements
- P2082 QoL mod (1.1.8.1)
- Aryx aircraft
