![1000120182](https://github.com/user-attachments/assets/c035f0df-7035-427c-8b8f-61a2904a8e6c)

## Features

### Gun Control Fix
Corrects gun ballistics against the vanilla trajectory simulation: overrides `Bullet.TrajectoryTrace`'s timestep to use `Time.fixedDeltaTime`, and replaces `Kinematics.TrajectorySim` with a fixed-timestep simulation (proper gravity/drag integration, early-exit on divergence) so AI gun-lead calculations are accurate.

### IRplus IR Overhaul
- **High off-boresight launch** — raises the HUD/weapon lock-on angle for IR missiles up to a configurable angle (default 90°).
- **Enhanced turning** — raises IR missile turn rate and torque for tighter tracking performance.
- **Lock-On After Launch (LOAL)** — missiles fired without a lock search a cone ahead of them for a valid IR target after launch, with optional player-view slaving while scanning and per-target flare-evasion memory so decoyed missiles require a stronger signature to relock.

### Railgun Fixes
- **Priority fix** — railguns (155mm-class) never target missiles or aircraft; all other targeting priority logic is reimplemented to match QoL's original behavior.
- **Range fix** — boosts railgun `maxRange` and multiplies bullet self-destruct timer so shells don't despawn before reaching extended-range targets.

### Navex Ship Fixes
Recenters ship wake VFX onto the hull centerline (fixing starboard-offset and heading-independent drift), with per-ship exclusions and special-cased centering for certain hulls. Also tunes the `Aryx_StrikeCarrier1` (Andromeda) and `Aryx_EscortCarrier1` (Devotion) propulsion thrust and hull drag to reach their intended top speeds.
- **ShipAI Zero Standoff** — Forces ship AI `standoffDistance` to 0, so they navigate directly to their waypoints instead of orbiting awkwardly at a distance.
- **Hovercraft Climb Fix** — LandingCraft1 (hovercraft) propulsion is marked underwater-only by default, which hard-cuts thrust to zero the instant it climbs onto a beach. Removes that cutoff and applies a configurable thrust multiplier (default 1.15x) so it can climb ashore.

### AI Short Takeoff Fix
AI-piloted swivel-duct aircraft (default: SmallFighter1 / Vortex) are hard-locked out of the automatic duct-vectoring mode selection that human pilots get, so they never use the reduced duct angle needed for a short takeoff roll — causing AI to fail launching off short carrier decks (e.g. SmallCarrier1 / Cursor). This lets matched AI aircraft use the same automatic Forward/ShortTakeoff/Hover/ShortLanding mode selection as a human pilot.

### QoL Integration
- **Restore Event Content** — removes the "Event Content Only" restriction QoL 1.1.8.1 added to the APM-71, ATB-10, 40mm Railcannon.
- **Hide P2082 Mod Tag** — hides the QoL version banner on the main menu.
- **Suppress QoL Debug Log** — drops QoL's Debug-level log spam (e.g. `[LoadoutTrace]` dumps fired on every AI aircraft spawn) before it's written to the console/log file. Mass AI spawns during mission load can emit hundreds of these synchronously, causing multi-second frame stalls; this is enabled by default since it's purely cosmetic logging.
- **Disable Aircraft Livery Randomization** — unpatches QoL's performance heavy per-spawn random-livery assignment.
- **Disable Aircraft Name Randomization** — unpatches QoL's per-spawn random pilot-name assignment. Cheaper than the livery randomization; left enabled (not unpatched) by default, but available to toggle off if spawn-time stutter persists.
- **Cursor LADS to CIWS** — optionally preserves QoL's swap of the Cursor's rear LADS with a CIWS. If disabled, reverts to the vanilla Cursor air defense layout (2 CIWS + 1 LADS).
- **PAB-350LR Dispenser Fix** — removes the inherited `SubmunitionDispenser` component so PAB-350LR behaves as a single-warhead bunker-buster instead of a cluster glide bomb.

### Integrations
- **Catapult Integration** — Injects catapult launch support and correct offsets for the Kestrel and Ternion. Also includes an optional Catapult Offset Logger that automatically finds the front landing gear (`gear_f`) on any modded aircraft and spits out a ready-to-use QoL catapult data string to make it catapult-capable.
- **Dynamo (Destroyer1) Brain Fix** — Fixes the broken vanilla Destroyer1 (Dynamo) AI by copying `RoleIdentity`, `FireControl`, and `Turret` definitions from the Aryx HeavyFrigate1 (Ironside), granting it full target engagement logic.
- **Naval Interceptor Nerf Undo**: Reverted nerfs applied to the Eclipse's radar (`minSignal`) and AAM-45 Sabre's speed (`supersonicDrag`).
- **Airbase Radius Clamp** — Clamps a carrier's `Airbase.GetRadius()` to the ship's actual hull length, preventing oversized/overlapping carrier airspace that confuses ATC.
- **QoL Preclusion Override** — Neutralises QoL's `LoadCustomWeapons` StandardLoadouts wipe (which causes AI aircraft to use impossible loadouts on MC-260 Chimera) by intercepting the wipe loop via a transpiler, restoring vanilla AI weapon-preclusion behaviour.

### WIP Missiles & Advanced Loggers
- **WIP Missiles VLS Enabler (Piledriver MIRV, AAM-42N, MMR-S4)** — Re-enables incomplete experimental missiles and patches AAM-42N to function cleanly in VLS. Added VLS booster and safety fuses.
- **P_AAM2 Deeper Logger** — (Optional Logger) Analyzing why this WIP missile is not choosing targets correctly.
- **Ship AI & Railgun Deeper Logger** — (Optional Logger) Analyzes `CombatAI.AnalyzeTarget` and `Turret.AssessTargetPriority` from inside the game to see exactly why ships choose their targets.

### Misc Fixes
- **Vanilla NPE Guards** — Suppresses recurring vanilla `Refueler.Start()` null-reference exceptions.
- **Landing Craft Stuck Fix** — Prevents Landing Crafts from permanently freezing in a "Holding" state if they approach the beach too quickly.
