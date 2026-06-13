## Features

### Kestrel FBW Fix (Calibrated to Default)
Tunable fly-by-wire PID parameters for the FQ-106 Kestrel, replacing its stock oscillation-prone values. All gains are exposed in BepInEx config.
### F-16M FBW Fix
- **Center of Mass correction** — The F-16M's CoM is rear-heavy (Z = −0.06), causing persistent pitch-up and tail/engine damage when spawning. This fix shifts the CoM forward to a configurable Z position (default 0.5).
- Tunable FBW PID parameters (pFactorFast, dFactorFast, yaw/roll tightness, angular velocity limits).
- **Landing Gear Softening** — The F-16M's stock landing gear is overly stiff and bounces the aircraft back into the air on touchdown. This fix applies configurable multipliers to the gear's spring rate and damping rate (defaults: 0.7x spring, 2.0x damping) for a smoother landing.
### F-99 FBW Fix (Calibrated to Default)
Tunable FBW PID parameters for the F-99 Shrike, correcting its stock oscillation behaviour.
### Chimera Tail Weld Fix (Should be Unnecessary)
Welds the MC-260 Chimera's top-rear fuselage panel into the airframe to stop it visibly twisting under V-tail load, with mass/CoM reconciliation and an impact-breaker for hard crashes. Also spawns the Chimera with Flight Assist off.
### Navex Ship Fixes
Recenters the wake VFX origin onto the hull centerline for big ships, fixing both a starboard-offset wake and a heading-independent (compass-direction) drift on the Atlas Class Supply Ship, Devotion Class Light Carrier, and Ironside Class Frigate. Also tunes the Andromeda class Cruiser's and Devotion Class Light Carrier's propulsion thrust and hull drag to reach their intended top speeds.

### QoL Integration
- **Suppress QoL debug logs** — Optional filter for the verbose `[Debug : qol]` log spam.
- **Hide P2082ModTag** — Hides the QoL version banner on the main menu.
- **Fix Drop Tank Pierce Damage** — Zeroes the pierce damage on `P_DropTank1_spent` so jettisoned tanks no longer destroy your aircraft.
- **F-16M Drop Tank** — Adds the DT-1600 drop tank as a loadout option on the F-16M's inner wing pylons.
- **Cross-Aircraft Weapon Transfer** — Copies QoL-added weapon loadouts from vanilla aircraft to Aryx equivalents (Chicane→Knockout, Revoker→F-99, Cricket→MiG-15, Vortex→F-16M).
- **Compass Nozzle Fix** — QoL's T/A-30 Compass model swap (CompassNew bundle) reassigns the engine nozzle meshes but leaves their materials mismatched, making the nozzles appear to disappear. This fix copies the correct materials over so the nozzles render properly.

## Requirements
- P2082 QoL mod (1.1.8.0)
- Aryx aircraft
