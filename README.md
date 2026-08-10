![1000120182](https://github.com/user-attachments/assets/c035f0df-7035-427c-8b8f-61a2904a8e6c)

## Features

### Gun Control Fix
Corrects gun ballistics against the vanilla trajectory simulation: overrides `Bullet.TrajectoryTrace`'s timestep to use `Time.fixedDeltaTime`, and replaces `Kinematics.TrajectorySim` with a fixed-timestep simulation (proper gravity/drag integration, early-exit on divergence) so AI gun-lead calculations are accurate.

### IRplus IR Overhaul
- **High off-boresight launch** — raises the HUD/weapon lock-on angle for IR missiles up to a configurable angle (default 90°).
- **Enhanced turning** — raises IR missile turn rate and torque for tighter tracking performance.
- **Lock-On After Launch (LOAL)** — missiles fired without a lock search a cone ahead of them for a valid IR target after launch, with optional player-view slaving while scanning and per-target flare-evasion memory so decoyed missiles require a stronger signature to relock.

### Navex Ship Fixes
- **Wake Recenter**: Recenters ship wake VFX onto the hull centerline (fixing starboard-offset and heading-independent drift).
- **Airbase Radius Clamp**: Clamps a carrier's `Airbase.GetRadius()` to the ship's actual hull length, preventing oversized/overlapping carrier airspace that confuses ATC.

### Modular Tailhooks
- **Vanilla Tailhook Tweaks**: Adjusts deployed angle for vanilla tailhooks so they catch wires better.
- **Brawler & Ternion Tailhooks**: Adds functional tailhook mounts to the Brawler (CAS1) and Ternion (Multirole1) so they can land on carriers.

### Integrations
- **Penumbra Catapult Integration** — Allows Ternion/Kestrel to launch from Penumbra, and ensures aircraft spawned in Penumbra elevators/hangar correctly assign to their respective catapults. Includes an optional catapult offset logger.
